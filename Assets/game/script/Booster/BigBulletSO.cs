using UnityEngine;

[CreateAssetMenu(fileName = "Data_BigBullet", menuName = "Game/Boosters/Big Bullet")]
public class BigBulletSO : BoosterSO
{
    public float scaleMultiplier = 2.5f;

    [Header("Cài đặt lực bắn")]
    public float forceMultiplier = 2.0f;

    [Header("Visuals & VFX")]
    public GameObject activationVFX;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        // Thay vì kích hoạt ngay, ta NẠP (Arm) vào súng để chờ bắn
        return cannon != null && cannon.ArmBigBullet(scaleMultiplier, forceMultiplier, activationVFX);
    }
}