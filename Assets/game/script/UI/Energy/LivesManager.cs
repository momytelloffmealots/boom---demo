using System;
using UnityEngine;

public class LivesManager : MonoBehaviour
{
    public static LivesManager Instance;

    [Header("Cấu hình Mạng")]
    public int maxLives = 5;
    public int timeToRecoverMinutes = 30; // 30 phút hồi 1 mạng

    private int currentLives;
    private DateTime nextLifeTime;

    // Loa phát thanh báo cho UI biết số thay đổi
    public event Action<int, string> OnLivesUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadLives(); // Đọc thẻ nhớ ngay khi mở game
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Nếu chưa đầy mạng, liên tục tính thời gian
        if (currentLives < maxLives)
        {
            TimeSpan timeRemaining = nextLifeTime - DateTime.Now;

            // Nếu đếm ngược về 0 (hoặc âm) -> Hồi 1 mạng
            if (timeRemaining.TotalSeconds <= 0)
            {
                currentLives++;
                if (currentLives < maxLives)
                {
                    // Đặt lại thời gian cho mạng tiếp theo (cộng thêm số giây bị lố cho chuẩn)
                    nextLifeTime = DateTime.Now.AddMinutes(timeToRecoverMinutes).AddSeconds(timeRemaining.TotalSeconds);
                }
                SaveLives();
            }
            UpdateUI();
        }
    }

    // Hàm gọi khi Thua game
    public void LoseLife()
    {
        if (currentLives == maxLives)
        {
            // Nếu đang full mà bị trừ, bắt đầu đếm 30 phút
            nextLifeTime = DateTime.Now.AddMinutes(timeToRecoverMinutes);
        }

        if (currentLives > 0)
        {
            currentLives--;
            SaveLives();
        }
    }

    public int GetCurrentLives() => currentLives;

    // 🔥 MỚI: Hàm nạp đầy 5 mạng lập tức
    public void RefillAllLives()
    {
        currentLives = maxLives;
        SaveLives(); // Hàm SaveLives đã có sẵn logic tự động xóa đếm ngược và báo UI hiển thị "Full"
    }

    // Phát loa thông báo ngay lập tức (Dùng khi UI vừa bật lên)
    public void ForceUpdateUI()
    {
        UpdateUI();
    }

    private void LoadLives()
    {
        currentLives = PlayerPrefs.GetInt("CurrentLives", maxLives);
        string timeString = PlayerPrefs.GetString("NextLifeTime", "");

        if (currentLives < maxLives && !string.IsNullOrEmpty(timeString))
        {
            nextLifeTime = DateTime.Parse(timeString);

            // TÍNH TOÁN THỜI GIAN OFFLINE: Tắt máy đi ngủ sáng dậy tự cộng mạng
            while (currentLives < maxLives && DateTime.Now >= nextLifeTime)
            {
                currentLives++;
                if (currentLives < maxLives)
                {
                    nextLifeTime = nextLifeTime.AddMinutes(timeToRecoverMinutes);
                }
            }
        }
        SaveLives();
    }

    private void SaveLives()
    {
        PlayerPrefs.SetInt("CurrentLives", currentLives);
        if (currentLives < maxLives)
            PlayerPrefs.SetString("NextLifeTime", nextLifeTime.ToString());
        else
            PlayerPrefs.SetString("NextLifeTime", "");

        PlayerPrefs.Save();
        UpdateUI();
    }

    private void UpdateUI()
    {
        string timeStr = "Full";
        if (currentLives < maxLives)
        {
            TimeSpan timeRemaining = nextLifeTime - DateTime.Now;
            if (timeRemaining.TotalSeconds < 0) timeRemaining = TimeSpan.Zero;
            // Ép format thời gian chuẩn 30:00 (Phút:Giây)
            timeStr = string.Format("{0:D2}:{1:D2}", timeRemaining.Minutes, timeRemaining.Seconds);
        }
        OnLivesUpdated?.Invoke(currentLives, timeStr);
    }
}