using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleShopItemUI : MonoBehaviour
{
    [SerializeField] private Image imgItem;
    [SerializeField] private TextMeshProUGUI txtButtonLabel;
    [SerializeField] private Button btnBuy; // Khai báo rõ nút Mua ở đây

    private ItemSO itemData;
    private ShopManager shopManager;

    public void Setup(ItemSO data, ShopManager manager)
    {
        itemData = data;
        shopManager = manager;

        // 1. Cài đặt hiển thị ảnh & sự kiện mở Popup khi bấm vào ảnh
        if (imgItem != null && data.icon != null)
        {
            imgItem.sprite = data.icon;

            Button imgBtn = imgItem.GetComponent<Button>();
            if (imgBtn == null)
            {
                imgBtn = imgItem.gameObject.AddComponent<Button>();
            }

            imgBtn.onClick.RemoveAllListeners();
            imgBtn.onClick.AddListener(OnImageClicked);
        }

        // 2. Cài đặt giá tiền
        if (txtButtonLabel != null && itemData != null)
        {
            txtButtonLabel.text = itemData.price.ToString();
        }

        // 3. Cài đặt sự kiện Mua cho nút btnBuy
        if (btnBuy != null)
        {
            btnBuy.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnImageClicked()
    {
        if (shopManager != null && shopManager.DetailPopup != null)
        {
            shopManager.DetailPopup.ShowPopup(itemData);
        }
    }

    private void OnBuyClicked()
    {
        if (shopManager != null && itemData != null)
        {
            if (shopManager.TryBuyItem(itemData))
            {
                Debug.Log($"Mua thành công: {itemData.itemName}");
                RemoveUI();
            }
            else
            {
                Debug.Log("Không đủ coin!");
            }
        }
    }

    public void RemoveUI()
    {
        transform.SetParent(null);
        Destroy(gameObject);
    }
}