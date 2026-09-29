using UnityEngine;

public class Rocket : MonoBehaviour
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
    
    // 🔥 Bộ lọc thời gian chống rè âm thanh khi spawn/nổ nhiều tên lửa cùng lúc
    private static float lastFlySoundTime = -1f;
    private static float lastExplosionSoundTime = -1f;

    private void Start()
    {
        transform.localScale = Vector3.one * defaultScale;
        FindNewTarget();

        // CHỐNG RÈ: Chỉ phát tiếng bay nếu khoảng cách với lần phát trước đó lớn hơn 0.1 giây
        if (AudioManager.Instance != null)
        {
            if (Time.time - lastFlySoundTime > 0.1f)
            {
                // Gọi thẳng tiếng rít gió từ AudioManager trung tâm
                AudioManager.Instance.PlayRocketFly();
                lastFlySoundTime = Time.time;
            }
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
        // CHỐNG RÈ: Chỉ phát tiếng nổ nếu khoảng cách với lần phát trước đó lớn hơn 0.1 giây
        if (AudioManager.Instance != null)
        {
            if (Time.time - lastExplosionSoundTime > 0.1f)
            {
                // Gọi thẳng tiếng nổ từ AudioManager trung tâm
                AudioManager.Instance.PlayRocketExplode();
                lastExplosionSoundTime = Time.time;
            }
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