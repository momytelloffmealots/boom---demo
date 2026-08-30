using UnityEngine;

[CreateAssetMenu(fileName = "Data_BombBooster", menuName = "Game/Boosters/Bomb Booster")]
public class BombBoosterSO : BoosterSO
{
    [Header("Bom Dính")]
    public GameObject stickyBombPrefab; 
    public int spawnCount = 3;          

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        if (stickyBombPrefab != null)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                // 🔥 ĐẦY TRỤC Z LÙI HẲN RA NGOÀI (ví dụ -6f)
                // Bọ sẽ xuất hiện ở ngoài màn hình (gần camera), sau đó bay vào trong theo chiều dương Z để đập gạch
                float posX = (i - 1f) * 1.5f;
                Vector3 fixedSpawnPos = new Vector3(posX, 2f, -6f); 
                
                Instantiate(stickyBombPrefab, fixedSpawnPos, Quaternion.identity);
            }
            
            Debug.Log("<color=green>Đã đẻ 3 chú bọ ở ngoài màn hình bay vào!</color>");
            return true;
        }
        
        Debug.LogWarning("Chưa gắn Prefab Bom vào Data_BombBooster!");
        return false;
    }
}