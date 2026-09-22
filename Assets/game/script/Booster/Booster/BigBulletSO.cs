using UnityEngine;

[CreateAssetMenu(fileName = "Data_BigBullet", menuName = "Game/Boosters/Big Bullet")]
public class BigBulletSO : BoosterSO
{
    public float scaleMultiplier = 2.5f;
    
    [Header("Cài đặt lực bắn")]
    public float forceMultiplier = 2.0f; // Hệ số nhân lực bắn (Ví dụ: 2 = mạnh gấp đôi)

    [Header("Visuals & VFX")]
    public GameObject activationVFX;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        // THÊM: Truyền forceMultiplier vào hàm của nòng pháo
        bool isActivated = cannon != null && cannon.ActivateBigBullet(scaleMultiplier, forceMultiplier);

        if (isActivated && activationVFX != null)
        {
            GameObject vfx = Instantiate(activationVFX, cannon.FirePoint.position, cannon.FirePoint.rotation, cannon.FirePoint);
            cannon.SetChargeVFX(vfx); 
        }

        return isActivated;
    }
}