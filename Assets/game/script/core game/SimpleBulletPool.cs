using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Object Pool duoc dung chung cho Bullet va VFX.
// Bullet.cs TU quan ly thoi gian song. Pool chi auto-return cac object khong co Bullet.
public class SimpleBulletPool : MonoBehaviour
{
    public static SimpleBulletPool Instance { get; private set; }

    [System.Serializable]
    public class PoolItem
    {
        public string name;
        public GameObject prefab;
        public int initialSize = 5;
        public float autoReturnDelay = 1.5f;
    }

    [Header("1. Danh sach Prefab nap san")]
    [SerializeField] private List<PoolItem> prewarmItems = new List<PoolItem>();

    [Header("2. Dan mac dinh (SimpleCannon)")]
    [SerializeField] private GameObject defaultBulletPrefab;
    [SerializeField] private int bulletSize = 60;

    [Header("3. Cau hinh Object Pool")]
    [SerializeField] private bool expandPoolWhenEmpty = true;
    [SerializeField] private float defaultVfxReturnDelay = 1.5f;

    private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary =
        new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, HashSet<GameObject>> poolHashSet =
        new Dictionary<GameObject, HashSet<GameObject>>();
    private readonly Dictionary<GameObject, float> autoReturnTimes =
        new Dictionary<GameObject, float>();
    private readonly Dictionary<GameObject, int> spawnVersions =
        new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, GameObject> instancePrefabs =
        new Dictionary<GameObject, GameObject>();
    private readonly Dictionary<float, WaitForSeconds> waitCache =
        new Dictionary<float, WaitForSeconds>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[BulletPool] DUPLICATE: Da ton tai Pool '" +
                Instance.gameObject.name + "' (scene " + Instance.gameObject.scene.name +
                "), Pool moi '" + gameObject.name + "' bi huy. Kiem tra DontDestroyOnLoad.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePools();
        Debug.Log("[BulletPool] INIT " + GetBulletPoolDebugInfo() +
                  " in scene " + SceneManager.GetActiveScene().name, this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsurePool(GameObject prefab)
    {
        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary[prefab] = new Queue<GameObject>();

        if (!poolHashSet.ContainsKey(prefab))
            poolHashSet[prefab] = new HashSet<GameObject>();
    }

    private WaitForSeconds GetWait(float delay)
    {
        if (!waitCache.TryGetValue(delay, out WaitForSeconds wait))
        {
            wait = new WaitForSeconds(delay);
            waitCache.Add(delay, wait);
        }
        return wait;
    }

    private void InitializePools()
    {
        foreach (PoolItem item in prewarmItems)
        {
            if (item.prefab == null) continue;
            autoReturnTimes[item.prefab] = item.autoReturnDelay;
            for (int i = 0; i < Mathf.Max(0, item.initialSize); i++)
                CreateNewInstance(item.prefab);
        }

        if (defaultBulletPrefab == null)
        {
            Debug.LogError("[BulletPool] Chua gan Default Bullet Prefab trong Inspector!", this);
            return;
        }

        EnsurePool(defaultBulletPrefab);
        int missing = Mathf.Max(0, bulletSize - poolDictionary[defaultBulletPrefab].Count);
        for (int i = 0; i < missing; i++)
            CreateNewInstance(defaultBulletPrefab);
    }

    private GameObject CreateNewInstance(GameObject prefab)
    {
        EnsurePool(prefab);
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        instancePrefabs[obj] = prefab;
        poolHashSet[prefab].Add(obj);
        poolDictionary[prefab].Enqueue(obj);
        return obj;
    }

    // overrideDelay > 0: thoi gian tra ve tu dong cho VFX.
    // Bullet.cs co lifecycle rieng nen Pool KHONG dat timer cho Bullet.
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float overrideDelay = -1f)
    {
        if (prefab == null)
        {
            Debug.LogError("[BulletPool] Spawn nhan prefab null.", this);
            return null;
        }

        EnsurePool(prefab);
        Queue<GameObject> queue = poolDictionary[prefab];
        HashSet<GameObject> available = poolHashSet[prefab];
        GameObject obj = null;

        while (queue.Count > 0)
        {
            GameObject candidate = queue.Dequeue();
            available.Remove(candidate);
            if (candidate == null) continue;

            if (!candidate.activeSelf)
            {
                obj = candidate;
                break;
            }

            Debug.LogWarning("[BulletPool] Phat hien object active trong Queue: " + candidate.name, this);
        }

        if (obj == null)
        {
            if (!expandPoolWhenEmpty)
            {
                Debug.LogWarning("[BulletPool] Pool da het: " + prefab.name +
                                 ". Hay tang Bullet Size hoac bat Expand Pool When Empty.", this);
                return null;
            }

            // Chi tao them khi Queue that su het. Ve sau object moi van duoc tai su dung.
            obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            instancePrefabs[obj] = prefab;
        }

        obj.transform.SetParent(null);
        obj.transform.SetPositionAndRotation(position, rotation);

        // Version chan coroutine cu thu hoi mot object da duoc Spawn lai.
        spawnVersions.TryGetValue(obj, out int oldVersion);
        int version = oldVersion + 1;
        spawnVersions[obj] = version;

        // Bullet.OnEnable se khoi tao Rigidbody va timer cho moi lan spawn.
        obj.SetActive(true);

        // KHONG su dung timer cua Pool voi Bullet, tranh hai bo dem thoi gian tranh chap.
        if (obj.GetComponent<Bullet>() == null)
        {
            float delay = overrideDelay;
            if (delay <= 0f)
            {
                if (!autoReturnTimes.TryGetValue(prefab, out delay))
                    delay = defaultVfxReturnDelay;
            }

            if (delay > 0f)
                StartCoroutine(AutoReturnWithVersion(prefab, obj, delay, version));
        }

        return obj;
    }

