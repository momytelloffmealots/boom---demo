using UnityEngine;

public class BulletImpact : MonoBehaviour
{
    [Header("War FX Asset")]
    [Tooltip("Kéo Prefab hiệu ứng nổ từ thư mục _Effects vào đây")]
    [SerializeField] private GameObject explosionVFXPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        if (explosionVFXPrefab != null)
        {
            // 1. Lấy điểm va chạm đầu tiên
            ContactPoint contact = collision.contacts[0];

            // 2. Tạo hiệu ứng nổ tại điểm va chạm, xoay hướng văng ra ngoài mặt tường
            Instantiate(explosionVFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            //CFX_SpawnSystem.GetNextObject(explosionVFXPrefab);
        }
    }
}