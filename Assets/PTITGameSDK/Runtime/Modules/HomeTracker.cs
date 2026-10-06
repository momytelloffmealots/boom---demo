using PTITGameSDK.Core;
using System.Collections.Generic;

namespace PTITGameSDK.Modules
{
    public class HomeTrackEvent : BaseTrackEvent<HomeTrackEvent>
    {
        private HomeTrackEvent() : base("home_track") { }

        public static HomeTrackEvent Create(string actionType, string actionName)
        {
            return new HomeTrackEvent()
                .SetParameter("action_type", actionType)
                .SetParameter("action_name", actionName);
        }

        public HomeTrackEvent SetHomeVisitInfo(string homeVisitId, int homeVisitIndex, string entrySource)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("home_visit_index", homeVisitIndex)
                  .SetParameter("entry_source", entrySource);
        }

        public HomeTrackEvent SetClickInfo(string homeVisitId, string placement, float duration)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("placement", placement)
                  .SetParameter("duration", duration);
        }
    }
}
