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

        // LÁ CHẮN 1: Bỏ qua mọi va chạm trong 0.3s đầu khi vừa spawn ra Scene
        if (Time.time < enableTime + spawnImmunityTime) return;

        // LÁ CHẮN 2: Dùng vận tốc tương đối (Relative Velocity) thay vì Impulse để tránh spike lực
        float impactVelocity = collision.relativeVelocity.magnitude;

        // ================= 1. XỬ LÝ BLOCK GLASS (THỦY TINH) =================
        if (data.blockType == BlockType.Glass)
        {
            // Kiểm tra xem vật va chạm có phải là viên Đạn không
            bool isHitByBullet = collision.gameObject.GetComponent<Bullet>() != null;

            // 💡 QUY TẮC VỠ GLASS:
            // - Nếu dính ĐẠN: Vỡ ngay lập tức!
            // - Nếu va chạm vật khác (rơi xuống sàn/đập chai khác): Vận tốc va chạm phải >= breakImpactThreshold
            if (isHitByBullet || impactVelocity >= data.breakImpactThreshold)
            {
                BreakGlass(collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
            }
            return;
        }

        // ================= 2. XỬ LÝ BLOCK NORMAL (THƯỜNG) =================
        if ((groundLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            if (data.normalBehavior == NormalBlockBehavior.StandardVFX)
            {
                HandleNormalStandardVFX(hitPoint);
            }
            else if (data.normalBehavior == NormalBlockBehavior.DeformShader)
            {
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
            ParticleSystem particle = Instantiate(data.glassParticleVFX, spawnPoint, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, particle.main.duration + 0.5f);
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