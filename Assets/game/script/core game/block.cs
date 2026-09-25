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

    [Header("Glass Safety Settings")]
    [Tooltip("Thời gian miễn nhiễm vỡ khi vừa vào game (giây)")]
    [SerializeField] private float spawnImmunityTime = 0.3f;
    private float enableTime;

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
        enableTime = Time.time; // Lấy thời điểm vừa bật Block

        if (data != null && rb != null)
        {
            rb.mass = data.mass;
        }

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

        // Bỏ qua mọi va chạm trong 0.3s đầu khi vừa spawn ra Scene để tránh tự nổ
        if (Time.time < enableTime + spawnImmunityTime) return;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        bool isGroundHit = (groundLayers.value & (1 << collision.gameObject.layer)) != 0;

        // ================= 1. XỬ LÝ BLOCK GLASS (THỦY TINH) =================
        if (data.blockType == BlockType.Glass)
        {
            bool isHitByBullet = collision.gameObject.GetComponent<Bullet>() != null;
            float impactVelocity = collision.relativeVelocity.magnitude;

            // 💡 QUY TẮC VỠ GLASS:
            // 1. Chạm ĐẤT (Ground) -> VỠ LẬP TỨC!
            // 2. Dính ĐẠN -> VỠ LẬP TỨC!
            // 3. Va chạm khối khác với vận tốc đủ lớn (>= threshold) -> VỠ LẬP TỨC!
            if (isGroundHit || isHitByBullet || impactVelocity >= data.breakImpactThreshold)
            {
                BreakGlass(hitPoint);
            }
            return;
        }

        // ================= 2. XỬ LÝ BLOCK NORMAL (THƯỜNG) =================
        // Loại Normal chỉ vỡ/xử lý khi chạm LAYER ĐẤT (Ground)
        if (isGroundHit)
        {
            if (data.normalBehavior == NormalBlockBehavior.StandardVFX)
            {
                // Loại Normal 1: Chạm đất ẩn ngay lập tức & hiện VFX
                HandleNormalStandardVFX(hitPoint);
            }
            else if (data.normalBehavior == NormalBlockBehavior.DeformShader)
            {
                // Loại Normal 2: DUY NHẤT loại này rơi xuống đất chờ 1s (chạy Shader méo) rồi mới ẩn
                StartCoroutine(DeformAndDestroyRoutine());
            }
        }
    }

    private void BreakGlass(Vector3 spawnPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (data.brokenGlassObjectPrefab != null)
        {
            GameObject brokenObj = Instantiate(data.brokenGlassObjectPrefab, transform.position, transform.rotation);
            Destroy(brokenObj, 3.0f);
        }

        if (data.glassParticleVFX != null)
        {
            GameObject particleObj = Instantiate(data.glassParticleVFX, spawnPoint, Quaternion.identity);
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(particleObj, ps.main.duration + 0.5f);
            }
            else
            {
                Destroy(particleObj, 3.0f);
            }
        }

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