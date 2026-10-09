using System.Collections.Generic;
using UnityEngine;

namespace PTITGameSDK.Core
{
    public class PTITGameTracker : MonoBehaviour
    {
        private static PTITGameTracker _instance;
        public static PTITGameTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PTITGameTracker");
                    _instance = go.AddComponent<PTITGameTracker>();
                    DontDestroyOnLoad(go);
                    _instance.InitializeDefaultProviders();
                }
                return _instance;
            }
        }

        private List<ITrackingProvider> _providers = new List<ITrackingProvider>();

        // Global Metadata Variables
        public string SessionId { get; private set; } = string.Empty;
        public string AudienceId { get; private set; } = string.Empty;
        public string CampaignName { get; private set; } = string.Empty;
        public string Variant { get; private set; } = string.Empty;
        public string Version { get; private set; } = string.Empty;

        public string ServerUrl { get; private set; } = string.Empty;
        public string GameId { get; private set; } = string.Empty;
        public string InstallSource { get; private set; } = "unknown";

        private void Awake()
        {
            Version = Application.version;

            // Auto generate Session ID for current app session
            SessionId = System.Guid.NewGuid().ToString();

            // Fetch or create persistent Audience ID (Device ID)
            if (PlayerPrefs.HasKey("ptit_audience_id"))
            {
                AudienceId = PlayerPrefs.GetString("ptit_audience_id");
            }
            else
            {
                AudienceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(AudienceId) || AudienceId == SystemInfo.unsupportedIdentifier)
                {
                    AudienceId = System.Guid.NewGuid().ToString();
                }
                PlayerPrefs.SetString("ptit_audience_id", AudienceId);
                PlayerPrefs.Save();
            }

            InitializeInstallSource();
        }

        /* Hàm InitializeInstallSource sẽ phân loại nguồn cài đặt thành:
           "google_play": Nếu cài từ Google Play (com.android.vending).
           "apk": Nếu cài từ file APK ngoài, sideload (trả về null/empty).
           "app_store": (Mặc định cho nền tảng iOS).
           Hoặc trả về đúng tên package name của store bên thứ 3 (ví dụ của Amazon, Samsung Galaxy Store...).
           "editor": Nếu đang chạy test trên Unity Editor.
        */
        private void InitializeInstallSource()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        string packageName = currentActivity.Call<string>("getPackageName");
                        using (AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                        {
                            string installerPackageName = packageManager.Call<string>("getInstallerPackageName", packageName);
                            if (string.IsNullOrEmpty(installerPackageName))
                            {
                                InstallSource = "apk";
                            }
                            else if (installerPackageName == "com.android.vending")
                            {
                                InstallSource = "google_play";
                            }
                            else
                            {
                                InstallSource = installerPackageName; // Các store khác như amazon, samsung...
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PTITGameSDK] Error getting install source: " + e.Message);
                InstallSource = "error";
            }
            #elif UNITY_IOS && !UNITY_EDITOR
                        InstallSource = "app_store"; // Trên iOS đa số cài từ AppStore trừ khi sideload
            #else
                        InstallSource = "editor";
            #endif
        }

        private void InitializeDefaultProviders()
        {
            // By default, just register Firebase
            //RegisterProvider(new FirebaseTrackingProvider());
            
            // Tự động khởi tạo server nội bộ với URL mặc định
            InitTracking("block_crush", "https://bigame.ezwork.vn/logs");
        }

        /// <summary>
        /// Initialize the SDK with a custom server URL to send logs in parallel.
        /// </summary>
        public void InitTracking(string gameId, string customServerUrl = "https://bigame.ezwork.vn/logs", bool useBatchMode = true)
        {
            GameId = gameId;
            ServerUrl = customServerUrl;
            if (!string.IsNullOrEmpty(customServerUrl))
            {
                RegisterProvider(new CustomServerTrackingProvider(customServerUrl, gameId, useBatchMode));
            }
        }

        public void RegisterProvider(ITrackingProvider provider)
        {
            // Bỏ qua nếu đã tồn tại provider cùng loại (tránh duplicate init làm kẹt/crash queue)
            if (_providers.Exists(p => p.GetType() == provider.GetType()))
            {
                return;
            }
            
            provider.Initialize();
            _providers.Add(provider);
        }

        /// <summary>
        /// Configure global metadata to be attached to all events.
        /// </summary>
        public void SetGlobalMetadata(string sessionId, string audienceId, string campaignName, string variant)
        {
            SessionId = sessionId;
            AudienceId = audienceId;
            CampaignName = campaignName;
            Variant = variant;
        }
        
        public void SetSessionId(string sessionId) => SessionId = sessionId;
        public void SetAudienceId(string audienceId) => AudienceId = audienceId;

        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.LogEvent(eventName, parameters);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PTITGameTracker] Error in provider {provider.GetType().Name}: {e.Message}");
                }
            }
        }

        public void SetUserProperty(string propertyName, string value)
        {
            foreach (var provider in _providers)
            {
                provider.SetUserProperty(propertyName, value);
            }
        }

        public void FlushLogs()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.ForceSyncCache(); // Đảm bảo đồng bộ ngay lập tức xuống disk trước khi flush
                    provider.Flush();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PTITGameTracker] Error flushing provider {provider.GetType().Name}: {e.Message}");
                }
            }
        }

        // private void OnApplicationPause(bool pauseStatus)
        // {
        //     if (pauseStatus)
        //     {
        //         FlushLogs();
        //     }
        // }

        // private void OnApplicationQuit()
        // {
        //     FlushLogs();
        // }
    }
}
