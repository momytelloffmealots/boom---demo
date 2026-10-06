using PTITGameSDK.Core;

namespace PTITGameSDK.Modules
{
    /// <summary>
    /// Logs the board and three shape slots produced for a gameplay batch.
    /// The payload intentionally uses exactly 19 gameplay parameters so the
    /// six metadata parameters injected by BaseTrackEvent stay within Firebase's limit.
    /// </summary>
    public sealed class BlockBatchSuggestedEvent : BaseTrackEvent<BlockBatchSuggestedEvent>
    {
        private BlockBatchSuggestedEvent() : base("block_batch_suggested") { }

        public static BlockBatchSuggestedEvent Create(long batchId, string suggestionSource, int batchSize)
        {
            return new BlockBatchSuggestedEvent()
                .SetParameter("batch_id", batchId)
                .SetParameter("suggestion_source", suggestionSource)
                .SetParameter("batch_size", batchSize);
        }

        public BlockBatchSuggestedEvent SetBoardInfo(
            string mapState,
            double fillRatio,
            int emptyRegionCount,
            string nearCompleteLines)
        {
            return SetParameter("map_state", mapState)
                .SetParameter("fill_ratio", fillRatio)
                .SetParameter("empty_region_count", emptyRegionCount)
                .SetParameter("near_complete_lines", nearCompleteLines);
        }

        public BlockBatchSuggestedEvent SetShapeInfo(
            int slot,
            string idAndMask,
            string size,
            double complexity,
            int validPositions)
        {
            string prefix = $"shape_{slot}_";
            return SetParameter(prefix + "id_mask", idAndMask)
                .SetParameter(prefix + "size", size)
                .SetParameter(prefix + "complexity", complexity)
                .SetParameter(prefix + "valid_positions", validPositions);
        }
    }

    /// <summary>
    /// Logs the summarized interaction history when all active shapes in a batch are placed.
    /// Sequence values are pre-chunked by the game telemetry coordinator to respect
    /// Firebase's 100-character string parameter limit.
    /// </summary>
    public sealed class BlockBatchCompletedEvent : BaseTrackEvent<BlockBatchCompletedEvent>
    {
        private BlockBatchCompletedEvent() : base("block_batch_completed") { }

        public static BlockBatchCompletedEvent Create(long batchId, int batchSize, double sessionTime)
        {
            return new BlockBatchCompletedEvent()
                .SetParameter("batch_id", batchId)
                .SetParameter("batch_size", batchSize)
                .SetParameter("session_time", sessionTime);
        }

        public BlockBatchCompletedEvent SetMoveCounts(
            int moveAttempt,
            int successCount,
            int failCount,
            int attemptOverflowCount)
        {
            return SetParameter("move_attempt", moveAttempt)
                .SetParameter("success_count", successCount)
                .SetParameter("fail_count", failCount)
                .SetParameter("attempt_overflow_count", attemptOverflowCount);
        }

        public BlockBatchCompletedEvent SetAttemptSequences(
            string moveResults,
            string moveShapeIds1,
            string moveShapeIds2,
            string decisionTime1,
            string decisionTime2,
            string decisionTime3,
            string decisionTime4,
            string dragTime1,
            string dragTime2,
            string dragTime3,
            string dragTime4)
        {
            return SetParameter("move_results", moveResults)
                .SetParameter("move_shape_ids_1", moveShapeIds1)
                .SetParameter("move_shape_ids_2", moveShapeIds2)
                .SetParameter("decision_time_1", decisionTime1)
                .SetParameter("decision_time_2", decisionTime2)
                .SetParameter("decision_time_3", decisionTime3)
                .SetParameter("decision_time_4", decisionTime4)
                .SetParameter("drag_time_1", dragTime1)
                .SetParameter("drag_time_2", dragTime2)
                .SetParameter("drag_time_3", dragTime3)
                .SetParameter("drag_time_4", dragTime4);
        }
    }
}
