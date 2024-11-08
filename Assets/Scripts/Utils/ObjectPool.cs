using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private static ObjectPool instance;
    public static ObjectPool Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        // 풀에서 비활성화된 오브젝트 가져오기
        Queue<GameObject> pool = poolDictionary[tag];
        GameObject objectToSpawn;

        // 풀이 비어있으면 새로 생성
        if (pool.Count == 0)
        {
            Pool originalPool = pools.Find(p => p.tag == tag);
            objectToSpawn = Instantiate(originalPool.prefab);
        }
        else
        {
            objectToSpawn = pool.Dequeue();
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // 오브젝트가 IPooledObject를 구현했다면 OnObjectSpawn 호출
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
    }

    public void CreatePool(string tag, GameObject prefab, int size)
    {
        // 이미 존재하는 풀이면 무시
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} already exists.");
            return;
        }

        // 새로운 풀 생성
        Queue<GameObject> objectPool = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
        poolDictionary.Add(tag, objectPool);
    }
}
