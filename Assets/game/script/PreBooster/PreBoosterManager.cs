using UnityEngine;

public class PreBoosterManager : MonoBehaviour
{
    public static PreBoosterManager Instance { get; private set; }

    [Header("References")]
    public SimpleCannon playerCannon;

    [Header("Pre-Boosters Data")]
    public BoosterSO bombBoosterSO; // Chỉ chứa Bom Dính

    private void Awake()
    {
        // Singleton pattern để dễ dàng gọi từ nơi khác
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Hàm này sẽ được GameRuleController gọi khi bắt đầu ván đấu
    /// </summary>
    public void ApplyPreBoosters()
    {
        if (playerCannon == null || bombBoosterSO == null) return;

        string preSelectKey = $"PRE_SELECTED_{bombBoosterSO.boosterID}";
        string boosterKey = $"BOOSTER_{bombBoosterSO.boosterID}";

        // Kiểm tra xem người chơi có tick chọn Bom Xanh không
        if (PlayerPrefs.GetInt(preSelectKey, 0) == 1)
        {
            // Trừ số lượng trong kho
            int count = PlayerPrefs.GetInt(boosterKey, 0);
            PlayerPrefs.SetInt(boosterKey, count - 1);
            
            // Kích hoạt nạp Bom Xanh vào súng
            bombBoosterSO.ActivateBooster(playerCannon);
            
            // Xóa cờ đã dùng để không bị lặp lại ở level sau
            PlayerPrefs.SetInt(preSelectKey, 0);
            PlayerPrefs.Save();
            
            Debug.Log("<color=cyan>PreBoosterManager: Đã nạp Bom Dính thành công!</color>");
        }
    }
}