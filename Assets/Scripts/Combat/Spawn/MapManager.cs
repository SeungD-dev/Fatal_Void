using Unity.Cinemachine;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map/Map";

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

        // 맵을 원점에 중심 배치
        CenterMapToOrigin(currentMap);

        // 카메라 바운드 업데이트
        UpdateCameraBounds(currentMap);

        return currentMap;
    }
    private void CenterMapToOrigin(GameMap map)
    {
        // 타일맵 바운드 및 중심 계산
        Vector2 mapCenter = CalculateMapCenter(map);

        // 맵 위치 조정
        Vector3 offset = new Vector3(-mapCenter.x, -mapCenter.y, 0);
        map.transform.position = offset;

        Debug.Log($"Map centered at origin. Applied offset: {offset}");
    }

    private Vector2 CalculateMapCenter(GameMap map)
    {
        // 타일맵 바운드 중심 계산
        var floorTilemap = map.FloorTilemap;
        var wallTilemap = map.WallTilemap;

        // 사용 가능한 타일맵 가져오기
        var tilemap = floorTilemap != null ? floorTilemap : wallTilemap;
        if (tilemap == null) return Vector2.zero;

        // 바운드 계산
        var bounds = tilemap.cellBounds;

        // 월드 좌표로 변환
        Vector3 worldMin = tilemap.CellToWorld(bounds.min);
        Vector3 worldMax = tilemap.CellToWorld(bounds.max);

        // 셀 크기를 고려한 조정
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        worldMax += new Vector3(cellSize.x, cellSize.y, 0);

        // 맵 중심 계산
        return new Vector2(
            (worldMin.x + worldMax.x) * 0.5f,
            (worldMin.y + worldMax.y) * 0.5f
        );
    }

    private void UpdateCameraBounds(GameMap map)
    {
        // Cinemachine Confiner2D 찾기
        var confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        if (confiner == null) return;

        // PolygonCollider2D 직접 찾기
        var boundingShape = confiner.GetComponent<PolygonCollider2D>();
        if (boundingShape == null)
        {
            Debug.LogWarning("Cinemachine Confiner doesn't have a PolygonCollider2D component");
            return;
        }

        // 맵 크기 계산
        float width = map.MapSize.x;
        float height = map.MapSize.y;

        // 바운딩 폴리곤 설정
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-width / 2, -height / 2);
        points[1] = new Vector2(width / 2, -height / 2);
        points[2] = new Vector2(width / 2, height / 2);
        points[3] = new Vector2(-width / 2, height / 2);

        boundingShape.points = points;

        // 캐시 갱신 
        confiner.InvalidateBoundingShapeCache();

        Debug.Log($"Updated camera bounds to match map size: {map.MapSize}");
    }
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
