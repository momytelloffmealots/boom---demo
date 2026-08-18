using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockDataBase", menuName = "Game/Block Data Base")]
public class BlockDataBase : ScriptableObject
{
    [Header("Block Info")]
    public string blockName;
    public float mass = 1f; // Đổi int -> float cho khớp với Rigidbody

    [Header("Visual & Effects")]
    //public GameObject prefab; // Prefab 3D của Block
    public GameObject vfxPrefab; // Hiệu ứng nổ khi chạm đất
}