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
                // Dàn hàng ngang tên lửa một chút để chúng không bị đè lên nhau
                float offsetX = (i - 1f) * 0.8f; 
                
                // Lấy vị trí nòng pháo làm điểm xuất phát ban đầu
                Vector3 spawnPos = cannon != null 
                    ? cannon.FirePoint.position + new Vector3(offsetX, 0, 0)
                    : new Vector3(offsetX, -2f, 0f); // Fallback nếu không tìm thấy pháo
                
                Instantiate(stickyBombPrefab, spawnPos, Quaternion.identity);
            }
            
            return true;
        }
        
        return false;
    }
}