using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private InventoryItemUI itemPrefab;

    private List<InventoryItemUI> activeSlots = new List<InventoryItemUI>();

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquippedItemChanged += UpdateAllSlotsUI;
        }
        RefreshInventory();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquippedItemChanged -= UpdateAllSlotsUI;
        }
    }

    public void RefreshInventory()
    {
        if (contentParent == null || itemPrefab == null) return;

        // Xóa các ô UI cũ
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
        activeSlots.Clear();

        if (InventoryManager.Instance == null) return;

        List<ItemSO> items = InventoryManager.Instance.GetOwnedItems();

        // Sinh ra các ô item mới
        foreach (var item in items)
        {
            if (item == null) continue;

            InventoryItemUI slot = Instantiate(itemPrefab, contentParent);
            slot.Setup(item);
            activeSlots.Add(slot);
        }
    }

    private void UpdateAllSlotsUI()
    {
        foreach (var slot in activeSlots)
        {
            if (slot != null)
            {
                slot.UpdateEquipState();
            }
        }
    }

    public void CloseInventory()
    {
        gameObject.SetActive(false);
    }
}