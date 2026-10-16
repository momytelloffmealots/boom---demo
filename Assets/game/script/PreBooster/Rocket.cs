using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [Header("Cài đặt bay")]
    public float speed = 8f;            
    public float defaultScale = 0.07f; 
    public float explosionScale = 1.6f; 
    public GameObject explosionVFX;    

    [Header("Cài đặt Chọn Mục Tiêu")]
    [Tooltip("Khoảng cách tối thiểu giữa các điểm nổ của các tên lửa")]
    public float minTargetDistance = 1.5f;

    [Header("Lực nổ phụ (Vật lý)")]
    public float explosionForce = 2000f;
    public float explosionRadius = 3f;
    public float upliftModifier = 0.5f;

    private Block targetBlock;

    // Danh sách quản lý các tên lửa đang bay để tránh chọn trùng/gần điểm chạm
    private static readonly List<Rocket> activeRockets = new List<Rocket>();
    
    // Bộ lọc thời gian chống rè âm thanh khi spawn/nổ nhiều tên lửa cùng lúc
    private static float lastFlySoundTime = -1f;
    private static float lastExplosionSoundTime = -1f;

    private void OnEnable()
    {
        if (!activeRockets.Contains(this))
        {
            activeRockets.Add(this);
        }
    }

    private void OnDestroy()
    {
        activeRockets.Remove(this);
    }

    private void Start()
    {
        transform.localScale = Vector3.one * defaultScale;
        FindNewTarget();

        // CHỐNG RÈ: Chỉ phát tiếng bay nếu khoảng cách với lần phát trước đó lớn hơn 0.1 giây
        if (AudioManager.Instance != null)
        {
            if (Time.time - lastFlySoundTime > 0.1f)
            {
                AudioManager.Instance.PlayRocketFly();
                lastFlySoundTime = Time.time;
            }
        }
    }

    private void FindNewTarget()
    {
        Block[] allBlocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        if (allBlocks.Length == 0)
        {
            targetBlock = null;
            return;
        }

        // 1. Tìm độ cao thấp nhất (minY) và cao nhất (maxY) của cấu trúc gạch hiện tại
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Block b in allBlocks)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            float y = b.transform.position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        float midY = (minY + maxY) * 0.5f;

        // 2. Lọc ra các block nằm ở một nửa bên trên (y >= midY)
        List<Block> upperBlocks = new List<Block>();
        foreach (Block b in allBlocks)
        {
            if (b != null && b.gameObject.activeInHierarchy && b.transform.position.y >= midY)
            {
                upperBlocks.Add(b);
            }
        }

        // Nếu nửa trên không có block nào hợp lệ thì dùng toàn bộ danh sách làm dự phòng
        List<Block> candidatePool = upperBlocks.Count > 0 ? upperBlocks : new List<Block>(allBlocks);

        // 3. Lọc các block đảm bảo khoảng cách tối thiểu (minTargetDistance) với các tên lửa khác
        List<Block> wellSpacedBlocks = new List<Block>();

        foreach (Block candidate in candidatePool)
        {
            if (candidate == null) continue;

            bool isFarEnough = true;
            foreach (Rocket otherRocket in activeRockets)
            {
                if (otherRocket == null || otherRocket == this || otherRocket.targetBlock == null) continue;

                float dist = Vector3.Distance(candidate.transform.position, otherRocket.targetBlock.transform.position);
                if (dist < minTargetDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
            {
                wellSpacedBlocks.Add(candidate);
            }
        }

        // 4. Chọn mục tiêu
        if (wellSpacedBlocks.Count > 0)
        {
            // Nếu có các block ở nửa trên và cách đủ xa nhau -> Chọn ngẫu nhiên trong nhóm này
            targetBlock = wellSpacedBlocks[Random.Range(0, wellSpacedBlocks.Count)];
        }
        else
        {
            // Dự phòng (khi tháp gạch quá hẹp): Chọn block ở nửa trên có khoảng cách xa các tên lửa khác nhất
            Block farthestBlock = null;
            float bestMinDistance = -1f;

            foreach (Block candidate in candidatePool)
            {
                if (candidate == null) continue;

                float minDistToOthers = float.MaxValue;
                foreach (Rocket otherRocket in activeRockets)
                {
                    if (otherRocket == null || otherRocket == this || otherRocket.targetBlock == null) continue;

                    float dist = Vector3.Distance(candidate.transform.position, otherRocket.targetBlock.transform.position);
                    if (dist < minDistToOthers)
                    {
                        minDistToOthers = dist;
                    }
                }

                if (minDistToOthers > bestMinDistance)
                {
                    bestMinDistance = minDistToOthers;
                    farthestBlock = candidate;
                }
            }

            targetBlock = farthestBlock != null ? farthestBlock : candidatePool[Random.Range(0, candidatePool.Count)];
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
        // Giải phóng mục tiêu khỏi danh sách trước khi nổ
        activeRockets.Remove(this);

        // CHỐNG RÈ: Chỉ phát tiếng nổ nếu khoảng cách với lần phát trước đó lớn hơn 0.1 giây
        if (AudioManager.Instance != null)
        {
            if (Time.time - lastExplosionSoundTime > 0.1f)
            {
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