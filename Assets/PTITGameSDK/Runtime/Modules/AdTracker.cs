using PTITGameSDK.Core;

namespace PTITGameSDK.Modules
{
    public class AdTrackEvent : BaseTrackEvent<AdTrackEvent>
    {
        private AdTrackEvent() : base("ad_track") { }

        public static AdTrackEvent Create(string actionName)
        {
            return new AdTrackEvent()
                .SetParameter("action_name", actionName); // request, load, load_fail, show, show_fail, click, impression
        }

        public AdTrackEvent SetBasicInfo(string adPlatform, int statusInternet, string adFormat, string adUnitName, string adSource, string placement)
        {
            return SetParameter("ad_platform", adPlatform)
                  .SetParameter("status_internet", statusInternet)
                  .SetParameter("ad_format", adFormat)
                  .SetParameter("ad_unit_name", adUnitName)
                  .SetParameter("ad_source", adSource)
                  .SetParameter("placement", placement);
        }

        public AdTrackEvent SetErrorInfo(string networkErrorInfo, string mediationErrorInfo)
        {
            return SetParameter("network_error_info", networkErrorInfo)
                  .SetParameter("mediation_error_info", mediationErrorInfo);
        }

        public AdTrackEvent SetLTV(float value, string currency = "USD")
        {
            return SetParameter("value", value)
                  .SetParameter("currency", currency);
        }

        public AdTrackEvent SetCreativeInfo(string adId, string creativeId)
        {
            return SetParameter("ad_id", adId)
                  .SetParameter("creative_id", creativeId);
        }
    }

    public class AdImpressionEvent : BaseTrackEvent<AdImpressionEvent>
    {
        private AdImpressionEvent() : base("ad_impression") 
        { 
            SetParameter("action_name", "impression");
        }

        public static AdImpressionEvent Create()
        {
            return new AdImpressionEvent();
        }

        public AdImpressionEvent SetBasicInfo(string adPlatform, int statusInternet, string adFormat, string adUnitName, string adSource, string placement)
        {
            return SetParameter("ad_platform", adPlatform)
                  .SetParameter("status_internet", statusInternet)
                  .SetParameter("ad_format", adFormat)
                  .SetParameter("ad_unit_name", adUnitName)
                  .SetParameter("ad_source", adSource)
                  .SetParameter("placement", placement);
        }

        public AdImpressionEvent SetLTV(float value, string currency = "USD")
        {
            return SetParameter("value", value)
                  .SetParameter("currency", currency);
        }

        public AdImpressionEvent SetCreativeInfo(string adId, string creativeId)
        {
            return SetParameter("ad_id", adId)
                  .SetParameter("creative_id", creativeId);
        }
    }
}
