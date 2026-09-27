using UnityEngine;

[CreateAssetMenu(fileName = "Data_InfiniteAmmo", menuName = "Game/Boosters/Infinite Ammo")]
public class InfiniteAmmoSO : BoosterSO
{
    public float duration = 10f;

    [Header("Visuals & VFX")]
    public GameObject activationVFX;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        // 🔥 SỬA THÀNH HÀM ARM (NẠP ĐẠN CHỜ BẮN)
        return cannon != null && cannon.ArmInfiniteAmmo(duration, activationVFX);
    }
}