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

    [Header("Âm thanh")]
    public AudioClip flyingSFX;    // Tiếng rít khi bay
    public AudioClip explosionSFX; // Tiếng va chạm nổ bùm

    private Block targetBlock;

    private void Start()
    {
        transform.localScale = Vector3.one * defaultScale;
        FindNewTarget();

        // 1. PHÁT TIẾNG BAY: Gọi ngay khi tên lửa vừa sinh ra
        if (flyingSFX != null)
        {
            // Ép âm thanh phát tại vị trí của Camera để người chơi luôn nghe thấy rõ nhất với âm lượng 70% (0.7f)
            AudioSource.PlayClipAtPoint(flyingSFX, Camera.main.transform.position, 0.7f);
        }
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

        if (Vector3.Distance(transform.position, targetBlock.transform.position) < 0.5f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 2. PHÁT TIẾNG NỔ: Gọi ngay trước khi xử lý logic hất văng và hủy tên lửa
        if (explosionSFX != null)
        {
            // Phát tiếng nổ với âm lượng tối đa 100% (1f)
            AudioSource.PlayClipAtPoint(explosionSFX, Camera.main.transform.position, 1f);
        }

        if (explosionVFX != null)
        {
            Vector3 vfxPosition = new Vector3(transform.position.x, transform.position.y, -2f);
            GameObject vfx = Instantiate(explosionVFX, vfxPosition, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * explosionScale;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        bool hitAnyBlock = false;

        foreach (Collider hit in colliders)
        {
            Block block = hit.GetComponentInParent<Block>();
            
            if (block != null && hit.attachedRigidbody != null)
            {
                hitAnyBlock = true;
                hit.attachedRigidbody.isKinematic = false;
                hit.attachedRigidbody.WakeUp();
                hit.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, upliftModifier, ForceMode.Impulse);
            }
        }

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