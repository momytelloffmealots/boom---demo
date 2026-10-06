using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace GeckoArrow.Runtime.CoreSystem
{
    [System.Serializable]
    public abstract class PrintableConfig
    {
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    interface IRemoteConfigEntry
    {
        void Fetch();
    }

    [System.Serializable]
    public class RemoteConfigEntry<T> : IRemoteConfigEntry
    {
        [Header("Value")]
        public string key;
        /// <summary>
        /// If true, the value will be fetched from Remote Config, if Remote Config is not available, it will use the cached value from PlayerPrefs.
        /// If false, it will use the default value.
        /// </summary>
        public bool fetch = true;
        public T defaultValue;
        // Removed Odin [ShowIf] attribute to keep SDK independent
        public T value;

        public void ClearCache()
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        public void Print(bool prettyPrint = false)
        {
#if UNITY_EDITOR
            //Debug.Log($"Key: {key}, Value: {ToString()}, Default Value: {defaultValue}");
            //Print to json file using windows explorer path select
            var json = ToString(prettyPrint);
            Debug.Log($"JSON: {json}");

            string path = UnityEditor.EditorUtility.SaveFilePanel(
                "Save Remote Config Entry as JSON",
                "",
                $"{key}.json",
                "json");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Saved JSON to: {path}");
            }
#endif
        }

        public void Fetch()
        {
            if (fetch)
            {
                var cachedValue = defaultValue;
                bool hasCached = PlayerPrefs.HasKey(key);
                if (hasCached)
                {
                    cachedValue = ParseValue<T>(PlayerPrefs.GetString(key), defaultValue);
                }

                string remoteStringValue = ToString(false); // default string
                bool isDataFetched = false;
                if (PTITGameSDK.Modules.PTITRemoteConfig.Instance != null)
                {
                    isDataFetched = PTITGameSDK.Modules.PTITRemoteConfig.Instance.IsDataFetched;
                    remoteStringValue = PTITGameSDK.Modules.PTITRemoteConfig.Instance.GetString(key, ToString(false));
                }

                value = ParseValue<T>(remoteStringValue, defaultValue);

                string source;
                if (isDataFetched)
                    source = "PTITRemoteConfig";
                else if (hasCached)
                    source = "PlayerPrefs (cached)";
                else
                    source = "Default";

                Debug.Log($"[RemoteConfig] key={key} | value={ToString()} | source={source} | defaultValue={defaultValue}");

                PlayerPrefs.SetString(key, ToString());
            }
            else
            {
                value = defaultValue;
                Debug.Log($"[RemoteConfig] key={key} | value={value} | source=Default (fetch=false)");
            }
        }

        private static TValue ParseValue<TValue>(string serializedValue, TValue fallbackValue)
        {
            if (string.IsNullOrEmpty(serializedValue)) return fallbackValue;
            try
            {
                switch (Type.GetTypeCode(typeof(TValue)))
                {
                    case TypeCode.String:
                        return (TValue)Convert.ChangeType(serializedValue, typeof(TValue));
                    case TypeCode.Int32:
                        return (TValue)Convert.ChangeType(Convert.ToInt32(serializedValue), typeof(TValue));
                    case TypeCode.Single:
                        return (TValue)Convert.ChangeType(Convert.ToSingle(serializedValue), typeof(TValue));
                    case TypeCode.Double:
                        return (TValue)Convert.ChangeType(Convert.ToDouble(serializedValue), typeof(TValue));
                    case TypeCode.Boolean:
                        return (TValue)Convert.ChangeType(Convert.ToBoolean(serializedValue), typeof(TValue));
                    default:
                        TValue parsed = JsonUtility.FromJson<TValue>(serializedValue);
                        return parsed == null ? fallbackValue : parsed;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return fallbackValue;
            }
        }

        public string ToString(bool prettyPrint = false)
        {
            if (Application.isPlaying)
            {
                return Type.GetTypeCode(typeof(T)) switch
                {
                    TypeCode.String => value != null ? value.ToString() : "",
                    TypeCode.Int32 => value.ToString(),
                    TypeCode.Single => value.ToString(),
                    TypeCode.Double => value.ToString(),
                    TypeCode.Boolean => value.ToString(),
                    _ => value != null ? ((PrintableConfig)(object)value).ToString() : ""
                };
            }
            else
            {
                return Type.GetTypeCode(typeof(T)) switch
                {
                    TypeCode.String => defaultValue != null ? defaultValue.ToString() : "",
                    TypeCode.Int32 => defaultValue.ToString(),
                    TypeCode.Single => defaultValue.ToString(),
                    TypeCode.Double => defaultValue.ToString(),
                    TypeCode.Boolean => defaultValue.ToString(),
                    _ => defaultValue != null ? ((PrintableConfig)(object)defaultValue).ToString() : ""
                };
            }
        }
    }

    public class RemoteConfigCollectionBase<T> : ScriptableObject where T : ScriptableObject
    {
        public bool Fetched => _fetched;
        private bool _fetched;
        private List<Action> _onFetchCompleteCallbacks = new List<Action>();
        public void Init()
        {
            // Get all properties of the RemoteConfigCollection class
            var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Check if the property type implements IRemoteConfigEntry
                if (typeof(IRemoteConfigEntry).IsAssignableFrom(field.FieldType))
                {
                    // Get the value of the property (the IRemoteConfigEntry instance)
                    var remoteConfigEntry = field.GetValue(this) as IRemoteConfigEntry;

                    // Call Fetch on the IRemoteConfigEntry instance
                    remoteConfigEntry?.Fetch();
                }
            }
            _fetched = true;
            // Invoke all callbacks after fetching is complete
            foreach (var callback in _onFetchCompleteCallbacks)
            {
                callback?.Invoke();
            }
            _onFetchCompleteCallbacks.Clear();
            PlayerPrefs.Save();
        }

        public void RegisterOnFetchComplete(Action callback)
        {
            _onFetchCompleteCallbacks.Add(callback);
        }
    }
}