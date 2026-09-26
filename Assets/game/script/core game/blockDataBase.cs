using UnityEngine;

public enum BlockType
{
    Normal, 
    Glass   
}

public enum NormalBlockBehavior
{
    StandardVFX,    
    DeformShader    
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
    public GameObject groundVfxPrefab;             
    public string deformProgressProperty = "_DeformAmount"; 

    [Header("Glass Block Options (Chỉ dùng khi BlockType = Glass)")]
    [Tooltip("Ngưỡng vận tốc va chạm tối thiểu để Glass vỡ khi đập vào vật KHÔNG PHẢI ĐẤT")]
    public float breakImpactThreshold = 8f;
    
    [Header("Glass Spawn Objects (Khi vỡ ra 2 GameObject)")]
    public GameObject brokenGlassObjectPrefab;    // GameObject 1: Mô hình mảnh vỡ 3D
    public GameObject waterSplashPrefab;         // GameObject 2: Hiệu ứng nước/bắn nước
    public GameObject glassParticleVFX;          // Particle vụn kính (nếu muốn dùng thêm)
    public AudioClip glassBreakSound;             // Âm thanh vỡ
}