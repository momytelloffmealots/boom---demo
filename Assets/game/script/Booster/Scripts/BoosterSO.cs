using UnityEngine;

public abstract class BoosterSO : ScriptableObject
{
    [Header("Base Info")]
    public string boosterID;
    public string boosterName;
    public Sprite icon;
    public int price;

    public abstract bool ActivateBooster(SimpleCannon cannon);
}