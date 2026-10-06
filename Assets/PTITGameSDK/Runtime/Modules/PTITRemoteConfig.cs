using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.RemoteConfig;
using Firebase.Extensions;

namespace PTITGameSDK.Modules
{
    public class PTITRemoteConfig : MonoBehaviour
    {
        private static PTITRemoteConfig _instance;
        public static PTITRemoteConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PTITRemoteConfig");
                    _instance = go.AddComponent<PTITRemoteConfig>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public bool IsDataFetched { get; private set; } = false;

        /// <summary>
        /// Khởi tạo và thiết lập các giá trị mặc định cho Remote Config.
        /// Gọi hàm này sau khi FirebaseApp đã sẵn sàng.
        /// </summary>
        public void InitializeAndFetch(Dictionary<string, object> defaultValues = null)
        {
            if (defaultValues != null && defaultValues.Count > 0)
            {
                FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaultValues).ContinueWithOnMainThread(task =>
                {
                    FetchData();
                });
            }
            else
            {
                FetchData();
            }
        }

        private void FetchData()
        {
            // Bắt đầu fetch dữ liệu từ server
            FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
            {
                if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                {
                    Debug.LogError("RemoteConfig fetch failed.");
                    IsDataFetched = false;
                    return;
                }

                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask =>
                {
                    if (activateTask.IsCompleted)
                    {
                        Debug.Log("RemoteConfig activated successfully.");
                        IsDataFetched = true;

                        // Tối ưu từ IKAME: Cache dữ liệu vào PlayerPrefs để dùng offline hoặc khi chưa fetch xong
                        foreach (var keyValuePair in FirebaseRemoteConfig.DefaultInstance.AllValues)
                        {
                            PlayerPrefs.SetString($"RemoteConfig_{keyValuePair.Key}", keyValuePair.Value.StringValue);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Chờ cho đến khi Remote Config được fetch thành công hoặc timeout (giống IKAME).
        /// </summary>
        public IEnumerator WaitForRemoteConfig(float timeoutSeconds = 20f)
        {
            float timeLimit = Time.realtimeSinceStartup + timeoutSeconds;
            var wait = new WaitForSecondsRealtime(0.5f);

            while (!IsDataFetched)
            {
                if (Time.realtimeSinceStartup > timeLimit)
                {
                    Debug.LogError("RemoteConfig data fetch timeout.");
                    yield break; // Thoát nếu quá thời gian
                }
                yield return wait;
            }
        }

        // ==============================================================================
        // CÁC HÀM GETTER (Bọc lại các hàm chuẩn của Firebase tránh crash nếu thiếu key)
        // Cải tiến: Nếu chưa fetch xong, ưu tiên lấy từ PlayerPrefs cache (như IKAME)
        // ==============================================================================

        public string GetString(string key, string defaultValue = "")
        {
            try
            {
                if (IsDataFetched)
                {
                    return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
                }
            }
            catch { }
            
            // Fallback to PlayerPrefs Cache
            if (PlayerPrefs.HasKey($"RemoteConfig_{key}"))
                return PlayerPrefs.GetString($"RemoteConfig_{key}");
            
            return defaultValue;
        }

        public long GetLong(string key, long defaultValue = 0)
        {
            try
            {
                if (IsDataFetched)
                {
                    return FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
                }
            }
            catch { }

            if (PlayerPrefs.HasKey($"RemoteConfig_{key}"))
            {
                if (long.TryParse(PlayerPrefs.GetString($"RemoteConfig_{key}"), out long result))
                    return result;
            }
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            try
            {
                if (IsDataFetched)
                {
                    return (float)FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;
                }
            }
            catch { }

            if (PlayerPrefs.HasKey($"RemoteConfig_{key}"))
            {
                if (float.TryParse(PlayerPrefs.GetString($"RemoteConfig_{key}"), out float result))
                    return result;
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            try
            {
                if (IsDataFetched)
                {
                    return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
                }
            }
            catch { }

            if (PlayerPrefs.HasKey($"RemoteConfig_{key}"))
            {
                if (bool.TryParse(PlayerPrefs.GetString($"RemoteConfig_{key}"), out bool result))
                    return result;
            }
            return defaultValue;
        }
    }
}
