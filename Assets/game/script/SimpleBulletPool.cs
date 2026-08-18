using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBulletPool : MonoBehaviour
{
    public static SimpleBulletPool Instance { get; private set; }

    [System.Serializable]
    public class PoolItem
    {
        public string name;
        public GameObject prefab;
        public int initialSize = 5;
        public float autoReturnDelay = 1.5f; // <--- Hiện ô điền thời gian ở Inspector
    }

    [Header("1. Danh sách Prefab nạp sẵn (Bấm + để thêm VFX/Đạn)")]
    [SerializeField] private List<PoolItem> prewarmItems = new List<PoolItem>();

    [Header("2. Đạn mặc định (Dành cho SimpleCannon.cs)")]
    [SerializeField] private GameObject defaultBulletPrefab;

    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, float> autoReturnTimes = new Dictionary<GameObject, float>();

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

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var item in prewarmItems)
        {
            if (item.prefab == null) continue;

            // Lưu cấu hình thời gian hủy của Prefab này
            if (!autoReturnTimes.ContainsKey(item.prefab))
            {
                autoReturnTimes.Add(item.prefab, item.autoReturnDelay);
            }

            for (int i = 0; i < item.initialSize; i++)
            {
                CreateNewInstance(item.prefab);
            }
        }

        if (defaultBulletPrefab != null && !poolDictionary.ContainsKey(defaultBulletPrefab))
        {
            for (int i = 0; i < 10; i++)
            {
                CreateNewInstance(defaultBulletPrefab);
            }
        }
    }

    private GameObject CreateNewInstance(GameObject prefab)
    {
        if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolDictionary[prefab] = queue;
        }

        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        queue.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// Spawn Object.
    /// - Nếu không truyền autoReturnDelay, Pool sẽ tự lấy thời gian cấu hình ở Inspector.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float overrideDelay = -1f)
    {
        if (prefab == null) return null;

        if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolDictionary[prefab] = queue;
        }

        GameObject obj = null;
        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null) break;
        }

        if (obj == null)
        {
            obj = Instantiate(prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(null);
        obj.SetActive(true);

        // Xác định thời gian tự hủy: Ưu tiên tham số truyền vào, nếu không có thì lấy cấu hình Inspector
        float delay = overrideDelay;
        if (delay <= 0f && autoReturnTimes.TryGetValue(prefab, out float defaultDelay))
        {
            delay = defaultDelay;
        }

        if (delay > 0f)
        {
            StartCoroutine(AutoReturnRoutine(prefab, obj, delay));
        }

        return obj;
    }

    private IEnumerator AutoReturnRoutine(GameObject prefab, GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (instance != null && instance.activeSelf)
        {
            ReturnToPool(prefab, instance);
        }
    }

    public void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null || !instance.activeSelf) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform);

        if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolDictionary[prefab] = queue;
        }

        if (!queue.Contains(instance))
        {
            queue.Enqueue(instance);
        }
    }

    public GameObject GetBullet()
    {
        return Spawn(defaultBulletPrefab, transform.position, transform.rotation);
    }

    public void ReturnBullet(GameObject bullet)
    {
        ReturnToPool(defaultBulletPrefab, bullet);
    }
}