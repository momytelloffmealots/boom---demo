using UnityEngine;

[CreateAssetMenu(fileName = "Data_BigBullet", menuName = "Game/Boosters/Big Bullet")]
public class BigBulletSO : BoosterSO
{
    public float scaleMultiplier = 2.5f;

    [Header("Visuals & VFX")]
    public GameObject activationVFX;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        bool isActivated = cannon != null && cannon.ActivateBigBullet(scaleMultiplier);

        if (isActivated && activationVFX != null)
        {
            GameObject vfx = Instantiate(activationVFX, cannon.FirePoint.position, cannon.FirePoint.rotation, cannon.FirePoint);
            
            // Ép pháo giữ cái VFX này lại, không tự hủy sau 2s nữa
            cannon.SetChargeVFX(vfx); 
        }

        return isActivated;
    }
}