using UnityEngine;

[CreateAssetMenu(fileName = "Data_InfiniteAmmo", menuName = "Game/Boosters/Infinite Ammo")]
public class InfiniteAmmoSO : BoosterSO
{
    public float duration = 10f;

    [Header("Visuals & VFX")]
    public GameObject activationVFX;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        bool isActivated = cannon != null && cannon.ActivateInfiniteAmmo(duration);

        if (isActivated && activationVFX != null)
        {
            // Lấy vị trí CannonBasePoint của pháo. 
            // Dùng activationVFX.transform.rotation để giữ nguyên góc xoay mặc định của Prefab vòng Aura phẳng
            GameObject vfx = Instantiate(activationVFX, cannon.CannonBasePoint.position, activationVFX.transform.rotation);

            // Cực hay: Ép vòng Aura tự động biến mất đúng bằng thời gian duration của Buff
            Destroy(vfx, duration);
        }

        return isActivated;
    }
}