using UnityEngine;

[CreateAssetMenu(fileName = "NewItemSO", menuName = "Shop/ItemSO")]
public class ItemSO : ScriptableObject
{
    public string id; // ID duy nhất cho mỗi item (VD: "cannon_01", "cannon_02")
    public string itemName;
    public Sprite icon;
    public int price;
}