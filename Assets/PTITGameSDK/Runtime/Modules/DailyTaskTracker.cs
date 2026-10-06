using PTITGameSDK.Core;
using System.Collections.Generic;

namespace PTITGameSDK.Modules
{
    public class DailyTaskTrackEvent : BaseTrackEvent<DailyTaskTrackEvent>
    {
        private DailyTaskTrackEvent() : base("daily_task_track") { }

        public static DailyTaskTrackEvent Create(string actionType, string actionName)
        {
            return new DailyTaskTrackEvent()
                .SetParameter("action_type", actionType)
                .SetParameter("action_name", actionName);
        }

        public DailyTaskTrackEvent SetShowInfo(string homeVisitId, string taskState, int totalTask, int completedTask, int claimableTask)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("task_state", taskState)
                  .SetParameter("total_task", totalTask)
                  .SetParameter("completed_task", completedTask)
                  .SetParameter("claimable_task", claimableTask);
        }

        public DailyTaskTrackEvent SetOpenInfo(string homeVisitId, string entrySource, int lastLevel, string lastLevelState, float lastLevelProgress, int lastFailStreak, float duration, int completedTask, int claimableTask)
        {
            return SetParameter("home_visit_id", homeVisitId)
                  .SetParameter("entry_source", entrySource)
                  .SetParameter("last_level", lastLevel)
                  .SetParameter("last_level_state", lastLevelState)
                  .SetParameter("last_level_progress", lastLevelProgress)
                  .SetParameter("last_fail_streak", lastFailStreak)
                  .SetParameter("duration", duration)
                  .SetParameter("completed_task", completedTask)
                  .SetParameter("claimable_task", claimableTask);
        }

        public DailyTaskTrackEvent SetProgressInfo(string taskId, string taskType, int taskTarget, int taskProgress, string taskStatus)
        {
            return SetParameter("task_id", taskId)
                  .SetParameter("task_type", taskType)
                  .SetParameter("task_target", taskTarget)
                  .SetParameter("task_progress", taskProgress)
                  .SetParameter("task_status", taskStatus);
        }

        public DailyTaskTrackEvent SetCompleteInfo(string taskId, string taskType, int taskTarget, int taskProgress)
        {
            return SetParameter("task_id", taskId)
                  .SetParameter("task_type", taskType)
                  .SetParameter("task_target", taskTarget)
                  .SetParameter("task_progress", taskProgress);
        }

        public DailyTaskTrackEvent SetClaimInfo(string taskId, string taskType, string rewardType, string rewardName, int value)
        {
            return SetParameter("task_id", taskId)
                  .SetParameter("task_type", taskType)
                  .SetParameter("reward_type", rewardType)
                  .SetParameter("reward_name", rewardName)
                  .SetParameter("value", value);
        }

        public DailyTaskTrackEvent SetMilestoneClaimInfo(string milestoneId, int currentPoint, int targetPoint, string rewardType, string rewardName, int value)
        {
            return SetParameter("milestone_id", milestoneId)
                  .SetParameter("current_point", currentPoint)
                  .SetParameter("target_point", targetPoint)
                  .SetParameter("reward_type", rewardType)
                  .SetParameter("reward_name", rewardName)
                  .SetParameter("value", value);
        }

        public DailyTaskTrackEvent SetCloseInfo(float duration, int claimCount, int taskCompletedCount)
        {
            return SetParameter("duration", duration)
                  .SetParameter("claim_count", claimCount)
                  .SetParameter("task_completed_count", taskCompletedCount);
        }
    }
}
