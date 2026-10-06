using PTITGameSDK.Core;
using UnityEngine;

namespace PTITGameSDK.Modules
{
    public class AudienceInforEvent : BaseTrackEvent<AudienceInforEvent>
    {
        private AudienceInforEvent() : base("audience_infor")
        {
            SetParameter("action_name", "first_open");
        }

        public static AudienceInforEvent CreateFirstOpenEvent()
        {
            return new AudienceInforEvent();
        }

        // Location data (typically JSON object, here we use stringified JSON or separate keys. 
        // We will stringify it based on the requirement)
        public AudienceInforEvent SetLocation(string city, string country, string continent, string region)
        {
            var locJson = $"{{\"city\":\"{city}\",\"country\":\"{country}\",\"continent\":\"{continent}\",\"region\":\"{region}\"}}";
            return SetParameter("location", locJson);
        }

        public AudienceInforEvent SetDevice(string category, string brand, string model, string os, string osVersion, string platform, string locale)
        {
            var devJson = $"{{\"category\":\"{category}\",\"mobile_brand_name\":\"{brand}\",\"mobile_model_name\":\"{model}\",\"operating_system\":\"{os}\",\"operating_system_version\":\"{osVersion}\",\"platform\":\"{platform}\",\"locale\":\"{locale}\"}}";
            return SetParameter("device", devJson);
        }
    }

    public class AudienceTrackEvent : BaseTrackEvent<AudienceTrackEvent>
    {
        private AudienceTrackEvent() : base("audience_track") { }

        public static AudienceTrackEvent CreateStartEvent(long startTime)
        {
            return new AudienceTrackEvent()
                .SetParameter("action_name", "start")
                .SetParameter("start_time", startTime)
                .SetParameter("end_time", 0L)
                .SetParameter("duration", 0f);
                
        }

        public static AudienceTrackEvent CreateEndEvent(long startTime, long endTime, float duration)
        {
            return new AudienceTrackEvent()
                .SetParameter("action_name", "end")
                .SetParameter("start_time", startTime)
                .SetParameter("end_time", endTime)
                .SetParameter("duration", duration);
        }
    }
}
