using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private int currentCoins = 10000; // Vốn khởi nghiệp ban đầu

    public event Action<int> OnCoinChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Tách object ra gốc (nếu đang nằm trong object khác) để tránh lỗi DontDestroyOnLoad
            transform.SetParent(null); 
            // Giữ cho Quản lý tiền không bị tiêu diệt khi bấm Reset Level
            DontDestroyOnLoad(gameObject);
            
            // 🔥 ĐỌC DATA: Lấy tiền từ điện thoại/máy tính. Nếu chưa có data (lần đầu chơi), lấy số vốn khởi nghiệp.
            currentCoins = PlayerPrefs.GetInt("SAVE_COIN_DATA", currentCoins);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetCoins() => currentCoins;

    public bool TrySpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            
            // 🔥 LƯU DATA: Tiêu tiền xong là chốt sổ ngay lập tức!
            PlayerPrefs.SetInt("SAVE_COIN_DATA", currentCoins);
            PlayerPrefs.Save();
            
            OnCoinChanged?.Invoke(currentCoins);
            Debug.Log($"Đã trừ {amount} coin. Số coin còn lại: {currentCoins}");
            return true;
        }

        Debug.Log("Không đủ coin!");
        return false;
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        
        // 🔥 LƯU DATA: Nhận được tiền cũng chốt sổ ngay lập tức!
        PlayerPrefs.SetInt("SAVE_COIN_DATA", currentCoins);
        PlayerPrefs.Save();
        
        OnCoinChanged?.Invoke(currentCoins);
    }
    
    // (Tuỳ chọn) Hàm hỗ trợ dùng để Xoá Data test game từ đầu
    [ContextMenu("Reset Coin Data")]
    public void ResetCoinData()
    {
        PlayerPrefs.DeleteKey("SAVE_COIN_DATA");
        currentCoins = 10000;
        OnCoinChanged?.Invoke(currentCoins);
        Debug.Log("Đã reset tiền về mặc định!");
    }
}