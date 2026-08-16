using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<ItemSO> myInventory = new List<ItemSO>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(ItemSO item)
    {
        myInventory.Add(item);
        Debug.Log($"[INVENTORY] Đã thêm {item.itemName} vào kho!");
    }
}