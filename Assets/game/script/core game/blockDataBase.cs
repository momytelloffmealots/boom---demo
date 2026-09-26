using UnityEngine;



public enum BlockType

{

    Normal, // Loại thường (Cần rơi xuống đất mới biến mất)

    Glass   // Thủy tinh (Chỉ cần đủ lực va chạm là vỡ lập tức)

}



public enum NormalBlockBehavior

{

    StandardVFX,    // Va chạm đất -> Ẩn ngay & Hiện vfxPrefab từ Pool

    DeformShader    // Va chạm đất -> Giữ nguyên, chạy ShaderGraph làm méo vật thể rồi biến mất sau 1s

}



[CreateAssetMenu(fileName = "NewBlockDataBase", menuName = "Game/Block Data Base")]

public class BlockDataBase : ScriptableObject

{

    [Header("Block Classification")]

    public string blockName;

    public BlockType blockType = BlockType.Normal;

    public float mass = 1f;



    [Header("Normal Block Options (Chỉ dùng khi BlockType = Normal)")]

    public NormalBlockBehavior normalBehavior = NormalBlockBehavior.StandardVFX;

    public GameObject groundVfxPrefab;             // VFX xuất hiện khi chạm đất (Cho loại StandardVFX)

    public string deformProgressProperty = "_DeformAmount"; // Tên biến float trong ShaderGraph làm méo (Cho loại DeformShader)



    [Header("Glass Block Options (Chỉ dùng khi BlockType = Glass)")]

    [Tooltip("Ngưỡng lực va chạm tối thiểu để Glass vỡ (Tác động bởi đạn hoặc rơi đập bất kỳ vật nào)")]

    public float breakImpactThreshold = 8f;

    public GameObject brokenGlassObjectPrefab;    // Object mô hình các mảnh vỡ (3D Fractured Model)

    public GameObject glassParticleVFX;       // Particle vụn thủy tinh văng ra tại vị trí vỡ (Đổi thành GameObject)

    public AudioClip glassBreakSound;             // Âm thanh vỡ

}