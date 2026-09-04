using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private Transform contentParent;
    [SerializeField] private SimpleShopItemUI itemPrefab;

    [Header("Data Settings")]
    [SerializeField] private List<BoosterSO> shopBoosterList;

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinUI;
            UpdateCoinUI();
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinUI;
        }
    }

    private void Start()
    {
        UpdateCoinUI();
        GenerateShop();
    }

    public void UpdateCoinUI()
    {
        if (CurrencyManager.Instance != null && txtCoins != null)
        {
            txtCoins.text = CurrencyManager.Instance.GetCoins().ToString();
        }
    }

    private void UpdateCoinUI(int newCoins)
    {
        if (txtCoins != null)
        {
            txtCoins.text = newCoins.ToString();
        }
    }

    public void GenerateShop()
    {
        if (contentParent == null || itemPrefab == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = contentParent.GetChild(i);
            Destroy(child.gameObject);
        }

        if (shopBoosterList == null) return;

        foreach (var boosterData in shopBoosterList)
        {
            if (boosterData == null) continue;

            SimpleShopItemUI itemUI = Instantiate(itemPrefab, contentParent);
            itemUI.Setup(boosterData, this);
        }
    }

    public bool TryBuyBooster(BoosterSO booster)
    {
        if (booster == null || CurrencyManager.Instance == null) return false;

        if (CurrencyManager.Instance.TrySpendCoins(booster.price))
        {
            int currentCount = PlayerPrefs.GetInt($"BOOSTER_{booster.boosterID}", 0);
            PlayerPrefs.SetInt($"BOOSTER_{booster.boosterID}", currentCount + 1);
            PlayerPrefs.Save();

            Debug.Log($"<color=green>Mua thành công: {booster.boosterName}! Số lượng hiện tại: {currentCount + 1}</color>");

            return true;
        }

        Debug.LogWarning("Không đủ coin!");
        return false;
    }
}