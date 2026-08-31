using UnityEngine;

public class StickyBomb : MonoBehaviour
{
    [Header("Cài đặt bay")]
    public float speed = 8f;            // Tốc độ bay mặc định là 8
    public float defaultScale = 5f;     // Kích thước bọ là 5
    public float explosionScale = 1.6f; // Kích thước nổ (bằng 1/3 của 5)
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

        // Bay từ ngoài màn hình vào với tốc độ
        transform.position = Vector3.MoveTowards(transform.position, targetBlock.transform.position, speed * Time.deltaTime);

        // Kiểm tra khoảng cách
        if (Vector3.Distance(transform.position, targetBlock.transform.position) < 0.2f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionVFX != null)
        {
            // Kéo hiệu ứng nổ nhích về phía camera một chút để tuyệt đối không bị gạch che khuất
            Vector3 vfxPosition = transform.position + new Vector3(0, 0, -1.5f);
            
            // Sinh ra hiệu ứng
            GameObject vfx = Instantiate(explosionVFX, vfxPosition, Quaternion.identity);

            // 🔥 ÁP DỤNG KÍCH THƯỚC NỔ MỚI (Nhỏ đi 2/3)
            vfx.transform.localScale = Vector3.one * explosionScale;
        }

        // Phá hủy cục gạch
        if (targetBlock != null)
        {
            targetBlock.ForceDestroy();
        }
        
        // Tự hủy con bọ
        Destroy(gameObject);
    }
}