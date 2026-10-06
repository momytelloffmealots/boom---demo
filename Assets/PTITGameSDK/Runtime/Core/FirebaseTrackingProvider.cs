using System;
using System.Collections.Generic;
using Firebase.Analytics;

namespace PTITGameSDK.Core
{
    public class FirebaseTrackingProvider : ITrackingProvider
    {
        public void Initialize()
        {
            // Usually, Firebase initialization is handled globally in the app.
            // But if you want to explicitly check dependencies, it can be done here.
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }

            var firebaseParams = new Parameter[parameters.Count];
            int i = 0;
            foreach (var kvp in parameters)
            {
                if (kvp.Key == "events" || kvp.Key == "result" || kvp.Key == "device")
                {
                    // Loại bỏ chuỗi events quá dài cho Firebase (giới hạn 100 char), thay bằng mảng rỗng
                    firebaseParams[i] = new Parameter(kvp.Key, "[]");
                }
                else if (kvp.Value is int intValue)
                    firebaseParams[i] = new Parameter(kvp.Key, intValue);
                else if (kvp.Value is long longValue)
                    firebaseParams[i] = new Parameter(kvp.Key, longValue);
                else if (kvp.Value is double doubleValue)
                    firebaseParams[i] = new Parameter(kvp.Key, doubleValue);
                else if (kvp.Value is float floatValue)
                    firebaseParams[i] = new Parameter(kvp.Key, floatValue);
                else
                {
                    string strValue = kvp.Value?.ToString() ?? string.Empty;
                    // Bắt buộc chuyển đổi key "value" sang dạng số (double) để Firebase hiểu đây là doanh thu
                    if (kvp.Key == "value" && double.TryParse(strValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedDouble))
                    {
                        firebaseParams[i] = new Parameter(kvp.Key, parsedDouble);
                    }
                    else
                    {
                        firebaseParams[i] = new Parameter(kvp.Key, strValue);
                    }
                }
                i++;
            }

            FirebaseAnalytics.LogEvent(eventName, firebaseParams);
        }

        public void SetUserProperty(string propertyName, string value)
        {
            FirebaseAnalytics.SetUserProperty(propertyName, value);
        }

        public void Flush()
        {
            // Firebase Analytics SDK handles its own flushing automatically.
            // There's no public manual flush method needed or available in Unity SDK.
        }

        public void ForceSyncCache()
        {
            // Handled internally by Firebase SDK
        }
    }
}
