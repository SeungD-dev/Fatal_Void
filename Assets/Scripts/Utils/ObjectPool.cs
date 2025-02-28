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
        [Tooltip("풀의 최대 크기 (0 = 무제한)")]
        public int maxSize;
        [Tooltip("풀이 비었을 때 한 번에 생성할 오브젝트 수")]
        public int growSize = 5;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;
    private Dictionary<GameObject, string> objectToTagMap; // 오브젝트를 태그에 매핑하는 딕셔너리

    // Scene 계층구조를 최적화하기 위한 옵션
    [Tooltip("true: 풀 오브젝트를 계층 구조에서 분리, false: 기존 방식대로 계층 구조 유지")]
    [SerializeField] private bool useOptimizedHierarchy = true;
    [Tooltip("false로 설정시 오브젝트 풀링 디버깅이 어려울 수 있지만 성능은 향상됩니다")]

    // 풀 컨테이너 캐싱 (기존 방식에서만 사용)
    private Dictionary<string, Transform> poolContainers;

    // 비활성화된 오브젝트의 부모 Transform (최적화 모드에서는 사용하지 않음)
    private Transform inactiveObjectsParent;

    private static ObjectPool instance;
    public static ObjectPool Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // DontDestroyOnLoad 추가
            InitializePools();
        }
        else if (instance != this) // 이 체크 추가
        {
            // 기존에 인스턴스가 존재하면 현재 오브젝트 제거
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

        // 기존 방식에서는 자기 자신을 부모로 설정
        inactiveObjectsParent = transform;

        foreach (Pool pool in pools)
        {
            CreateNewPool(pool);
        }
    }

    private void CreateNewPool(Pool poolConfig)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>();

        // 최적화 모드에서는 별도 컨테이너를 생성하지 않음
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

        // 최적화 모드에서는 계층 구조에서 완전히 분리
        if (useOptimizedHierarchy)
        {
            // Scene에서 루트 레벨로 설정하여 Transform 연산 최소화
            obj.transform.SetParent(null);
        }
        else
        {
            // 기존 방식대로 풀 컨테이너의 자식으로 설정
            obj.transform.SetParent(poolContainers[tag]);
        }

        objectToTagMap[obj] = tag; // 오브젝트와 태그 매핑 저장
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

        // 풀이 비었을 때 처리
        if (pool.Count == 0)
        {
            // 풀 확장
            // 최대 크기 체크
            int currentTotalSize = CountActiveAndInactiveObjects(tag);
            int growSize = config.growSize;

            if (config.maxSize > 0)
            {
                // 최대 크기 제한이 있는 경우
                growSize = Mathf.Min(growSize, config.maxSize - currentTotalSize);
                if (growSize <= 0)
                {
                    Debug.LogWarning($"Pool {tag} has reached its maximum size of {config.maxSize}");
                    return null;
                }
            }

            // 새 오브젝트들 생성
            for (int i = 0; i < growSize - 1; i++) // -1 because we'll create one more below
            {
                GameObject newObj = CreateNewPoolObject(config.prefab, tag);
                pool.Enqueue(newObj);
            }

            objectToSpawn = CreateNewPoolObject(config.prefab, tag);
        }
        else
        {
            objectToSpawn = pool.Dequeue();
            if (objectToSpawn == null) // 풀에 있는 오브젝트가 파괴된 경우
            {
                objectToSpawn = CreateNewPoolObject(config.prefab, tag);
            }
        }

        // 오브젝트 위치 및 회전 설정 (SetActive 전에 수행하여 불필요한 이벤트 호출 방지)
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
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
        // 태그 매핑을 통해 오브젝트가 속한 풀 찾기
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

        // 최적화 모드: 계층 구조에서 완전히 분리
        if (!useOptimizedHierarchy)
        {
            // 기존 방식에서만 풀 컨테이너의 자식으로 설정
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
            maxSize = 0, // 무제한
            growSize = 5
        };

        CreateNewPool(newPool);
    }

    public int CountActiveAndInactiveObjects(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return 0;

        // 비활성화된 오브젝트 수(큐에 있는 오브젝트)
        int inactiveCount = poolDictionary[tag].Count;

        // 활성화된 오브젝트 수 계산
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

        // 정적 리스트 재사용 (GC Alloc 방지)
        List<GameObject> objectsToReturn = new List<GameObject>();

        // 활성화된 오브젝트 찾기
        foreach (var pair in objectToTagMap)
        {
            if (pair.Value == tag && pair.Key != null && pair.Key.activeInHierarchy)
            {
                objectsToReturn.Add(pair.Key);
            }
        }

        // 모든 오브젝트 반환
        foreach (var obj in objectsToReturn)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                obj.SetActive(false);
                ReturnToPool(tag, obj);
            }
        }
    }

    // 기존 API와의 호환성을 위한 메서드
    public void ReturnToPool(string tag, GameObject objectToReturn, bool forceParenting = false)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);

        // forceParenting이 true이면 항상 풀 컨테이너의 자식으로 설정 (기존 방식과 호환성 유지)
        if (forceParenting || !useOptimizedHierarchy)
        {
            // 기존 풀 컨테이너가 있는 경우에만 부모로 설정
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

        // 현재 가용 오브젝트가 충분하면 아무것도 하지 않음
        if (pool.Count >= requiredCount)
        {
            return;
        }

        // 필요한 만큼만 추가 (정확히)
        int toAdd = requiredCount - pool.Count;
        for (int i = 0; i < toAdd; i++)
        {
            GameObject obj = CreateNewPoolObject(config.prefab, tag);
            pool.Enqueue(obj);
        }
    }
}