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
    [SerializeField] private List<ItemSO> shopItemList;

    private void OnEnable()
    {
        // Kích hoạt lắng nghe sự kiện đổi coin ĐỂ CẬP NHẬT TEXT (Không sinh lại UI)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged += UpdateCoinUI;
            UpdateCoinUI();
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện khi ẩn GameObject
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged -= UpdateCoinUI;
        }
    }

    private void Start()
    {
        UpdateCoinUI();
        GenerateShop(); // Chỉ khởi tạo UI các món đồ đúng 1 lần khi bắt đầu
    }

    // Cập nhật giao diện tiền mặt
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

    // Hàm tạo danh sách item trong shop
    public void GenerateShop()
    {
        if (contentParent == null || itemPrefab == null) return;

        // Xóa sạch các item UI cũ trước khi tạo lại
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (shopItemList == null) return;

        // Sinh ra các ô item tương ứng với danh sách Data
        foreach (var itemData in shopItemList)
        {
            if (itemData == null) continue;

            SimpleShopItemUI itemUI = Instantiate(itemPrefab, contentParent);
            itemUI.Setup(itemData, this);
        }
    }

    // Hàm xử lý logic khi bấm nút Mua
    public bool TryBuyItem(ItemSO item)
    {
        if (item == null || CurrencyManager.Instance == null) return false;

        // Thử trừ coin
        if (CurrencyManager.Instance.TrySpendCoins(item.price))
        {
            // 1. Gửi item vào Inventory (nếu đã tạo InventoryManager)
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(item);
            }

            // 2. Xóa item khỏi danh sách dữ liệu để khi mở lại shop không bị xuất hiện lại
            if (shopItemList.Contains(item))
            {
                shopItemList.Remove(item);
            }

            return true;
        }

        return false;
    }
}