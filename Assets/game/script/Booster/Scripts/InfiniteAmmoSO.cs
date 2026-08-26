using UnityEngine;

[CreateAssetMenu(fileName = "Data_InfiniteAmmo", menuName = "Game/Boosters/Infinite Ammo")]
public class InfiniteAmmoSO : BoosterSO
{
    public float duration = 10f;

    public override bool ActivateBooster(SimpleCannon cannon)
    {
        return cannon != null && cannon.ActivateInfiniteAmmo(duration);
    }
}