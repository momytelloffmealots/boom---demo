using PTITGameSDK.Core;
using System.Collections.Generic;

namespace PTITGameSDK.Modules
{
    public class LevelTrackEvent : BaseTrackEvent<LevelTrackEvent>
    {
        private LevelTrackEvent() : base("level_track") { }

        public static LevelTrackEvent Create(string actionName)
        {
            return new LevelTrackEvent()
                .SetParameter("action_name", actionName);
        }

        public LevelTrackEvent SetLevelInfo(string attempId, string levelId, string levelVersion, string levelType = "normal", string mode = "classic")
        {
            return SetParameter("attemp_id", attempId)
                  .SetParameter("level_id", levelId)
                  .SetParameter("level_version", levelVersion)
                  .SetParameter("level_type", levelType)
                  .SetParameter("mode", mode);
        }

        public LevelTrackEvent SetTimeInfo(long startTime, long endTime, float duration)
        {
            return SetParameter("start_time", startTime)
                    .SetParameter("end_time", endTime > 0 ? (object)endTime : 0L)
                  .SetParameter("duration", duration);
        }

        public LevelTrackEvent SetActionType(string actionType)
        {
            return SetParameter("action_type", actionType);
        }

        public LevelTrackEvent SetLoseReason(string loseReason)
        {
            return SetParameter("lose_reason", loseReason);
        }

        public LevelTrackEvent SetUsage(int isUseBooster, int isViewRewardAds)
        {
            return SetParameter("is_use_booster", isUseBooster)
                  .SetParameter("is_view_reward_ads", isViewRewardAds);
        }

        // Helper to set complex JSON string fields
        public LevelTrackEvent SetEventsJson(string eventsJson)
        {
            return SetParameter("events", eventsJson);
        }

        public LevelTrackEvent SetResultJson(string resultJson)
        {
            return SetParameter("result", resultJson);
        }

        public LevelTrackEvent SetHomeReturnInfo(string entrySource, int lastLevel, int lastArrow, float lastLevelDuration, int lastLevelAttempt, float lastLevelProgress, int lastFailStreak, int lastIsUseBooster, int lastBoosterCount, int lastIsViewAds, int homeVisitIndex)
        {
            return SetParameter("entry_source", entrySource)
                  .SetParameter("last_level", lastLevel)
                  .SetParameter("last_arrow", lastArrow)
                  .SetParameter("last_level_duration", lastLevelDuration)
                  .SetParameter("last_level_attempt", lastLevelAttempt)
                  .SetParameter("last_level_progress", lastLevelProgress)
                  .SetParameter("last_fail_streak", lastFailStreak)
                  .SetParameter("last_is_use_booster", lastIsUseBooster)
                  .SetParameter("last_booster_count", lastBoosterCount)
                  .SetParameter("last_is_view_ads", lastIsViewAds)
                  .SetParameter("home_visit_index", homeVisitIndex);
        }
    }
}