    private IEnumerator AutoReturnWithVersion(GameObject prefab, GameObject instance, float delay, int version)
    {
        yield return GetWait(delay);
        if (instance == null) yield break;
        if (!spawnVersions.TryGetValue(instance, out int currentVersion) || currentVersion != version)
            yield break;
        ReturnToPool(prefab, instance);
    }

    // API cu duoc giu lai cho nhung script VFX khac neu dang dung.
    public IEnumerator AutoReturnRoutine(GameObject prefab, GameObject instance, float delay)
    {
        if (instance == null) yield break;
        spawnVersions.TryGetValue(instance, out int version);
        yield return AutoReturnWithVersion(prefab, instance, delay, version);
    }

    public void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;

        // Chan nham tham so prefab/instance de bao ve pool.
        if (instancePrefabs.TryGetValue(instance, out GameObject realPrefab) && realPrefab != prefab)
        {
            Debug.LogError("[BulletPool] ReturnToPool sai prefab cho object " + instance.name, this);
            return;
        }

        if (!instancePrefabs.ContainsKey(instance))
        {
            Debug.LogError("[BulletPool] Object khong do Pool nay tao: " + instance.name, this);
            return;
        }

        // Neu Pool dang bi tat khi chuyen scene: dung, KHONG reparent / enqueue.
        // Bullet.OnDisable da khong con goi ReturnToPool nua.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy ||
            !gameObject.scene.isLoaded || !instance.scene.isLoaded)
        {
            if (instance.activeSelf)
                instance.SetActive(false);
            return;
        }

        EnsurePool(prefab);

        // Danh dau TRUOC SetActive(false), chan enqueue trung object.
        if (!poolHashSet[prefab].Add(instance)) return;

        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (instance.activeSelf)
            instance.SetActive(false);

        // CO Y KHONG SetParent khi tra object ve Pool.
        // Transform.SetParent trong luc OnDisable/scene unload gay loi Unity.
        // Queue/HashSet quan ly object; object inactive van duoc tai su dung.
        poolDictionary[prefab].Enqueue(instance);
    }

    // Diagnostic: F8 tren Cannon cho biet con bao nhieu vien trong Pool.
    public string GetBulletPoolDebugInfo()
    {
        if (defaultBulletPrefab == null) return "Pool: default prefab=NULL";
        int available = 0;
        if (poolHashSet.TryGetValue(defaultBulletPrefab, out HashSet<GameObject> set))
            available = set.Count;
        int total = 0;
        foreach (var pair in instancePrefabs)
        {
            if (pair.Key != null && pair.Value == defaultBulletPrefab)
                total++;
        }
        return $"Pool available={available}, total={total}, expand={expandPoolWhenEmpty}";
    }

    // API moi: dat dung vi tri truoc khi OnEnable cua Bullet duoc goi.
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        return Spawn(defaultBulletPrefab, position, rotation);
    }

    // API cu: van ho tro neu script khac goi GetBullet() khong tham so.
    public GameObject GetBullet()
    {
        return GetBullet(transform.position, transform.rotation);
    }

    public void ReturnBullet(GameObject bullet)
    {
        ReturnToPool(defaultBulletPrefab, bullet);
    }
}
