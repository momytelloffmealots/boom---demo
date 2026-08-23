using UnityEngine;
using TMPro; // Bắt buộc phải có để dùng TextMeshPro

public class BulletCountView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCount; // Kéo object "count" vào đây

    // Controller sẽ gọi hàm này để ra lệnh đổi chữ
    public void UpdateAmmoText(int currentAmmo)
    {
        if (txtCount != null)
        {
            txtCount.text = currentAmmo.ToString();
        }
    }
}