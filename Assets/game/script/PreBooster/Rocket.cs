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
    [Tooltip("Bỏ qua phần đáy tháp (0.15 = bỏ qua 15% chiều cao dưới cùng sát mặt sàn)")]
    [Range(0f, 0.4f)] public float ignoreBottomRatio = 0.15f;

    [Header("Lực nổ phụ (Vật lý)")]
    public float explosionForce = 2000f;
    public float explosionRadius = 3f;
    public float upliftModifier = 0.5f;

    private Block targetBlock;

    // Danh sách quản lý các tên lửa đang bay để phối hợp chia mục tiêu
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

        // 1. Tính toán khung giới hạn (Bounding Box) của toàn bộ tháp gạch
        float minY = float.MaxValue, maxY = float.MinValue;
        float minX = float.MaxValue, maxX = float.MinValue;
        List<Block> validBlocks = new List<Block>();

        foreach (Block b in allBlocks)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            validBlocks.Add(b);

            Vector3 pos = b.transform.position;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
        }

        if (validBlocks.Count == 0)
        {
            targetBlock = null;
            return;
        }

        float height = maxY - minY;
        float midY = minY + height * 0.5f;
        float bottomCutoffY = minY + height * ignoreBottomRatio;
        float centerX = (minX + maxX) * 0.5f;
        float halfWidth = Mathf.Max(0.01f, (maxX - minX) * 0.5f);

        // 2. Kiểm tra xem trong các tên lửa đang bay đã có quả nào nhắm vào nửa trên (y >= midY) chưa
        bool hasRocketInUpperHalf = false;
        foreach (Rocket other in activeRockets)
        {
            if (other != null && other != this && other.targetBlock != null)
            {
                if (other.targetBlock.transform.position.y >= midY)
                {
                    hasRocketInUpperHalf = true;
                    break;
                }
            }
        }

        // 3. Xây dựng tập hợp mục tiêu theo điều kiện:
        // - Nếu CHƯA có tên lửa nào ở nửa trên -> Bắt buộc quả này lấy từ giữa đổ lên (y >= midY)
        // - Nếu ĐÃ có ít nhất 1 quả ở nửa trên -> Cho phép chọn ngẫu nhiên toàn tháp (trừ hàng sát đáy)
        List<Block> candidatePool = new List<Block>();

        if (!hasRocketInUpperHalf)
        {
            foreach (Block b in validBlocks)
            {
                if (b.transform.position.y >= midY)
                {
                    candidatePool.Add(b);
                }
            }
        }
        else
        {
            foreach (Block b in validBlocks)
            {
                if (b.transform.position.y >= bottomCutoffY)
                {
                    candidatePool.Add(b);
                }
            }
        }

        // Dự phòng nếu tháp quá thấp khiến danh sách lọc bị rỗng
        if (candidatePool.Count == 0)
        {
            candidatePool.AddRange(validBlocks);
        }

        // 4. Lọc các block đảm bảo khoảng cách tối thiểu (minTargetDistance) với các tên lửa khác
        List<Block> wellSpacedBlocks = new List<Block>();

        foreach (Block candidate in candidatePool)
        {
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

        // 5. Chọn mục tiêu (Kết hợp ngẫu nhiên có trọng số ưu tiên mép ngoài và tầng cao để dễ đổ gạch)
        if (wellSpacedBlocks.Count > 0)
        {
            targetBlock = PickWeightedRandomBlock(wellSpacedBlocks, centerX, halfWidth, minY, Mathf.Max(0.01f, height));
        }
        else
        {
            // Dự phòng (khi tháp gạch quá hẹp): Chọn block cách xa các tên lửa khác nhất
            Block farthestBlock = null;
            float bestMinDistance = -1f;

            foreach (Block candidate in candidatePool)
            {
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

    /// <summary>
    /// Chọn ngẫu nhiên nhưng ưu tiên các viên gạch nằm ở rìa trái/phải hoặc trên cao
    /// để tránh việc tên lửa đâm vào giữa ruột tháp bị các viên xung quanh kẹp cứng.
    /// </summary>
    private Block PickWeightedRandomBlock(List<Block> blocks, float centerX, float halfWidth, float minY, float height)
    {
        float totalWeight = 0f;
        float[] weights = new float[blocks.Count];

        for (int i = 0; i < blocks.Count; i++)
        {
            Vector3 pos = blocks[i].transform.position;
            // Độ lệch ra 2 mép trái/phải (0 ở tâm, 1 ở rìa ngoài cùng)
            float edgeFactor = Mathf.Clamp01(Mathf.Abs(pos.x - centerX) / halfWidth);
            // Độ cao tương đối (0 ở đáy, 1 ở đỉnh)
            float heightFactor = Mathf.Clamp01((pos.y - minY) / height);

            // Trọng số cơ bản là 1, cộng thêm điểm nếu nằm ở rìa hoặc trên cao
            float weight = 1f + (edgeFactor * 1.5f) + (heightFactor * 1.0f);
            weights[i] = weight;
            totalWeight += weight;
        }

        float randomVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        for (int i = 0; i < blocks.Count; i++)
        {
            currentSum += weights[i];
            if (randomVal <= currentSum)
            {
                return blocks[i];
            }
        }

        return blocks[blocks.Count - 1];
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