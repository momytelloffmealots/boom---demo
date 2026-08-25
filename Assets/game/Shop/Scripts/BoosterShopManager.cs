using UnityEngine;
using System;

public class BoosterShopManager : MonoBehaviour
{
    public static BoosterShopManager Instance { get; private set; }

    public event Action OnBoosterPurchased;

    private void Awake()
    {
        Instance = this;
    }

    public int GetBoosterCount(string boosterID)
    {
        return PlayerPrefs.GetInt($"BOOSTER_{boosterID}", 0);
    }

    public bool BuyBoosterDirectly(BoosterSO booster)
    {
        if (booster == null) return false;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("❌ Không tìm thấy CurrencyManager trong Scene!");
            return false;
        }

        // Trừ coin trực tiếp bằng TrySpendCoins của CurrencyManager
        if (CurrencyManager.Instance.TrySpendCoins(booster.price))
        {
            // Cộng số lượng Booster vào PlayerPrefs
            int currentAmount = GetBoosterCount(booster.boosterID);
            PlayerPrefs.SetInt($"BOOSTER_{booster.boosterID}", currentAmount + 1);
            PlayerPrefs.Save();

            Debug.Log($"<color=green>Mua thành công 1 {booster.boosterName}! Số lượng hiện có: {currentAmount + 1}</color>");

            OnBoosterPurchased?.Invoke();
            return true;
        }

        Debug.LogWarning("⚠️ Không đủ tiền trong CurrencyManager!");
        return false;
    }
}