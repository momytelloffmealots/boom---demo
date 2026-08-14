using System.Collections.Generic;
using UnityEngine;

public class SimpleBulletPool : MonoBehaviour
{
    public static SimpleBulletPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<GameObject> bulletPool = new Queue<GameObject>();

    private void Awake()
    {
        Debug.Log($"[SimpleBulletPool] Awake called on '{gameObject.name}'. Current Instance: {(Instance != null ? Instance.gameObject.name : "null")}", this);
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"[SimpleBulletPool] Duplicate instance found on '{gameObject.name}' (current active instance is '{Instance.gameObject.name}'). Destroying duplicate GameObject.", this);
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        if (bulletPrefab == null) return;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBullet();
        }
    }

    private GameObject CreateNewBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform);
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
        return bullet;
    }

    public GameObject GetBullet()
    {
        GameObject bullet = null;
        // Lấy đạn ra cho đến khi tìm thấy đạn hợp lệ (tránh lỗi null nếu đạn bị hủy bên ngoài)
        while (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
            if (bullet != null)
            {
                bullet.transform.SetParent(null); // Đưa ra world space để tránh bị ảnh hưởng scale của Pool
                break;
            }
        }

        // Nếu hết đạn trong pool, tự động tạo mới
        if (bullet == null)
        {
            bullet = Instantiate(bulletPrefab);
        }

        bullet.SetActive(true);
        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        bullet.SetActive(false);
        bullet.transform.SetParent(transform); // Đưa lại làm con của Pool để phân cấp gọn gàng

        if (!bulletPool.Contains(bullet))
        {
            bulletPool.Enqueue(bullet);
        }
    }
}