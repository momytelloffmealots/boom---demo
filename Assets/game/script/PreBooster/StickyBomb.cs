using UnityEngine;

public class StickyBomb : MonoBehaviour
{
    [Header("Cài đặt bay")]
    public float speed = 8f;            
    public float defaultScale = 0.07f; 
    public float explosionScale = 1.6f; 
    public GameObject explosionVFX;    

    [Header("Lực nổ phụ (Vật lý)")]
    public float explosionForce = 2000f;
    public float explosionRadius = 3f;
    public float upliftModifier = 0.5f;

    private Block targetBlock;

    private void Start()
    {
        transform.localScale = Vector3.one * defaultScale;
        FindNewTarget();
    }

    private void FindNewTarget()
    {
        Block[] allBlocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        if (allBlocks.Length > 0)
        {
            targetBlock = allBlocks[Random.Range(0, allBlocks.Length)];
        }
        else
        {
            targetBlock = null; 
        }
    }

    private void Update()
    {
        if (targetBlock == null || !targetBlock.gameObject.activeInHierarchy)
        {
            FindNewTarget();
            return; 
        }

        Vector3 direction = (targetBlock.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.up = direction; 
        }

        transform.position = Vector3.MoveTowards(transform.position, targetBlock.transform.position, speed * Time.deltaTime);

        // 1. TĂNG KHOẢNG CÁCH NỔ LÊN 0.5f: Chạm vào rìa vỏ gạch là nổ luôn
        if (Vector3.Distance(transform.position, targetBlock.transform.position) < 0.5f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionVFX != null)
        {
            // 2. ÉP TRỤC Z = -2f: Đảm bảo hiệu ứng khói luôn sinh ra nổi lên trên cùng, không bị gạch che
            Vector3 vfxPosition = new Vector3(transform.position.x, transform.position.y, -2f);
            GameObject vfx = Instantiate(explosionVFX, vfxPosition, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * explosionScale;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        bool hitAnyBlock = false;

        foreach (Collider hit in colliders)
        {
            // 3. DÙNG COMPONENT THAY VÌ TAG: Quét thẳng vào script Block, bỏ qua lỗi sai Tag ngoài Unity
            Block block = hit.GetComponentInParent<Block>();
            
            if (block != null && hit.attachedRigidbody != null)
            {
                hitAnyBlock = true;
                hit.attachedRigidbody.isKinematic = false;
                hit.attachedRigidbody.WakeUp();
                hit.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, upliftModifier, ForceMode.Impulse);
            }
        }

        // 4. BẢO HIỂM: Nếu quét OverlapSphere bị hụt, ép hất văng ít nhất là viên gạch mục tiêu
        if (!hitAnyBlock && targetBlock != null)
        {
            Rigidbody rb = targetBlock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upliftModifier, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }
}