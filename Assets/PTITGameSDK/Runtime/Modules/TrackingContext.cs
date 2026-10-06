using System;
using UnityEngine;

namespace PTITGameSDK.Modules
{
    public static class TrackingContext
    {
        private const string HOME_VISIT_INDEX_KEY = "Tracking_HomeVisitIndex";
        
        public static string HomeVisitId { get; private set; }
        public static int HomeVisitIndex { get; private set; }
        public static string EntrySource { get; set; } = "session_start";
        
        // Caches for tracking parameters when transitioning between scenes
        public static int LastLevel { get; set; }
        public static string LastLevelResult { get; set; } = "quit";
        public static int LastFailStreak { get; set; }
        public static float LastLevelDuration { get; set; }
        public static int LastLevelAttempt { get; set; }
        public static float LastLevelProgress { get; set; }
        public static int LastIsUseBooster { get; set; }
        public static int LastBoosterCount { get; set; }
        public static int LastIsViewAds { get; set; }
        public static int LastArrow { get; set; }

        public static void GenerateNewHomeVisit()
        {
            HomeVisitIndex = PlayerPrefs.GetInt(HOME_VISIT_INDEX_KEY, 0) + 1;
            PlayerPrefs.SetInt(HOME_VISIT_INDEX_KEY, HomeVisitIndex);
            PlayerPrefs.Save();

            string dateString = DateTime.Now.ToString("yyMMdd");
            HomeVisitId = $"{dateString}{HomeVisitIndex:D4}";
        }
    }
}
