//using System;
//using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//public class Block : MonoBehaviour
//{
//    [Header("Data Reference")]
//    [SerializeField] private BlockDataBase data;

//    [Header("Collision Settings")]
//    [SerializeField] private LayerMask groundLayers;

//    private Rigidbody rb;

//    // Sự kiện báo cho Spawner/Manager biết khi Block va chạm đất để xử lý thu hồi
//    public event Action<Block> OnBlockDestroyed;

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//    }

//    // --- BƯỚC 2: THÊM HÀM START ĐỂ BÁO DANH VỚI TRỌNG TÀI ---
//    private void Start()
//    {
//        if (GameRuleController.Instance != null)
//        {
//            GameRuleController.Instance.RegisterBlock(this);
//        }
//    }
//    // --------------------------------------------------------

//    private void OnEnable()
//    {
//        // Cập nhật lại Mass từ SO mỗi khi Block active
//        if (data != null && rb != null)
//        {
//            rb.mass = data.mass;
//        }
//    }

//    private void OnCollisionEnter(Collision collision)
//    {
//        // Kiểm tra nếu chạm vào Layer thuộc groundLayers
//        if ((groundLayers.value & (1 << collision.gameObject.layer)) != 0)
//        {
//            // 1. Spawn VFX va chạm tại đúng vị trí tiếp xúc
//            if (data != null && data.vfxPrefab != null && collision.contacts.Length > 0)
//            {
//                ContactPoint contact = collision.contacts[0];
//                SimpleBulletPool.Instance.Spawn(
//                    data.vfxPrefab,
//                    contact.point,
//                    Quaternion.LookRotation(contact.normal)
//                );
//                SimpleBulletPool.Instance.ReturnToPool(data.vfxPrefab, data.vfxPrefab); // Trả VFX về Pool sau khi phát xong (nếu muốn tái sử dụng)
//            }

//            // 2. Bắn sự kiện ra ngoài (nếu Spawner/Manager đang đăng ký lắng nghe)
//            OnBlockDestroyed?.Invoke(this);
//            // 4. Tự ẩn bản thân (không Destroy để tái sử dụng)
//            gameObject.SetActive(false);
//        }
//    }

//    private void OnDisable()
//    {
//        // Dọn dẹp sạch sẽ các bộ đếm Invoke nếu có
//        CancelInvoke();
//    }
//    // Hàm này cho phép Bom Dính gọi để tiêu diệt Block
//    public void ForceDestroy()
//    {
//        if (data != null && data.vfxPrefab != null)
//        {
//            SimpleBulletPool.Instance.Spawn(data.vfxPrefab, transform.position, Quaternion.identity);
//            SimpleBulletPool.Instance.ReturnToPool(data.vfxPrefab, data.vfxPrefab);
//        }

//        // Báo cho Trọng tài (GameRuleController) biết là block đã vỡ
//        OnBlockDestroyed?.Invoke(this);

//        gameObject.SetActive(false);
//    }
//}

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private BlockDataBase data;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask groundLayers;

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isDestroyed = false;

    public event Action<Block> OnBlockDestroyed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (GameRuleController.Instance != null)
        {
            GameRuleController.Instance.RegisterBlock(this);
        }
    }

    private void OnEnable()
    {
        isDestroyed = false;

        if (data != null)
        {
            if (rb != null) rb.mass = data.mass;
        }

        // Reset lại giá trị Shader Property về 0 nếu tái sử dụng từ Pool
        if (meshRenderer != null && data != null && data.normalBehavior == NormalBlockBehavior.DeformShader)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(data.deformProgressProperty, 0f);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed || data == null) return;

        float impactForce = collision.collisionHitForce();

        // ================= 1. XỬ LÝ BLOCK GLASS (THỦY TINH) =================
        if (data.blockType == BlockType.Glass)
        {
            // Glass chỉ cần ĐỦ LỰC là vỡ ngay (bất kể va chạm với Đạn, Đất, hay Block khác khi rơi từ trên xuống)
            if (impactForce >= data.breakImpactThreshold)
            {
                BreakGlass(collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
            }
            return;
        }

        // ================= 2. XỬ LÝ BLOCK NORMAL (THƯỜNG) =================
        // Loại Normal bắt buộc phải va chạm với LAYER ĐẤT (Ground)
        if ((groundLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            if (data.normalBehavior == NormalBlockBehavior.StandardVFX)
            {
                // Loại Normal 1: Ẩn ngay lập tức và hiện vfxPrefab
                HandleNormalStandardVFX(hitPoint);
            }
            else if (data.normalBehavior == NormalBlockBehavior.DeformShader)
            {
                // Loại Normal 2: Rơi xuống đất, không ẩn ngay mà chạy Shader làm méo trong 1s rồi mới ẩn
                StartCoroutine(DeformAndDestroyRoutine());
            }
        }
    }

    // Xử lý nổ vỡ Glass
    private void BreakGlass(Vector3 spawnPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 1. Sinh Object mô hình mảnh vỡ 3D (Broken Object)
        if (data.brokenGlassObjectPrefab != null)
        {
            GameObject brokenObj = Instantiate(data.brokenGlassObjectPrefab, transform.position, transform.rotation);
            Destroy(brokenObj, 3.0f); // Tự dọn dẹp mô hình mảnh vỡ sau 3s
        }

        // 2. Sinh Particle VFX vỡ mảnh vụn
        if (data.glassParticleVFX != null)
        {
            ParticleSystem particle = Instantiate(data.glassParticleVFX, spawnPoint, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, particle.main.duration + 0.5f);
        }

        // 3. Âm thanh vỡ
        if (data.glassBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(data.glassBreakSound, spawnPoint);
        }

        OnBlockDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    // Xử lý Normal loại 1
    private void HandleNormalStandardVFX(Vector3 spawnPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (data.groundVfxPrefab != null && SimpleBulletPool.Instance != null)
        {
            GameObject vfx = SimpleBulletPool.Instance.Spawn(data.groundVfxPrefab, spawnPoint, Quaternion.identity);
            if (vfx != null)
            {
                SimpleBulletPool.Instance.ReturnToPool(vfx, data.groundVfxPrefab);
            }
        }

        OnBlockDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    // Xử lý Normal loại 2 (Shader làm méo trong 1s)
    private IEnumerator DeformAndDestroyRoutine()
    {
        if (isDestroyed) yield break;
        isDestroyed = true;

        // Báo ngay cho Trọng tài để đếm số lượng gạch đã xử lý
        OnBlockDestroyed?.Invoke(this);

        // Vô hiệu hóa Rigidbody/Collider để không tiếp tục va chạm vật lý nữa
        if (rb != null) rb.isKinematic = true;

        float duration = 1.0f; // Thời gian chạy Shader méo (1 giây)
        float elapsed = 0f;

        int propID = Shader.PropertyToID(data.deformProgressProperty);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat(propID, progress); // Truyền giá trị 0 -> 1 vào ShaderGraph
                meshRenderer.SetPropertyBlock(propBlock);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void ForceDestroy()
    {
        if (isDestroyed) return;

        if (data != null && data.blockType == BlockType.Glass)
        {
            BreakGlass(transform.position);
        }
        else
        {
            HandleNormalStandardVFX(transform.position);
        }
    }
}

// Extension helper tính toán lực va chạm
public static class CollisionExtensions
{
    public static float collisionHitForce(this Collision collision)
    {
        if (collision.impulse.sqrMagnitude > 0)
        {
            return collision.impulse.magnitude / Time.fixedDeltaTime;
        }
        return collision.relativeVelocity.magnitude;
    }
}

