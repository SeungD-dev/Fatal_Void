using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize;
        [Tooltip("Ǯ�� �ִ� ũ�� (0 = ������)")]
        public int maxSize;
        [Tooltip("Ǯ�� ����� �� �� ���� ������ ������Ʈ ��")]
        public int growSize = 5;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;
    private Dictionary<GameObject, string> objectToTagMap; // ������Ʈ�� �±׿� �����ϴ� ��ųʸ�

    // Scene ���������� ����ȭ�ϱ� ���� �ɼ�
    [Tooltip("true: Ǯ ������Ʈ�� ���� �������� �и�, false: ���� ��Ĵ�� ���� ���� ����")]
    [SerializeField] private bool useOptimizedHierarchy = true;
    [Tooltip("false�� ������ ������Ʈ Ǯ�� ������� ����� �� ������ ������ ���˴ϴ�")]

    // Ǯ �����̳� ĳ�� (���� ��Ŀ����� ���)
    private Dictionary<string, Transform> poolContainers;

    // ��Ȱ��ȭ�� ������Ʈ�� �θ� Transform (����ȭ ��忡���� ������� ����)
    private Transform inactiveObjectsParent;
    private Transform playerTransform;
    private static ObjectPool instance;
    public static ObjectPool Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // DontDestroyOnLoad �߰�
            InitializePools();
        }
        else if (instance != this) // �� üũ �߰�
        {
            // ������ �ν��Ͻ��� �����ϸ� ���� ������Ʈ ����
            Debug.LogWarning("Multiple ObjectPool instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();
        objectToTagMap = new Dictionary<GameObject, string>();
        poolContainers = new Dictionary<string, Transform>();

        // ���� ��Ŀ����� �ڱ� �ڽ��� �θ�� ����
        inactiveObjectsParent = transform;

        foreach (Pool pool in pools)
        {
            CreateNewPool(pool);
        }
    }
    public void SetPlayerReference(Transform player)
    {
        playerTransform = player;
    }

    private void CreateNewPool(Pool poolConfig)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>();

        // ����ȭ ��忡���� ���� �����̳ʸ� �������� ����
        if (!useOptimizedHierarchy)
        {
            GameObject poolContainer = new GameObject($"Pool-{poolConfig.tag}");
            poolContainer.transform.SetParent(transform);
            poolContainers[poolConfig.tag] = poolContainer.transform;
        }

        for (int i = 0; i < poolConfig.initialSize; i++)
        {
            GameObject obj = CreateNewPoolObject(poolConfig.prefab, poolConfig.tag);
            objectPool.Enqueue(obj);
        }

        poolDictionary[poolConfig.tag] = objectPool;
        poolConfigs[poolConfig.tag] = poolConfig;
    }

    private GameObject CreateNewPoolObject(GameObject prefab, string tag)
    {
        GameObject obj = Instantiate(prefab);

        // ����ȭ ��忡���� ���� �������� ������ �и�
        if (useOptimizedHierarchy)
        {
            // Scene���� ��Ʈ ������ �����Ͽ� Transform ���� �ּ�ȭ
            obj.transform.SetParent(null);
        }
        else
        {
            // ���� ��Ĵ�� Ǯ �����̳��� �ڽ����� ����
            obj.transform.SetParent(poolContainers[tag]);
        }

        objectToTagMap[obj] = tag; // ������Ʈ�� �±� ���� ����
        obj.SetActive(false);
        return obj;
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        Pool config = poolConfigs[tag];

        GameObject objectToSpawn;

        // Ǯ�� ����� �� ó��
        if (pool.Count == 0)
        {
            // ���� �ڵ�...
            objectToSpawn = CreateNewPoolObject(config.prefab, tag);
        }
        else
        {
            objectToSpawn = pool.Dequeue();
            if (objectToSpawn == null) // Ǯ�� �ִ� ������Ʈ�� �ı��� ���
            {
                objectToSpawn = CreateNewPoolObject(config.prefab, tag);
            }
        }

        // ������Ʈ ��ġ �� ȸ�� ���� (SetActive ���� �����Ͽ� ���ʿ��� �̺�Ʈ ȣ�� ����)
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Enemy ������Ʈ�� �ְ� �÷��̾� ������ ������ �ʱ�ȭ
        Enemy enemy = objectToSpawn.GetComponent<Enemy>();
        if (enemy != null && playerTransform != null)
        {
            enemy.Initialize(playerTransform);
        }

        objectToSpawn.SetActive(true);

        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        // �±� ������ ���� ������Ʈ�� ���� Ǯ ã��
        if (!objectToTagMap.TryGetValue(objectToReturn, out string tag))
        {
            Debug.LogWarning($"Object not managed by pool: {objectToReturn.name}");
            return;
        }

        ReturnToPool(tag, objectToReturn);
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);

        // ����ȭ ���: ���� �������� ������ �и�
        if (!useOptimizedHierarchy)
        {
            // ���� ��Ŀ����� Ǯ �����̳��� �ڽ����� ����
            objectToReturn.transform.SetParent(poolContainers[tag]);
        }

        poolDictionary[tag].Enqueue(objectToReturn);
    }

    public void CreatePool(string tag, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} already exists.");
            return;
        }

        Pool newPool = new Pool
        {
            tag = tag,
            prefab = prefab,
            initialSize = size,
            maxSize = 0, // ������
            growSize = 5
        };

        CreateNewPool(newPool);
    }

    public int CountActiveAndInactiveObjects(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return 0;

        // ��Ȱ��ȭ�� ������Ʈ ��(ť�� �ִ� ������Ʈ)
        int inactiveCount = poolDictionary[tag].Count;

        // Ȱ��ȭ�� ������Ʈ �� ���
        int activeCount = 0;
        foreach (var pair in objectToTagMap)
        {
            if (pair.Value == tag && pair.Key.activeSelf)
            {
                activeCount++;
            }
        }

        return activeCount + inactiveCount;
    }

    public void ReturnAllObjectsToPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return;

        // ���� ����Ʈ ���� (GC Alloc ����)
        List<GameObject> objectsToReturn = new List<GameObject>();

        // Ȱ��ȭ�� ������Ʈ ã��
        foreach (var pair in objectToTagMap)
        {
            if (pair.Value == tag && pair.Key != null && pair.Key.activeInHierarchy)
            {
                objectsToReturn.Add(pair.Key);
            }
        }

        // ��� ������Ʈ ��ȯ
        foreach (var obj in objectsToReturn)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                obj.SetActive(false);
                ReturnToPool(tag, obj);
            }
        }
    }

    // ���� API���� ȣȯ���� ���� �޼���
    public void ReturnToPool(string tag, GameObject objectToReturn, bool forceParenting = false)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);

        // forceParenting�� true�̸� �׻� Ǯ �����̳��� �ڽ����� ���� (���� ��İ� ȣȯ�� ����)
        if (forceParenting || !useOptimizedHierarchy)
        {
            // ���� Ǯ �����̳ʰ� �ִ� ��쿡�� �θ�� ����
            if (poolContainers.TryGetValue(tag, out Transform container))
            {
                objectToReturn.transform.SetParent(container);
            }
        }

        poolDictionary[tag].Enqueue(objectToReturn);
    }
    public bool DoesPoolExist(string tag)
    {
        return poolDictionary != null && poolDictionary.ContainsKey(tag);
    }
    public void ExpandPool(string tag, int additionalCount)
    {
        if (!poolDictionary.ContainsKey(tag) || !poolConfigs.ContainsKey(tag)) return;

        Pool config = poolConfigs[tag];
        Queue<GameObject> pool = poolDictionary[tag];

        for (int i = 0; i < additionalCount; i++)
        {
            GameObject obj = CreateNewPoolObject(config.prefab, tag);
            pool.Enqueue(obj);
        }

        Debug.Log($"Expanded pool {tag} to {pool.Count} objects");
    }

    public int GetAvailableCount(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            return 0;
        }

        return poolDictionary[tag].Count;
    }
    public void EnsurePoolCapacity(string tag, int requiredCount)
    {
        if (!poolDictionary.ContainsKey(tag) || !poolConfigs.ContainsKey(tag))
        {
            return;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        Pool config = poolConfigs[tag];

        // ���� ���� ������Ʈ�� ����ϸ� �ƹ��͵� ���� ����
        if (pool.Count >= requiredCount)
        {
            return;
        }

        // �ʿ��� ��ŭ�� �߰� (��Ȯ��)
        int toAdd = requiredCount - pool.Count;
        for (int i = 0; i < toAdd; i++)
        {
            GameObject obj = CreateNewPoolObject(config.prefab, tag);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 모든 풀을 정리하고 오브젝트들을 파괴합니다.
    /// </summary>
    public void ClearAllPools()
    {
        if (poolDictionary == null) return;

        foreach (var pair in poolDictionary)
        {
            string tag = pair.Key;
            Queue<GameObject> pool = pair.Value;

            // 풀 안의 모든 오브젝트 파괴
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        // 활성화된 오브젝트들도 정리 (objectToTagMap 사용)
        if (objectToTagMap != null)
        {
            var activeObjects = new List<GameObject>(objectToTagMap.Keys);
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            objectToTagMap.Clear();
        }

        // 딕셔너리들 초기화
        poolDictionary.Clear();
        poolConfigs?.Clear();
        poolContainers?.Clear();

        Debug.Log("All object pools have been cleared.");
    }
}