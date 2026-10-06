using PTITGameSDK.Core;

namespace PTITGameSDK.Modules
{
    public class HardCurrencyEvent : BaseTrackEvent<HardCurrencyEvent>
    {
        private HardCurrencyEvent() : base("hard_currency") { }

        public static HardCurrencyEvent Create(string actionType, string actionName, string currency, int value, string groupPlacement, string placement, string attempId = null, string levelId = null)
        {
            return new HardCurrencyEvent()
                .SetParameter("action_type", actionType)
                .SetParameter("action_name", actionName)
                .SetParameter("currency", currency)
                .SetParameter("value", value)
                .SetParameter("group_placement", groupPlacement)
                .SetParameter("placement", placement)
                .SetParameter("attemp_id", attempId)
                .SetParameter("level_id", levelId);
        }
    }

    public class SoftCurrencyEvent : BaseTrackEvent<SoftCurrencyEvent>
    {
        private SoftCurrencyEvent() : base("soft_currency") { }

        public static SoftCurrencyEvent Create(string actionName, string currency, int value, string placement, string attempId = null, string levelId = null)
        {
            return new SoftCurrencyEvent()
                .SetParameter("action_name", actionName)
                .SetParameter("currency", currency)
                .SetParameter("value", value)
                .SetParameter("placement", placement)
                .SetParameter("attemp_id", attempId)
                .SetParameter("level_id", levelId);
        }

         public SoftCurrencyEvent SetLevelContext(string attempId, string levelId)
        {
            return SetParameter("attemp_id", attempId)
                  .SetParameter("level_id", levelId);
        }

    }

    public class BoosterAfterWinEvent : BaseTrackEvent<BoosterAfterWinEvent>
    {
        private BoosterAfterWinEvent() : base("booster_after_win_level") { }

        public static BoosterAfterWinEvent Create(string levelId, string boosterName, int quantity, string receiveBoosterType)
        {
            return new BoosterAfterWinEvent()
                .SetParameter("level_id", levelId)
                .SetParameter("booster_name", boosterName)
                .SetParameter("quantity", quantity)
                .SetParameter("receive_booster_type", receiveBoosterType);
        }
    }

    public class BoosterOutLevelEvent : BaseTrackEvent<BoosterOutLevelEvent>
    {
        private BoosterOutLevelEvent() : base("booster_track_out_level") { }

        public static BoosterOutLevelEvent Create(string actionName, string boosterName, int quantity, string placement)
        {
            return new BoosterOutLevelEvent()
                .SetParameter("action_name", actionName)
                .SetParameter("booster_name", boosterName)
                .SetParameter("quantity", quantity)
                .SetParameter("placement", placement);
        }
    }
}
