using System.Collections.Generic;

namespace PTITGameSDK.Core
{
    /// <summary>
    /// Interface for integrating with analytics providers like Firebase, AppsFlyer, etc.
    /// </summary>
    public interface ITrackingProvider
    {
        void Initialize();
        void LogEvent(string eventName, Dictionary<string, object> parameters);
        void SetUserProperty(string propertyName, string value);
        void Flush();
        void ForceSyncCache();
    }
}
