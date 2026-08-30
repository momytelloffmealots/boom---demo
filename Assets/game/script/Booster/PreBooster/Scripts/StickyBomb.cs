using UnityEngine;

public class StickyBomb : MonoBehaviour
{
    [Header("Cài đặt bay")]
    public float speed = 8f;            // Tốc độ bay mặc định là 8
    public float defaultScale = 5f;     // Scale mặc định là 5
    public GameObject explosionVFX;    

    private Block targetBlock;

    private void Start()
    {
        // Tự động chỉnh kích thước bọ bằng 5 khi vừa sinh ra
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

        // Bay từ ngoài màn hình vào với tốc độ 8
        transform.position = Vector3.MoveTowards(transform.position, targetBlock.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetBlock.transform.position) < 0.2f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        if (targetBlock != null)
        {
            targetBlock.ForceDestroy();
        }
        
        Destroy(gameObject);
    }
}