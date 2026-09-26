using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private BlockDataBase data;

    [Header("Tag Settings")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private string bulletTag = "Bullet";

    [Header("Glass Safety Settings")]
    [Tooltip("Thời gian miễn nhiễm vỡ khi vừa vào game (giây)")]
    [SerializeField] private float spawnImmunityTime = 0.3f;
    private float enableTime;

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isDestroyed = false;
    private Coroutine deformCoroutine;

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
        enableTime = Time.time;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (data != null)
            {
                rb.mass = data.mass;
            }
        }

        if (meshRenderer != null && data != null && data.normalBehavior == NormalBlockBehavior.DeformShader)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(data.deformProgressProperty, 0f);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    private void OnDisable()
    {
        if (deformCoroutine != null)
        {
            StopCoroutine(deformCoroutine);
            deformCoroutine = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed || data == null) return;

        // Bỏ qua va chạm trong 0.3s đầu khi vừa spawn ra Scene
        if (Time.time < enableTime + spawnImmunityTime) return;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

        // DÙNG COMPARETAG TRỰC TIẾP TẠI ĐÂY
        bool isGroundHit = collision.gameObject.CompareTag(groundTag);

        // ================= 1. TRƯỜNG HỢP CHẠM ĐẤT (GROUND) =================
        // HỄ CHẠM ĐẤT LÀ MỌI BLOCK BIẾN MẤT LUÔN!
        if (isGroundHit)
        {
            if (data.blockType == BlockType.Glass)
            {
                // Glass chạm đất -> Vỡ ngay & tạo 2 GameObject (Mảnh vỡ + Nước)
                BreakGlass(hitPoint);
            }
            else // Normal Block
            {
                if (data.normalBehavior == NormalBlockBehavior.StandardVFX)
                {
                    HandleNormalStandardVFX(hitPoint);
                }
                else if (data.normalBehavior == NormalBlockBehavior.DeformShader)
                {
                    if (deformCoroutine != null) StopCoroutine(deformCoroutine);
                    deformCoroutine = StartCoroutine(DeformAndDestroyRoutine());
                }
            }
            return;
        }

        // ================= 2. TRƯỜNG HỢP VA CHẠM VỚI VẬT KHÁC (ĐẠN HOẶC BLOCK) =================
        // Riêng Glass: Đủ lực HOẶC dính Đạn mới vỡ
        if (data.blockType == BlockType.Glass)
        {
            bool isHitByBullet = collision.gameObject.CompareTag(bulletTag);
            float impactVelocity = collision.relativeVelocity.magnitude;

            if (isHitByBullet || impactVelocity >= data.breakImpactThreshold)
            {
                BreakGlass(hitPoint);
            }
        }
    }

    private void BreakGlass(Vector3 spawnPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 1. Spawn GameObject 1: Mô hình Mảnh Vỡ 3D
        if (data.brokenGlassObjectPrefab != null)
        {
            GameObject brokenObj = Instantiate(data.brokenGlassObjectPrefab, transform.position, transform.rotation);
            Destroy(brokenObj, 3.0f);
        }

        // 2. Spawn GameObject 2: Hiệu ứng Nước / Bắn Nước (Water Splash)
        if (data.waterSplashPrefab != null)
        {
            GameObject waterObj = Instantiate(data.waterSplashPrefab, spawnPoint, Quaternion.identity);
            Destroy(waterObj, 3.0f);
        }

        // 3. Spawn Particle VFX bổ sung (nếu có)
        if (data.glassParticleVFX != null)
        {
            GameObject particleObj = Instantiate(data.glassParticleVFX, spawnPoint, Quaternion.identity);
            if (particleObj.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                ps.Play();
                Destroy(particleObj, ps.main.duration + 0.5f);
            }
            else
            {
                Destroy(particleObj, 3.0f);
            }
        }

        // 4. Âm thanh vỡ
        if (data.glassBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(data.glassBreakSound, spawnPoint);
        }

        OnBlockDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

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

    private IEnumerator DeformAndDestroyRoutine()
    {
        if (isDestroyed) yield break;
        isDestroyed = true;

        OnBlockDestroyed?.Invoke(this);

        if (rb != null) rb.isKinematic = true;

        float duration = 1.0f;
        float elapsed = 0f;
        int propID = Shader.PropertyToID(data.deformProgressProperty);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat(propID, progress);
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