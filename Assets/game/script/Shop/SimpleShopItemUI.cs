using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleShopItemUI : MonoBehaviour
{
    [SerializeField] private Image imgItem;
    [SerializeField] private TextMeshProUGUI txtButtonLabel;
    [SerializeField] private Button btnBuy;

    private BoosterSO boosterData;
    private ShopManager shopManager;

    public void Setup(BoosterSO data, ShopManager manager)
    {
        boosterData = data;
        shopManager = manager;

        if (imgItem != null && boosterData != null && boosterData.icon != null)
        {
            imgItem.sprite = boosterData.icon;
        }

        // ĐÃ SỬA: boosterData.price (chữ p viết thường)
        if (txtButtonLabel != null && boosterData != null)
        {
            txtButtonLabel.text = boosterData.price.ToString();
        }

        if (btnBuy != null)
        {
            btnBuy.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnBuyClicked()
    {
        if (shopManager != null && boosterData != null)
        {
            shopManager.TryBuyBooster(boosterData);
        }
    }
}