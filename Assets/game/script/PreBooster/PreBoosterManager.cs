using UnityEngine;
using System.Collections; // Bắt buộc phải có dòng này để chạy Coroutine

public class PreBoosterManager : MonoBehaviour
{
    public static PreBoosterManager Instance { get; private set; }

    [Header("References")]
    public SimpleCannon playerCannon;

    [Header("Pre-Boosters Data")]
    public BoosterSO bombBoosterSO; 

    private void Awake()
    {
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
    /// Thay vì kích hoạt ngay, hàm này sẽ gọi một luồng đếm ngược
    /// </summary>
    public void ApplyPreBoosters()
    {
        StartCoroutine(DelayedApplyRoutine());
    }

    private IEnumerator DelayedApplyRoutine()
    {
        // 🔥 ĐỢI 1.5 GIÂY: Ép hệ thống phải chờ màn hình Loading rút đi hoàn toàn
        yield return new WaitForSeconds(1f);

        if (playerCannon == null || bombBoosterSO == null) yield break; // Dùng yield break thay cho return trong Coroutine

        string preSelectKey = $"PRE_SELECTED_{bombBoosterSO.boosterID}";
        string boosterKey = $"BOOSTER_{bombBoosterSO.boosterID}";

        if (PlayerPrefs.GetInt(preSelectKey, 0) == 1)
        {
            int count = PlayerPrefs.GetInt(boosterKey, 0);
            PlayerPrefs.SetInt(boosterKey, count - 1);
            
            bombBoosterSO.ActivateBooster(playerCannon);
            
            PlayerPrefs.SetInt(preSelectKey, 0);
            PlayerPrefs.Save();
            
            Debug.Log("<color=cyan>PreBoosterManager: Đã nạp Bom Dính thành công!</color>");
        }
    }
}