using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<ItemSO> ownedItems = new List<ItemSO>();
    private ItemSO equippedItem;

    public event Action OnEquippedItemChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemSO item)
    {
        if (item != null && !ownedItems.Contains(item))
        {
            ownedItems.Add(item);
            Debug.Log($"[INVENTORY] Đã thêm thành công: {item.itemName}");

            if (equippedItem == null)
            {
                EquipItem(item);
            }
        }
    }

    public List<ItemSO> GetOwnedItems()
    {
        return ownedItems;
    }

    public void EquipItem(ItemSO item)
    {
        if (item != null && ownedItems.Contains(item))
        {
            equippedItem = item;
            Debug.Log($"Đã trang bị: {item.itemName}");
            OnEquippedItemChanged?.Invoke();
        }
    }

    public bool IsEquipped(ItemSO item)
    {
        return equippedItem == item;
    }

    public ItemSO GetEquippedItem()
    {
        return equippedItem;
    }
}