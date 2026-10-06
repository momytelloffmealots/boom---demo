using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.Events;
using PTITGameSDK.Core;

namespace PTITGameSDK.Modules.Ads
{
    public enum AdMediationType
    {
        AppLovinMax,
        UnityLevelPlay
    }

    public class PTITAdManager : MonoBehaviour
    {
        private static PTITAdManager _instance;
        public static PTITAdManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PTITAdManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("PTITAdManager");
                        _instance = go.AddComponent<PTITAdManager>();
                    }
                }
                return _instance;
            }
        }

        public AdMediationType activeMediation = AdMediationType.UnityLevelPlay;

        [Header("LevelPlay Keys")]
        public string levelPlayAndroidAppKey = "2537b6e05";
        public string levelPlayIosAppKey = "";
        public string levelPlayBannerAdUnitId = "r61p4bwsm6lh2b5v";
        public string levelPlayInterAdUnitId = "c3vdhf86xbk6ew8v";
        public string levelPlayRewardAdUnitId = "r93i0tkbo5hgafkc";

        [Header("AppLovin Max Keys")]
        public string maxSdkKey = "M4GLwqezVT2WDo75OWFGOV873pVg6-3S3Kpz8Rxe_-9CnHI9oXPB2TI5LpnRnqvr8hpH8kw7i4KTMcc891KCad";
        public string maxInterAdUnitIdAndroid = "b799c22507494fdc";
        public string maxRewardAdUnitIdAndroid = "4158fdbf4cdf71ca";
        public string maxBannerAdUnitIdAndroid = "fd698de48d43c87d";
        public string maxMrecAdUnitIdAndroid = "ebee917f2f7cda6c";
        
        public string maxInterAdUnitIdIOS = "ENTER_IOS_INTERSTITIAL_AD_UNIT_ID_HERE";
        public string maxRewardAdUnitIdIOS = "ENTER_IOS_REWARD_AD_UNIT_ID_HERE";
        public string maxBannerAdUnitIdIOS = "ENTER_IOS_BANNER_AD_UNIT_ID_HERE";
        public string maxMrecAdUnitIdIOS = "ENTER_IOS_MREC_AD_UNIT_ID_HERE";

        [HideInInspector] public bool isDisableAds = false;
        [HideInInspector] public bool noAds = false;
        public float delayTimer = -1;
        public float cdInterAds = 90;
        private bool isFirstOpen = true;

        // LevelPlay specifics
        private LevelPlayBannerAd bannerAd;
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayRewardedAd rewardedVideoAd;

        // AppLovin MAX specifics
        private bool isMaxSdkInitialized = false;
        private bool isMaxSdkInitializing = false;
        private bool isBannerShowing = false;
        private bool isMRecShowing = false;
        
        private string CurrentMaxInterAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? maxInterAdUnitIdIOS : maxInterAdUnitIdAndroid;
        private string CurrentMaxRewardAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? maxRewardAdUnitIdIOS : maxRewardAdUnitIdAndroid;
        private string CurrentMaxBannerAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? maxBannerAdUnitIdIOS : maxBannerAdUnitIdAndroid;
        private string CurrentMaxMRecAdUnitId => Application.platform == RuntimePlatform.IPhonePlayer ? maxMrecAdUnitIdIOS : maxMrecAdUnitIdAndroid;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitSDK();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitSDK()
        {
            isDisableAds = PlayerPrefs.GetInt("ads_jelly", 1) == 0 || noAds;

            if (activeMediation == AdMediationType.AppLovinMax)
            {
                SetupAppLovinMax();
            }
            else if (activeMediation == AdMediationType.UnityLevelPlay)
            {
                SetupLevelPlay();
            }
        }

        #region AppLovin MAX Setup

        private void SetupAppLovinMax()
        {
            if (isMaxSdkInitializing || isMaxSdkInitialized) return;
            isMaxSdkInitializing = true;
            
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("[Ads] MAX SDK Initialized");
                isMaxSdkInitialized = true;
                isMaxSdkInitializing = false;

                InitializeMaxInterstitialAds();
                InitializeMaxRewardedAds();
                InitializeMaxBannerAds();
                InitializeMaxMRecAds();
            };

            MaxSdk.SetSdkKey(maxSdkKey);
            MaxSdk.InitializeSdk();
        }

        private void InitializeMaxInterstitialAds()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adUnitId, adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", adUnitId, "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", CurrentMaxInterAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadInterstitial(CurrentMaxInterAdUnitId);
            };
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => { 
                AdTrackEvent.Create("show_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", CurrentMaxInterAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadInterstitial(CurrentMaxInterAdUnitId);
                var callback = onInterstitialComplete;
                onInterstitialComplete = null;
                callback?.Invoke();
            };
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adUnitId, adInfo) => { 
                Time.timeScale = 0; 
                AdTrackEvent.Create("show").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (adUnitId, adInfo) => { 
                AdTrackEvent.Create("click").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adUnitId, adInfo) => { 
                Time.timeScale = 1;
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", CurrentMaxInterAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadInterstitial(CurrentMaxInterAdUnitId);
                var callback = onInterstitialComplete;
                onInterstitialComplete = null;
                callback?.Invoke();
            };
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += TrackMaxAdRevenue;
            AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", CurrentMaxInterAdUnitId, "Unknown", "gameplay").Track();
            MaxSdk.LoadInterstitial(CurrentMaxInterAdUnitId);
        }

        private void InitializeMaxRewardedAds()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adUnitId, adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", adUnitId, "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", CurrentMaxRewardAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadRewardedAd(CurrentMaxRewardAdUnitId);
            };
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => { 
                AdTrackEvent.Create("show_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", CurrentMaxRewardAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadRewardedAd(CurrentMaxRewardAdUnitId);
                var callback = onRewardedSuccess;
                onRewardedSuccess = null;
                callback?.Invoke();
                OnRewardedAdClosed();
            };
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adUnitId, adInfo) => { 
                Time.timeScale = 0; 
                AdTrackEvent.Create("show").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (adUnitId, adInfo) => { 
                AdTrackEvent.Create("click").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adUnitId, reward, adInfo) => {
                var callback = onRewardedSuccess;
                onRewardedSuccess = null;
                callback?.Invoke();
            };
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adUnitId, adInfo) => {
                Time.timeScale = 1;
                AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", CurrentMaxRewardAdUnitId, "Unknown", "gameplay").Track();
                MaxSdk.LoadRewardedAd(CurrentMaxRewardAdUnitId);
                OnRewardedAdClosed();
            };
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += TrackMaxAdRevenue;
            AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", CurrentMaxRewardAdUnitId, "Unknown", "gameplay").Track();
            MaxSdk.LoadRewardedAd(CurrentMaxRewardAdUnitId);
        }

        private void InitializeMaxBannerAds()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adUnitId, adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", adUnitId, "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
            };
            AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", CurrentMaxBannerAdUnitId, "Unknown", "gameplay").Track();
            MaxSdk.CreateBanner(CurrentMaxBannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(CurrentMaxBannerAdUnitId, Color.clear);
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += TrackMaxAdRevenue;
        }

        private void InitializeMaxMRecAds()
        {
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += (adUnitId, adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "mrec", adUnitId, adInfo != null ? adInfo.NetworkName : "Unknown", "gameplay").Track();
            };
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "mrec", adUnitId, "Unknown", "gameplay").SetErrorInfo(errorInfo != null ? errorInfo.MediatedNetworkErrorMessage : string.Empty, errorInfo != null ? errorInfo.Message : string.Empty).Track();
            };
            AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "mrec", CurrentMaxMRecAdUnitId, "Unknown", "gameplay").Track();
            MaxSdk.CreateMRec(CurrentMaxMRecAdUnitId, MaxSdkBase.AdViewPosition.BottomCenter);
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += TrackMaxAdRevenue;
        }

        private void TrackMaxAdRevenue(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null || adInfo.Revenue < 0) return;
            Debug.Log("[PTITAdManager] Max Ad Revenue Paid: " + adInfo.Revenue);
            
            // Gửi trực tiếp (bypass batch) - dùng để thống kê doanh thu realtime
            AdImpressionEvent.Create()
                .SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, adInfo.AdFormat, adInfo.AdUnitIdentifier, adInfo.NetworkName, string.Empty)
                .SetLTV((float)adInfo.Revenue)
                .SetCreativeInfo(string.Empty, adInfo.CreativeIdentifier)
                .Track();

            // AnhVT 120826 Gửi theo batch (ad_track, action_name=impression) - dùng để double check với ad_impression
            AdTrackEvent.Create("impression")
                .SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, adInfo.AdFormat, adInfo.AdUnitIdentifier, adInfo.NetworkName, string.Empty)
                .SetLTV((float)adInfo.Revenue)
                .Track();
        }

        #endregion

        #region Unity LevelPlay Setup

        private void SetupLevelPlay()
        {
#if UNITY_ANDROID
            string appKey = levelPlayAndroidAppKey;
#elif UNITY_IOS
            string appKey = levelPlayIosAppKey;
#else
            string appKey = levelPlayAndroidAppKey;
#endif

            if (string.IsNullOrEmpty(appKey))
            {
                Debug.LogWarning("[LevelPlay] App Key is empty!");
                return;
            }

            Debug.Log($"Initializing LevelPlay SDK with Key: {appKey}");
            
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += (error) => { Debug.LogWarning("LevelPlay Init Failed: " + error); };
            LevelPlay.OnImpressionDataReady += TrackLevelPlayAdRevenue;
            
            LevelPlay.Init(appKey);
        }

        private void TrackLevelPlayAdRevenue(LevelPlayImpressionData impressionData)
        {
            if (impressionData == null || !impressionData.Revenue.HasValue || impressionData.Revenue.Value < 0) return;
            Debug.Log("[PTITAdManager] LevelPlay Ad Revenue Paid: " + impressionData.Revenue.Value);

            // Gửi trực tiếp (bypass batch) - dùng để thống kê doanh thu realtime
            AdImpressionEvent.Create()
                .SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, impressionData.AdFormat ?? string.Empty, impressionData.MediationAdUnitName ?? string.Empty, impressionData.AdNetwork ?? string.Empty, string.Empty)
                .SetLTV((float)impressionData.Revenue.Value)
                .SetCreativeInfo(impressionData.AuctionId ?? string.Empty, impressionData.CreativeId ?? string.Empty)
                .Track();

            // AnhVT 120826 Gửi theo batch (ad_track, action_name=impression) - dùng để double check với ad_impression
            AdTrackEvent.Create("impression")
                .SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, impressionData.AdFormat ?? string.Empty, impressionData.MediationAdUnitName ?? string.Empty, impressionData.AdNetwork ?? string.Empty, string.Empty)
                .SetLTV((float)impressionData.Revenue.Value)
                .Track();
        }

        private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("[LevelPlay] SDK Initialized with config: " + config);
            
            // Setup Rewarded
            rewardedVideoAd = new LevelPlayRewardedAd(levelPlayRewardAdUnitId);
            rewardedVideoAd.OnAdLoaded += (adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").Track();
            };
            rewardedVideoAd.OnAdLoadFailed += (error) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
            };
            rewardedVideoAd.OnAdDisplayed += (adInfo) => { 
                Time.timeScale = 0; 
                AdTrackEvent.Create("show").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
            };
            rewardedVideoAd.OnAdClicked += (adInfo) => { 
                AdTrackEvent.Create("click").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
            };
            rewardedVideoAd.OnAdClosed += (adInfo) => {
                Time.timeScale = 1;
                AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
                rewardedVideoAd.LoadAd();
                OnRewardedAdClosed();
            };
            rewardedVideoAd.OnAdRewarded += (adInfo, reward) => {
                var callback = onRewardedSuccess;
                onRewardedSuccess = null;
                callback?.Invoke();
            };
            rewardedVideoAd.OnAdDisplayFailed += (adInfo, error) => {
                Time.timeScale = 1;
                AdTrackEvent.Create("show_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
                rewardedVideoAd.LoadAd();
                var callback = onRewardedSuccess;
                onRewardedSuccess = null;
                callback?.Invoke();
                OnRewardedAdClosed();
            };
            AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
            rewardedVideoAd.LoadAd();

            // Setup Interstitial
            interstitialAd = new LevelPlayInterstitialAd(levelPlayInterAdUnitId);
            interstitialAd.OnAdLoaded += (adInfo) => {
                AdTrackEvent.Create("load").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").Track();
            };
            interstitialAd.OnAdLoadFailed += (error) => {
                AdTrackEvent.Create("load_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
            };
            interstitialAd.OnAdDisplayed += (adInfo) => { 
                Time.timeScale = 0; 
                AdTrackEvent.Create("show").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
            };
            interstitialAd.OnAdClicked += (adInfo) => { 
                AdTrackEvent.Create("click").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
            };
            interstitialAd.OnAdClosed += (adInfo) => { 
                Time.timeScale = 1; 
                AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
                interstitialAd.LoadAd();
                var callback = onInterstitialComplete;
                onInterstitialComplete = null;
                callback?.Invoke();
            };
            interstitialAd.OnAdDisplayFailed += (adInfo, error) => {
                Time.timeScale = 1;
                AdTrackEvent.Create("show_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
                AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
                interstitialAd.LoadAd();
                var callback = onInterstitialComplete;
                onInterstitialComplete = null;
                callback?.Invoke();
            };
            AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
            interstitialAd.LoadAd();
        }
        #endregion

        private void Update()
        {
            if (delayTimer > 0) delayTimer -= Time.deltaTime;
        }

        // ========================== PUBLIC API ==========================

        private UnityAction onInterstitialComplete;

        public void ShowInterstitial(UnityAction onComplete = null, bool ignoreCooldown = false)
        {
            if (isDisableAds || noAds)
            {
                onComplete?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log("[PTITAdManager] (UNITY_EDITOR) Interstitial Ad simulated successfully!");
            onComplete?.Invoke();
            return;
#endif

            bool timerOk = ignoreCooldown || delayTimer <= 0;
            if (!timerOk)
            {
                onComplete?.Invoke();
                return;
            }

            onInterstitialComplete = onComplete;

            if (activeMediation == AdMediationType.AppLovinMax)
            {
                if (isMaxSdkInitialized && MaxSdk.IsInterstitialReady(CurrentMaxInterAdUnitId))
                {
                    delayTimer = cdInterAds;
                    MaxSdk.ShowInterstitial(CurrentMaxInterAdUnitId);
                }
                else
                {
                    AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", CurrentMaxInterAdUnitId, "Unknown", "gameplay").Track();
                    MaxSdk.LoadInterstitial(CurrentMaxInterAdUnitId);
                    onComplete?.Invoke();
                }
            }
            else // LevelPlay
            {
                if (interstitialAd != null && interstitialAd.IsAdReady())
                {
                    delayTimer = cdInterAds;
                    interstitialAd.ShowAd();
                }
                else
                {
                    if (interstitialAd != null)
                    {
                        AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "interstitial", levelPlayInterAdUnitId, "Unknown", "gameplay").Track();
                        interstitialAd.LoadAd();
                    }
                    onComplete?.Invoke();
                }
            }
        }

        public bool isRewardAdsReady()
        {
            if (isDisableAds) return true;
#if UNITY_EDITOR
            return true;
#endif
            if (activeMediation == AdMediationType.AppLovinMax)
            {
                return isMaxSdkInitialized && MaxSdk.IsRewardedAdReady(CurrentMaxRewardAdUnitId);
            }
            return rewardedVideoAd != null && rewardedVideoAd.IsAdReady();
        }

        private UnityAction onRewardedSuccess;
        private UnityAction onRewardedClosed;

        public void ShowRewardedAd(UnityAction onReceiveReward, UnityAction onClosed = null)
        {
            if (isDisableAds || Application.internetReachability == NetworkReachability.NotReachable)
            {
                onReceiveReward?.Invoke();
                onClosed?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log("[PTITAdManager] (UNITY_EDITOR) Rewarded Ad simulated successfully!");
            onReceiveReward?.Invoke();
            onClosed?.Invoke();
            return;
#endif

            onRewardedSuccess = onReceiveReward;
            onRewardedClosed = onClosed;

            if (activeMediation == AdMediationType.AppLovinMax)
            {
                if (isMaxSdkInitialized && MaxSdk.IsRewardedAdReady(CurrentMaxRewardAdUnitId))
                {
                    MaxSdk.ShowRewardedAd(CurrentMaxRewardAdUnitId);
                }
                else
                {
                    onReceiveReward?.Invoke();
                    AdTrackEvent.Create("request").SetBasicInfo("ApplovinMAX", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", CurrentMaxRewardAdUnitId, "Unknown", "gameplay").Track();
                    MaxSdk.LoadRewardedAd(CurrentMaxRewardAdUnitId);
                    OnRewardedAdClosed();
                }
            }
            else // LevelPlay
            {
                if (rewardedVideoAd != null && rewardedVideoAd.IsAdReady())
                {
                    rewardedVideoAd.ShowAd();
                }
                else
                {
                    onReceiveReward?.Invoke();
                    if (rewardedVideoAd != null)
                    {
                        AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "reward", levelPlayRewardAdUnitId, "Unknown", "gameplay").Track();
                        rewardedVideoAd.LoadAd();
                    }
                    OnRewardedAdClosed();
                }
            }
        }

        private void OnRewardedAdClosed()
        {
            var callback = onRewardedClosed;
            onRewardedClosed = null;
            callback?.Invoke();
        }

        public void ToggleBannerVisibility(bool isShowing)
        {
            if (isDisableAds || noAds)
            {
                if (activeMediation == AdMediationType.AppLovinMax) MaxSdk.HideBanner(CurrentMaxBannerAdUnitId);
                else if (bannerAd != null) bannerAd.HideAd();
                return;
            }

            if (activeMediation == AdMediationType.AppLovinMax)
            {
                if (isShowing) MaxSdk.ShowBanner(CurrentMaxBannerAdUnitId);
                else MaxSdk.HideBanner(CurrentMaxBannerAdUnitId);
                isBannerShowing = isShowing;
            }
            else // LevelPlay
            {
                if (isShowing)
                {
                    if (bannerAd == null)
                    {
                        bannerAd = new LevelPlayBannerAd(levelPlayBannerAdUnitId);
                        bannerAd.OnAdLoaded += (adInfo) => {
                            AdTrackEvent.Create("load").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", levelPlayBannerAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").Track();
                        };
                        bannerAd.OnAdLoadFailed += (error) => {
                            AdTrackEvent.Create("load_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", levelPlayBannerAdUnitId, "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
                        };
                        bannerAd.OnAdDisplayed += (adInfo) => {
                            AdTrackEvent.Create("show").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", levelPlayBannerAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").Track();
                        };
                        bannerAd.OnAdDisplayFailed += (adInfo, error) => {
                            AdTrackEvent.Create("show_fail").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", levelPlayBannerAdUnitId, adInfo != null ? adInfo.AdNetwork : "Unknown", "gameplay").SetErrorInfo(string.Empty, error != null ? error.ErrorMessage : string.Empty).Track();
                        };
                    }
                    AdTrackEvent.Create("request").SetBasicInfo("LevelPlay", Application.internetReachability == NetworkReachability.NotReachable ? 0 : 1, "banner", levelPlayBannerAdUnitId, "Unknown", "gameplay").Track();
                    bannerAd.LoadAd();
                    bannerAd.ShowAd();
                }
                else
                {
                    if (bannerAd != null) bannerAd.HideAd();
                }
            }
        }

        public void ToggleMRecVisibility()
        {
            if (activeMediation == AdMediationType.AppLovinMax)
            {
                if (!isMRecShowing && !isDisableAds)
                {
                    MaxSdk.ShowMRec(CurrentMaxMRecAdUnitId);
                }
                else
                {
                    MaxSdk.HideMRec(CurrentMaxMRecAdUnitId);
                }
                isMRecShowing = !isMRecShowing;
            }
        }
    }
}
