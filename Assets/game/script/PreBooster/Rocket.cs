using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [Header("Cài đặt Bay Uốn Lượn")]
    public float speed = 9f;
    [Tooltip("Độ rộng của nhịp uốn lượn sang hai bên")]
    public float waveAmplitude = 0.85f;
    [Tooltip("Số nhịp uốn lượn trên suốt quãng đường bay")]
    public float waveFrequency = 1.8f;
    [Tooltip("Độ tản rộng ban đầu để các tên lửa không đè đường bay của nhau")]
    public float spreadArc = 0.7f;

    [Header("Cài đặt Hiển thị & VFX")]
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
    [Tooltip("Bán kính phá vỡ thủy tinh trực tiếp (0.6 = phạm vi 1 ô quanh điểm chạm)")]
    public float glassBreakRadius = 0.6f;

    private Block targetBlock;

    // Biến điều khiển quỹ đạo uốn lượn
    private Vector3 startPos;
    private Vector3 lastKnownTargetPos;
    private float flightProgress = 0f;
    private float flightDuration = 0.75f;
    private float sideSign = 1f;
    private float randomFreqMultiplier = 1f;
    private float randomPhase = 0f;
    private static int spawnCounter = 0;

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
        startPos = transform.position;

        FindNewTarget();
        InitializeWeaveTrajectory();

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

    private void InitializeWeaveTrajectory()
    {
        lastKnownTargetPos = targetBlock != null ? targetBlock.transform.position : startPos + Vector3.up * 5f;

        float distance = Vector3.Distance(startPos, lastKnownTargetPos);
        flightDuration = Mathf.Max(0.4f, (distance * 1.15f) / Mathf.Max(1f, speed));
        flightProgress = 0f;

        spawnCounter++;

        if (startPos.x < -0.15f)
        {
            sideSign = -1f;
        }
        else if (startPos.x > 0.15f)
        {
            sideSign = 1f;
        }
        else
        {
            sideSign = (spawnCounter % 2 == 0) ? 1f : -1f;
        }

        randomFreqMultiplier = Random.Range(0.85f, 1.15f);
        randomPhase = (spawnCounter % 3) * 0.6f;
    }

    private void Update()
    {
        if (targetBlock != null && targetBlock.gameObject.activeInHierarchy)
        {
            lastKnownTargetPos = targetBlock.transform.position;
        }
        else if (flightProgress < 0.6f)
        {
            FindNewTarget();
            if (targetBlock != null)
            {
                lastKnownTargetPos = targetBlock.transform.position;
            }
        }

        flightProgress += Time.deltaTime / flightDuration;
        float t = Mathf.Clamp01(flightProgress);

        float easedT = Mathf.Lerp(t, t * t, 0.25f);

        Vector3 nextPos = CalculateWeavePosition(easedT, startPos, lastKnownTargetPos);

        Vector3 moveDirection = nextPos - transform.position;
        if (moveDirection.sqrMagnitude > 0.00001f)
        {
            transform.up = moveDirection.normalized;
        }

        transform.position = nextPos;

        if (t >= 1f || (t > 0.8f && Vector3.Distance(transform.position, lastKnownTargetPos) < 0.4f))
        {
            Explode();
        }
    }

    private Vector3 CalculateWeavePosition(float t, Vector3 p0, Vector3 p2)
    {
        Vector3 toTarget = p2 - p0;
        Vector3 forward = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector3.up;
        Vector3 side = new Vector3(-forward.y, forward.x, 0f);

        Vector3 basePos = Vector3.Lerp(p0, p2, t);
        float envelope = Mathf.Sin(t * Mathf.PI);

        float arcOffset = spreadArc * sideSign * envelope;
        float waveAngle = (t * waveFrequency * randomFreqMultiplier * Mathf.PI * 2f) + randomPhase;
        float waveOffset = Mathf.Sin(waveAngle) * waveAmplitude * sideSign * envelope;

        return basePos + side * (arcOffset + waveOffset);
    }

    private void FindNewTarget()
    {
        Block[] allBlocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        if (allBlocks.Length == 0)
        {
            targetBlock = null;
            return;
        }

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

        if (candidatePool.Count == 0)
        {
            candidatePool.AddRange(validBlocks);
        }

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

        if (wellSpacedBlocks.Count > 0)
        {
            targetBlock = PickWeightedRandomBlock(wellSpacedBlocks, centerX, halfWidth, minY, Mathf.Max(0.01f, height));
        }
        else
        {
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

    private Block PickWeightedRandomBlock(List<Block> blocks, float centerX, float halfWidth, float minY, float height)
    {
        float totalWeight = 0f;
        float[] weights = new float[blocks.Count];

        for (int i = 0; i < blocks.Count; i++)
        {
            Vector3 pos = blocks[i].transform.position;
            float edgeFactor = Mathf.Clamp01(Mathf.Abs(pos.x - centerX) / halfWidth);
            float heightFactor = Mathf.Clamp01((pos.y - minY) / height);

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

    private void Explode()
    {
        activeRockets.Remove(this);

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

        // Lấy tâm chuẩn tại viên gạch mục tiêu để đo phạm vi 1 ô chính xác
        Vector3 impactCenter = targetBlock != null ? targetBlock.transform.position : transform.position;

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        bool hitAnyBlock = false;

        foreach (Collider hit in colliders)
        {
            Block block = hit.GetComponentInParent<Block>();
            if (block == null) continue;

            hitAnyBlock = true;
            Vector3 hitPoint = hit.ClosestPoint(transform.position);

            // Chỉ làm vỡ thủy tinh nếu là đúng viên gạch mục tiêu hoặc nằm trong bán kính 1 ô (glassBreakRadius)
            bool isWithinOneTile = (block == targetBlock) ||
                                   (Vector3.Distance(impactCenter, block.transform.position) <= glassBreakRadius);

            if (isWithinOneTile && block.TryBreakFromExplosion(hitPoint))
            {
                continue;
            }

            // Các block nằm ngoài phạm vi 1 ô chỉ chịu lực đẩy vật lý bình thường
            if (hit.attachedRigidbody != null)
            {
                hit.attachedRigidbody.isKinematic = false;
                hit.attachedRigidbody.WakeUp();
                hit.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, upliftModifier, ForceMode.Impulse);
            }
        }

        if (!hitAnyBlock && targetBlock != null)
        {
            if (!targetBlock.TryBreakFromExplosion(targetBlock.transform.position))
            {
                Rigidbody rb = targetBlock.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.WakeUp();
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upliftModifier, ForceMode.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }
}