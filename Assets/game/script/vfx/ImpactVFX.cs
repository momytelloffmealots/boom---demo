using UnityEngine;

public class ImpactVFX : MonoBehaviour
{
    [Header("1. VFX Khi Xuất Hiện (Muzzle/Spawn)")]
    [Tooltip("Kéo VFX muốn phát ngay khi Object vừa xuất hiện vào đây (VD: Lửa nòng súng)")]
    [SerializeField] private GameObject spawnVFXPrefab;

    [Header("2. VFX Khi Va Chạm (Impact)")]
    [Tooltip("Kéo VFX muốn phát khi va chạm vào đây (VD: Tia lửa/Nổ)")]
    [SerializeField] private GameObject impactVFXPrefab;
    [Tooltip("Tên Tag yêu cầu khi va chạm để kích hoạt VFX (Ví dụ: block)")]
    [SerializeField] private string targetTag = "block";

    [Header("Settings")]
    [Tooltip("Thời gian VFX tự biến mất khỏi Scene (giây)")]
    [SerializeField] private float vfxLifetime = 2f;

    private bool hasCollided = false;

    // Tự động chạy ngay khi Object xuất hiện hoặc được SetActive(true)
    private void OnEnable()
    {
        hasCollided = false; // Reset trạng thái va chạm để dùng lại đạn từ Pool

        if (spawnVFXPrefab != null)
        {
            // Tạo Spawn VFX bằng Instantiate bình thường
            GameObject vfxInstance = Instantiate(spawnVFXPrefab, transform.position, transform.rotation);
            Destroy(vfxInstance, vfxLifetime);
        }
    }

    // Tự động chạy khi Object va chạm vật lý
    private void OnCollisionEnter(Collision collision)
    {
        // Chỉ cho phép va chạm sinh VFX lần đầu tiên
        if (hasCollided) return;

        // Chỉ kích hoạt khi va chạm đúng tag yêu cầu
        if (!string.IsNullOrEmpty(targetTag) && !collision.gameObject.CompareTag(targetTag))
            return;

        hasCollided = true;

        if (impactVFXPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];

            // Tạo Impact VFX tại điểm va chạm bằng Instantiate bình thường
            GameObject vfxInstance = Instantiate(impactVFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(vfxInstance, vfxLifetime);
        }

        // Ẩn/Xóa Object sau va chạm (để Bullet.cs xử lý việc trì hoãn ẩn đạn nếu có đính kèm)
        if (!TryGetComponent<Bullet>(out _))
        {
            gameObject.SetActive(false);
        }
    }
}