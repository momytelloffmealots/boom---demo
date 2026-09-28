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
        enableTime = Time.time; // Lấy thời điểm vừa bật Block
        transform.localScale = originalScale; // Khôi phục scale gốc vì có thể bị shrink về 0 ở lần trước



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



        // ================= 1. XỬ LÝ BLOCK GLASS (THỦY TINH) =================

        if (data.blockType == BlockType.Glass)

        {

            //bool isHitByBullet = collision.gameObject.GetComponent<Bullet>() != null;

            float impactVelocity = collision.relativeVelocity.magnitude;



            // 💡 QUY TẮC VỠ GLASS:

            // 1. Chạm ĐẤT (Ground) -> VỠ LẬP TỨC!

            // 2. Dính ĐẠN -> VỠ LẬP TỨC!

            // 3. Va chạm khối khác với vận tốc đủ lớn (>= threshold) -> VỠ LẬP TỨC!

            if (isGroundHit  || impactVelocity >= data.breakImpactThreshold)

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

            GameObject vfx = SimpleBulletPool.Instance.Spawn(data.groundVfxPrefab, spawnPoint + Vector3.up*0.15f, Quaternion.identity);

            if (vfx != null)

            {

                SimpleBulletPool.Instance.ReturnToPool(vfx, data.groundVfxPrefab);

            }

        }



        OnBlockDestroyed?.Invoke(this);

        gameObject.SetActive(false);

    }



    private IEnumerator DeformAndDestroyRoutine(Vector3 hitPoint)
    {
        if (isDestroyed) yield break;
        isDestroyed = true;

        OnBlockDestroyed?.Invoke(this);

        if (rb != null) rb.isKinematic = true;

        // Lấy 2 mốc thời gian từ data
        float tDeform = data != null ? data.timeDeform : 0.2f;
        float tShrink = data != null ? data.timeShrink : 0.15f;

        int propID = Shader.PropertyToID(data != null ? data.deformProgressProperty : "_DeformAmount");
        int hitPropID = Shader.PropertyToID("_HitPosition");

        // --- GIAI ĐOẠN 1: Bóp méo (Shader Anim) ---
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

        // Chốt giá trị ở mức 1 (Xẹp tối đa)
        if (meshRenderers != null)
        {
            foreach (var mr in meshRenderers)
            {
                mr.GetPropertyBlock(propBlock);
                propBlock.SetFloat(propID, 1f);
                mr.SetPropertyBlock(propBlock);
            }
        }

        // --- GIAI ĐOẠN 2: Thu nhỏ dần về 0 (Scale) ---
        Vector3 startScale = transform.localScale;
        elapsed = 0f;
        while (elapsed < tShrink)
        {
            elapsed += Time.deltaTime;
            float progress = tShrink > 0f ? Mathf.Clamp01(elapsed / tShrink) : 1f;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            yield return null;
        }
        
        transform.localScale = Vector3.zero; // Chốt hạ Scale = 0

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