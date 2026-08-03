using UnityEngine;

public class ImpactVFX : MonoBehaviour
{
    [Header("1. VFX Khi Xuất Hiện (Muzzle/Spawn)")]
    [Tooltip("Kéo VFX muốn phát ngay khi Object vừa xuất hiện vào đây (VD: Lửa nòng súng)")]
    [SerializeField] private GameObject spawnVFXPrefab;

    [Header("2. VFX Khi Va Chạm (Impact)")]
    [Tooltip("Kéo VFX muốn phát khi va chạm vào đây (VD: Tia lửa/Nổ)")]
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("Settings")]
    [Tooltip("Thời gian VFX tự biến mất khỏi Scene (giây)")]
    [SerializeField] private float vfxLifetime = 2f;

    // Tự động chạy ngay khi Object xuất hiện hoặc được SetActive(true)
    private void OnEnable()
    {
        if (spawnVFXPrefab != null)
        {
            // Tạo Spawn VFX ngay tại vị trí và hướng hiện tại của Object
            GameObject vfxInstance = Instantiate(spawnVFXPrefab, transform.position, transform.rotation);
            Destroy(vfxInstance, vfxLifetime);
        }
    }

    // Tự động chạy khi Object va chạm vật lý
    private void OnCollisionEnter(Collision collision)
    {
        if (impactVFXPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];

            // Tạo Impact VFX tại điểm va chạm
            GameObject vfxInstance = Instantiate(impactVFXPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(vfxInstance, vfxLifetime);
        }

        // Ẩn/Xóa Object sau va chạm
        gameObject.SetActive(false);
    }
}