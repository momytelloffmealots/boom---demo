using System;
using System.Collections.Generic;

namespace PTITGameSDK.Core
{
    public abstract class BaseTrackEvent<T> where T : BaseTrackEvent<T>
    {
        protected string EventName;
        protected Dictionary<string, object> Parameters = new Dictionary<string, object>();

        protected BaseTrackEvent(string eventName)
        {
            EventName = eventName;
        }

        public T SetParameter(string key, object value)
        {
            if (value != null)
            {
                Parameters[key] = value;
            }
              else
            {
                Parameters[key] = string.Empty;
            }

            return (T)this;
        }

        /// <summary>
        /// Sends the event to all registered tracking providers.
        /// </summary>
        public void Track()
        {
            // Inject Global Metadata automatically
            var tracker = PTITGameTracker.Instance;
            string eventId = Guid.NewGuid().ToString();
            Parameters["event_id"] = eventId;
            Parameters["install_source"] = tracker.InstallSource;
            Parameters["session_id"] = tracker.SessionId;
            Parameters["audience_id"] = tracker.AudienceId;
            Parameters["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // or timestamp string depending on requirement
            Parameters["campaign_name"] = tracker.CampaignName;
            
            // Get Variant directly from Remote Config for A/B Testing, fallback to tracker.Variant
            string variantValue = tracker.Variant;
            try {
                if (Modules.PTITRemoteConfig.Instance != null) {
                    variantValue = Modules.PTITRemoteConfig.Instance.GetString("variant", tracker.Variant);
                }
            } catch {}
            if (string.IsNullOrEmpty(variantValue)) variantValue = "default";
            
            Parameters["variant"] = variantValue;
            Parameters["version"] = tracker.Version;

            PTITGameTracker.Instance.LogEvent(EventName, Parameters);
        }
    }

    public class GenericTrackEvent : BaseTrackEvent<GenericTrackEvent>
    {
        private GenericTrackEvent(string eventName) : base(eventName) { }

        public static GenericTrackEvent Create(string eventName, Dictionary<string, object> parameters)
        {
            var evt = new GenericTrackEvent(eventName);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    evt.SetParameter(kvp.Key, kvp.Value);
                }
            }
            return evt;
        }
    }
}
