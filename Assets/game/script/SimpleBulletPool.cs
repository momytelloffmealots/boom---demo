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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
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
        GameObject bullet = bulletPool.Count > 0 ? bulletPool.Dequeue() : CreateNewBullet();
        bullet.SetActive(true);
        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}