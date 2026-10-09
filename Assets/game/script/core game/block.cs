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
    private MeshRenderer[] meshRenderers;
    private MaterialPropertyBlock propBlock;
    private bool isDestroyed = false;
    private Vector3 originalScale;

    public event Action<Block> OnBlockDestroyed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        originalScale = transform.localScale;
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
        transform.localScale = originalScale;

        if (data != null && rb != null)
        {
            rb.mass = data.mass;
        }

        if (meshRenderers != null && data != null && data.normalBehavior == NormalBlockBehavior.DeformShader)
        {
            foreach (var mr in meshRenderers)
            {
                mr.GetPropertyBlock(propBlock);
                propBlock.SetFloat(data.deformProgressProperty, 0f);
                mr.SetPropertyBlock(propBlock);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDestroyed || data == null) return;

        // Bỏ qua mọi va chạm trong 0.3s đầu khi vừa spawn ra Scene để tránh tự nổ
        if (Time.time < enableTime + spawnImmunityTime) return;

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        bool isGroundHit = collision.gameObject.CompareTag("ground");

        // 🔥 MỚI: Kiểm tra xem có phải va chạm với Đạn không
        bool isHitByBullet = collision.gameObject.GetComponent<Bullet>() != null;

        // 🔥 PHÁT HIT SOUND KHI BỊ ĐẠN BẮN TRÚNG
        if (isHitByBullet && data.hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(data.hitSound);
        }

        // ================= 1. XỬ LÝ BLOCK GLASS (THỦY TINH) =================
        if (data.blockType == BlockType.Glass)
        {
            float impactVelocity = collision.relativeVelocity.magnitude;

            if (isGroundHit || impactVelocity >= data.breakImpactThreshold)
            {
                BreakGlass(hitPoint);
            }
            return;
        }

        // ================= 2. XỬ LÝ BLOCK NORMAL (THƯỜNG) =================
        if (isGroundHit)
        {
            if (data.normalBehavior == NormalBlockBehavior.StandardVFX)
            {
                HandleNormalStandardVFX(hitPoint);
            }
            else if (data.normalBehavior == NormalBlockBehavior.DeformShader)
            {
                StartCoroutine(DeformAndDestroyRoutine(hitPoint));
            }
        }
    }

    private void BreakGlass(Vector3 spawnPoint)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (data.brokenGlassObjectPrefab != null)
        {
            GameObject brokenObj = Instantiate(data.brokenGlassObjectPrefab, transform.position + Vector3.up * 0.15f, transform.rotation);
            Destroy(brokenObj, 3.0f);
        }

        if (data.glassParticleVFX != null)
        {
            GameObject particleObj = Instantiate(data.glassParticleVFX, spawnPoint, Quaternion.identity);
        }

        // 🔥 MỚI: PHÁT BREAK SOUND CHO THỦY TINH
        if (data.breakSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(data.breakSound);
        }
        else if (data.glassBreakSound != null) // Tương thích ngược với file cũ
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

        // 🔥 MỚI: PHÁT BREAK SOUND KHI XUẤT HIỆN VFX DẠNG CHUẨN
        if (data.breakSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(data.breakSound);
        }

        if (data.groundVfxPrefab != null && SimpleBulletPool.Instance != null)
        {
            SimpleBulletPool.Instance.Spawn(
                data.groundVfxPrefab,
                spawnPoint + Vector3.up * 0.15f,
                Quaternion.identity,
                1.5f
            );
        }

        OnBlockDestroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    private IEnumerator DeformAndDestroyRoutine(Vector3 hitPoint)
    {
        if (isDestroyed) yield break;
        isDestroyed = true;

        // 🔥 MỚI: PHÁT BREAK SOUND KHI BẮT ĐẦU CHẠY HIỆU ỨNG MÉO (SHADER)
        if (data.breakSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(data.breakSound);
        }

        OnBlockDestroyed?.Invoke(this);

        if (rb != null) rb.isKinematic = true;

        float tDeform = data != null ? data.timeDeform : 0.2f;
        float tShrink = data != null ? data.timeShrink : 0.15f;

        int propID = Shader.PropertyToID(data != null ? data.deformProgressProperty : "_DeformAmount");
        int hitPropID = Shader.PropertyToID("_HitPosition");

        // --- GIAI ĐOẠN 1: Bóp méo ---
        float elapsed = 0f;
        while (elapsed < tDeform)
        {
            elapsed += Time.deltaTime;
            float progress = tDeform > 0f ? Mathf.Clamp01(elapsed / tDeform) : 1f;

            if (meshRenderers != null)
            {
                foreach (var mr in meshRenderers)
                {
                    mr.GetPropertyBlock(propBlock);
                    propBlock.SetFloat(propID, progress);
                    propBlock.SetVector(hitPropID, hitPoint);
                    mr.SetPropertyBlock(propBlock);
                }
            }
            yield return null;
        }

        if (meshRenderers != null)
        {
            foreach (var mr in meshRenderers)
            {
                mr.GetPropertyBlock(propBlock);
                propBlock.SetFloat(propID, 1f);
                mr.SetPropertyBlock(propBlock);
            }
        }

        // --- GIAI ĐOẠN 2: Thu nhỏ Scale về 0 ---
        elapsed = 0f;
        Vector3 initialScale = transform.localScale;
        while (elapsed < tShrink)
        {
            elapsed += Time.deltaTime;
            float progress = tShrink > 0f ? Mathf.Clamp01(elapsed / tShrink) : 1f;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}