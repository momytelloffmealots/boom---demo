using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GeckoArrow.Runtime.CoreSystem;

namespace PTITGameSDK.Modules.RemoteConfig
{
    [CreateAssetMenu(fileName = "PTITRemoteConfigCollection", menuName = "PTITGameSDK/RemoteConfigCollection")]
    public class PTITRemoteConfigCollection : RemoteConfigCollectionBase<PTITRemoteConfigCollection>
    {
        public RemoteConfigEntry<int> interCoolDown;
        public RemoteConfigEntry<InterAdsConfig> interAdsConfig;
        public RemoteConfigEntry<int> noAdsShowLevel;
        public RemoteConfigEntry<int> miniumLevelsForBanner;
        public RemoteConfigEntry<int> rateUsShowLevel;
        public RemoteConfigEntry<int> rateUsLevelCooldown;
        public RemoteConfigEntry<int> handcraftedLevelActive;
        public RemoteConfigEntry<int> freeToPlayLevel;
        public RemoteConfigEntry<int> noReturnToMenuLevel;
        public RemoteConfigEntry<int> challengeLevelShow;
        public RemoteConfigEntry<int> setLevel;
        public RemoteConfigEntry<int> arrowType;
        public RemoteConfigEntry<UserAttributionConfig> userAttributionConfig;
        public RemoteConfigEntry<bool> timerEnabled;
        public RemoteConfigEntry<bool> monochromeImage;
        public RemoteConfigEntry<bool> bannerEnable;
        public RemoteConfigEntry<bool> darkModeDefault;
        public RemoteConfigEntry<string> boosterLevelShow;
        public RemoteConfigEntry<int> batchLogCount;
        public RemoteConfigEntry<string> variant;
        public RemoteConfigEntry<string> tutorialConfigs;

        public bool HasNonFetchingFields()
        {
            var fields = this.GetType().GetFields()
                .Where(f => f.FieldType.IsGenericType &&
                            f.FieldType.GetGenericTypeDefinition() == typeof(RemoteConfigEntry<>));

            foreach (var field in fields)
            {
                var entry = field.GetValue(this);
                if (entry == null) continue;
                var fetchField = entry.GetType().GetField("fetch");
                if (fetchField != null && !(bool)fetchField.GetValue(entry))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetNonFetchingFieldsMessage()
        {
            var fields = this.GetType().GetFields()
                .Where(f => f.FieldType.IsGenericType &&
                            f.FieldType.GetGenericTypeDefinition() == typeof(RemoteConfigEntry<>));

            var nonFetchingFields = new List<string>();

            foreach (var field in fields)
            {
                var entry = field.GetValue(this);
                if (entry == null) continue;

                var fetchField = entry.GetType().GetField("fetch");
                if (fetchField != null && !(bool)fetchField.GetValue(entry))
                {
                    nonFetchingFields.Add(field.Name);
                }
            }

            return $"Fields not set to fetch: {string.Join(", ", nonFetchingFields)}";
        }
    }

    [System.Serializable]
    public class InterAdsConfig : PrintableConfig
    {
        public int minWinLevel;
        public int minLoseLevel;
        public int minReplayLevel;
    }

    [System.Serializable]
    public class UserAttributionConfig : PrintableConfig
    {
        public UserAttribution blendedCampaign;
        public UserAttribution adsCampaign;
        public UserAttribution purchasedUsers;
        public UserAttribution IAPUsers;

        public UserAttribution GetUserAttribution(UserAttributionType attributionType)
        {
            switch (attributionType)
            {
                case UserAttributionType.BlendedCampaign:
                    return blendedCampaign;
                case UserAttributionType.AdsCampaign:
                    return adsCampaign;
                case UserAttributionType.PurchasedUsers:
                    return purchasedUsers;
                case UserAttributionType.IAPCampaign:
                    return IAPUsers;
            }

            return null;
        }
    }

    [System.Serializable]
    public class UserAttribution
    {
        public int id;
        public string campName;

        // Level gates for showing ad placements (-1 to disable)
        public int banner_lv;                 // Level to enable banner
        public int inter_lv;                  // Level to enable interstitial
        public int inter_retry_lv;            // Level to enable interstitial when retry

        // Cooldowns and rewards
        public int inter_cd;                  // Interstitial cooldown in seconds

        // Levels where rewarded ads become available (-1 to disable)
        public int reward_lives_lv;           // Rewarded ad for extra lives
        public int reward_endgame_lv;         // Rewarded ad for x2 endgame reward
        public int reward_booster_lv;         // Rewarded ad for booster
        public int reward_revive_lv;          // Rewarded ad for revive

        // Coin rewards
        public int coin_endgame_reward;       // Coins rewarded at endgame
        public int reward_coin_lv;            // Level to open coin rewarded ad
        public int reward_coin;               // Coin amount from rewarded ad

        // Daily caps per reward type (-1 for unlimited)
        public int max_reward_lives_lv;       // Max times to watch lives per day
        public int max_reward_endgame_lv;     // Max times to watch endgame x2 per day
        public int max_reward_booster_lv;     // Max times to watch booster per day
        public int max_reward_revive_lv;      // Max times to watch revive per day
        public int max_reward_coin_lv;        // Max times to watch coin per day

        // Attribution migration days (-1 or 0 for N/A)
        public int day_blended_to_ads;        // Days without IAP to switch from blended to ads
        public int day_iap_to_ads;            // Days without IAP to switch from IAP to ads

        // Booster purchasing behavior
        public int booster_purchase_amount;   // Number of boosters bought at once
        public int booster_price_rate;        // Price multiplier per purchase
    }

    public enum UserAttributionType
    {
        BlendedCampaign,
        AdsCampaign,
        PurchasedUsers,
        IAPCampaign
    }
}
