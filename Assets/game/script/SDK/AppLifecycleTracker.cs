using UnityEngine;
using PTITGameSDK.Modules;
using PTITGameSDK.Core;

public class AppLifecycleTracker : MonoBehaviour
{
    private long sessionStartTime;

    private void Awake()
    {
        // Giữ cho object này sống xuyên suốt mọi màn hình game
        DontDestroyOnLoad(gameObject);

        // Kiểm tra xem đây có phải là lần mở app đầu tiên không
        if (!PlayerPrefs.HasKey("HasOpenedBefore"))
        {
            PlayerPrefs.SetInt("HasOpenedBefore", 1);
            PlayerPrefs.Save();
            
            // Lấy toàn bộ thông số phần cứng của người chơi
            AudienceInforEvent.CreateFirstOpenEvent()
                .SetLocation("Unknown", "Unknown", "Unknown", "Unknown") // Location cần API riêng, tạm để Unknown
                .SetDevice(
                    SystemInfo.deviceType.ToString(),
                    SystemInfo.deviceModel, 
                    SystemInfo.deviceModel, 
                    SystemInfo.operatingSystem, 
                    SystemInfo.operatingSystem, 
                    Application.platform.ToString(), 
                    Application.systemLanguage.ToString()
                ).Track();
        }
    }

    private void Start()
    {
        // Ghi nhận giây phút bắt đầu phiên chơi
        sessionStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        AudienceTrackEvent.CreateStartEvent(sessionStartTime).Track();
    }

    private void OnApplicationQuit()
    {
        // Tính toán tổng thời gian online của phiên chơi này khi tắt app
        long endTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float duration = (endTime - sessionStartTime) / 1000f;
        
        AudienceTrackEvent.CreateEndEvent(sessionStartTime, endTime, duration).Track();
        
        // Bắt buộc đẩy log đang kẹt trong bộ nhớ lên server trước khi app bị đóng hẳn
        if (PTITGameTracker.Instance != null)
        {
            PTITGameTracker.Instance.FlushLogs();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Đẩy log lên server ngay khi người chơi ẩn game ra nền (Background)
        if (pauseStatus && PTITGameTracker.Instance != null)
        {
            PTITGameTracker.Instance.FlushLogs();
        }
    }
}