using PTITGameSDK.Core;

namespace PTITGameSDK.Modules
{
    public class IAPTrackEvent : BaseTrackEvent<IAPTrackEvent>
    {
        private IAPTrackEvent() : base("iap_track") { }

        public static IAPTrackEvent Create(string actionName, string placement, string typeShop, string productId)
        {
            return new IAPTrackEvent()
                .SetParameter("action_name", actionName) // show, click_button_iap, purchase_success, purchase_error, click_more
                .SetParameter("placement", placement)
                .SetParameter("type_shop", typeShop)
                .SetParameter("product_id", productId);
        }

        public IAPTrackEvent SetError(string errorMessage)
        {
            return SetParameter("error_message", errorMessage);
        }

        public IAPTrackEvent SetDuration(float duration)
        {
            return SetParameter("duration", duration);
        }
    }
}
