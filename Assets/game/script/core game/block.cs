using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private BlockDataBase data;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask groundLayers;

    private Rigidbody rb;

    // Sự kiện báo cho Spawner/Manager biết khi Block va chạm đất để xử lý thu hồi
    public event Action<Block> OnBlockDestroyed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // --- BƯỚC 2: THÊM HÀM START ĐỂ BÁO DANH VỚI TRỌNG TÀI ---
    private void Start()
    {
        if (GameRuleController.Instance != null)
        {
            GameRuleController.Instance.RegisterBlock(this);
        }
    }
    // --------------------------------------------------------

    private void OnEnable()
    {
        // Cập nhật lại Mass từ SO mỗi khi Block active
        if (data != null && rb != null)
        {
            rb.mass = data.mass;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra nếu chạm vào Layer thuộc groundLayers
        if ((groundLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            // 1. Spawn VFX va chạm tại đúng vị trí tiếp xúc
            if (data != null && data.vfxPrefab != null && collision.contacts.Length > 0)
            {
                ContactPoint contact = collision.contacts[0];
                SimpleBulletPool.Instance.Spawn(
                    data.vfxPrefab,
                    contact.point,
                    Quaternion.LookRotation(contact.normal)
                );
                SimpleBulletPool.Instance.ReturnToPool(data.vfxPrefab, data.vfxPrefab); // Trả VFX về Pool sau khi phát xong (nếu muốn tái sử dụng)
            }

            // 2. Bắn sự kiện ra ngoài (nếu Spawner/Manager đang đăng ký lắng nghe)
            OnBlockDestroyed?.Invoke(this);
            // 4. Tự ẩn bản thân (không Destroy để tái sử dụng)
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Dọn dẹp sạch sẽ các bộ đếm Invoke nếu có
        CancelInvoke();
    }
    // Hàm này cho phép Bom Dính gọi để tiêu diệt Block
    public void ForceDestroy()
    {
        if (data != null && data.vfxPrefab != null)
        {
            SimpleBulletPool.Instance.Spawn(data.vfxPrefab, transform.position, Quaternion.identity);
            SimpleBulletPool.Instance.ReturnToPool(data.vfxPrefab, data.vfxPrefab);
        }
        
        // Báo cho Trọng tài (GameRuleController) biết là block đã vỡ
        OnBlockDestroyed?.Invoke(this);
        
        gameObject.SetActive(false);
    }
}

