using UnityEngine;

public enum BoosterType
{
    BigBullet,      // Đạn phóng to (Chỉ có tác dụng 1 viên)
    InfiniteAmmo    // Đạn vô hạn (Tác dụng theo thời gian đếm ngược)
}

[CreateAssetMenu(fileName = "NewBoosterData", menuName = "Game/Booster Data")]
public class BoosterSO : ScriptableObject
{
    [Header("Booster Info")]
    public string boosterID;
    public string boosterName;
    public BoosterType type;
    public Sprite icon;
    public int price;

    [Header("Booster Config")]
    [Tooltip("Chỉ dùng cho InfiniteAmmo: Thời gian duy trì đạn vô hạn (giây)")]
    public float duration = 10f; 

    [Tooltip("Chỉ dùng cho BigBullet: Tỉ lệ phóng to (ví dụ: 2 nghĩa là gấp đôi)")]
    public float scaleMultiplier = 2f;
}