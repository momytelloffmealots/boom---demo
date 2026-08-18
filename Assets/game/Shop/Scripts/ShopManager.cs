using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private Transform contentParent;
    [SerializeField] private SimpleShopItemUI itemPrefab;
    [SerializeField] private ItemDetailPopup detailPopup; // Kéo Popup vào đây

    public ItemDetailPopup DetailPopup => detailPopup;

    [Header("Data Settings")]
    [SerializeField] private List<ItemSO> shopItemList;

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
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (shopItemList == null) return;

        foreach (var itemData in shopItemList)
        {
            if (itemData == null) continue;

            SimpleShopItemUI itemUI = Instantiate(itemPrefab, contentParent);
            itemUI.Setup(itemData, this);
        }
    }

    public bool TryBuyItem(ItemSO item)
    {
        if (item == null || CurrencyManager.Instance == null) return false;

        if (CurrencyManager.Instance.TrySpendCoins(item.price))
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(item);
            }

            if (shopItemList.Contains(item))
            {
                shopItemList.Remove(item);
            }

            return true;
        }

        return false;
    }
}