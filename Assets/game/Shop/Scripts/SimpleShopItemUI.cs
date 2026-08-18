using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleShopItemUI : MonoBehaviour
{
    [SerializeField] private Image imgItem;
    [SerializeField] private TextMeshProUGUI txtButtonLabel;

    private ItemSO itemData;
    private ShopManager shopManager;

    public void Setup(ItemSO data, ShopManager manager)
    {
        itemData = data;
        shopManager = manager;

        if (imgItem != null && data.icon != null)
        {
            imgItem.sprite = data.icon;
        }

        if (txtButtonLabel != null && itemData != null)
        {
            txtButtonLabel.text = itemData.price.ToString();
        }

        // Tự động tìm Button trong Prefab mà không cần kéo thả tay
        Button btn = GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnBuyClicked()
    {
        if (shopManager != null && itemData != null)
        {
            if (shopManager.TryBuyItem(itemData))
            {
                Debug.Log($"Đã mua thành công: {itemData.itemName}");
                Destroy(gameObject); // Biến mất khỏi Shop
            }
            else
            {
                Debug.Log("Không đủ coin!");
            }
        }
    }
}