using PTITGameSDK.Core;
using System.Collections.Generic;

namespace PTITGameSDK.Modules
{
    public class LuckySpinTrackEvent : BaseTrackEvent<LuckySpinTrackEvent>
    {
        private LuckySpinTrackEvent() : base("lucky_spin_track") { }

        public static LuckySpinTrackEvent Create(string actionType, string actionName)
        {
            return new LuckySpinTrackEvent()
                .SetParameter("action_type", actionType)
                .SetParameter("action_name", actionName);
        }

        public LuckySpinTrackEvent SetShowInfo(string homeVisitId, int freeSpin, int spinCountToday, float cooldown)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("free_spin", freeSpin)
                  .SetParameter("spin_count_today", spinCountToday)
                  .SetParameter("cooldown", cooldown);
        }

        public LuckySpinTrackEvent SetOpenInfo(string homeVisitId, string entrySource, int level, string lastLevelResult, int failStreak, float duration, string spinState)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("entry_source", entrySource)
                  .SetParameter("level", level)
                  .SetParameter("last_level_result", lastLevelResult)
                  .SetParameter("fail_streak", failStreak)
                  .SetParameter("duration", duration)
                  .SetParameter("spin_state", spinState);
        }

        public LuckySpinTrackEvent SetSpinInfo(string spinType, int spinIndex, string source)
        {
            return SetParameter("spin_type", spinType)
                  .SetParameter("spin_index", spinIndex)
                  .SetParameter("source", source);
        }

        public LuckySpinTrackEvent SetRewardInfo(string rewardType, string rewardName, int value, string spinType)
        {
            return SetParameter("reward_type", rewardType)
                  .SetParameter("reward_name", rewardName)
                  .SetParameter("value", value)
                  .SetParameter("spin_type", spinType);
        }

        public LuckySpinTrackEvent SetCloseInfo(int spinCount, float duration)
        {
            return SetParameter("spin_count", spinCount)
                  .SetParameter("duration", duration);
        }
    }
}
