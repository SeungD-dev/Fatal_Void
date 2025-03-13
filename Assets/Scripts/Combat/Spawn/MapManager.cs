using UnityEngine;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map";

    private GameMap currentMap;
    public GameMap CurrentMap => currentMap;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 지정된 맵 리소스 로드
    public GameMap LoadMap(string mapPath = null)
    {
        // 기존 맵 제거
        if (currentMap != null)
        {
            Destroy(currentMap.gameObject);
            currentMap = null;
        }

        GameObject mapInstance;

        // 미리 참조된 맵 프리팹 사용 (더 효율적)
        if (mapPrefabReference != null)
        {
            mapInstance = Instantiate(mapPrefabReference, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // 폴백: Resources에서 맵 프리팹 로드
            string path = string.IsNullOrEmpty(mapPath) ? mapResourcePath : mapPath;
            GameObject mapPrefab = Resources.Load<GameObject>(path);

            if (mapPrefab == null)
            {
                Debug.LogError($"Failed to load map from path: {path}");
                return null;
            }

            mapInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
        }

        // GameMap 컴포넌트 가져오기
        currentMap = mapInstance.GetComponent<GameMap>();

        if (currentMap == null)
        {
            Debug.LogError("Loaded map prefab does not have a GameMap component!");
            Destroy(mapInstance);
            return null;
        }

        Debug.Log($"Map loaded: {currentMap.MapName}");
        return currentMap;
    }

    // 플레이어 시작 위치 가져오기
    public Vector2 GetPlayerStartPosition()
    {
        if (currentMap == null)
        {
            Debug.LogWarning("No map loaded. Using default position.");
            return Vector2.zero;
        }

        // 맵 중앙에 플레이어 배치
        return Vector2.zero;
    }
}
