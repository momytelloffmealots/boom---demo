using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image imgItem;
    [SerializeField] private Button btnEquip;
    [SerializeField] private TextMeshProUGUI txtEquipStatus;

    private ItemSO itemData;

    public void Setup(ItemSO data)
    {
        itemData = data;

        if (imgItem != null && data != null && data.icon != null)
        {
            imgItem.sprite = data.icon;
        }

        if (btnEquip != null)
        {
            btnEquip.onClick.RemoveAllListeners();
            btnEquip.onClick.AddListener(OnEquipClicked);
        }

        UpdateEquipState();
    }

    // --- HÀM NÀY ĐANG BỊ THIẾU TRONG INVENTORYITEMUI ---
    public void UpdateEquipState()
    {
        if (InventoryManager.Instance == null || itemData == null) return;

        bool isEquipped = InventoryManager.Instance.IsEquipped(itemData);

        if (isEquipped)
        {
            if (txtEquipStatus != null) txtEquipStatus.text = "Equipped";
            if (btnEquip != null) btnEquip.interactable = false;
        }
        else
        {
            if (txtEquipStatus != null) txtEquipStatus.text = "Equip";
            if (btnEquip != null) btnEquip.interactable = true;
        }
    }

    private void OnEquipClicked()
    {
        if (InventoryManager.Instance != null && itemData != null)
        {
            InventoryManager.Instance.EquipItem(itemData);
        }
    }
}