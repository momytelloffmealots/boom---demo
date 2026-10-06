using PTITGameSDK.Core;

namespace PTITGameSDK.Modules
{
    public class UITrackEvent : BaseTrackEvent<UITrackEvent>
    {
        private UITrackEvent() : base("ui_track") { }

        public static UITrackEvent Create(string placement, string buttonName, string actionName = "click")
        {
            return new UITrackEvent()
                .SetParameter("action_name", actionName)
                .SetParameter("placement", placement)
                .SetParameter("button_name", buttonName);
        }
    }
}
