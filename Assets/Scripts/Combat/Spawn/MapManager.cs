using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map/Map";

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera cinemachineCamera; // Inspector에서 할당

    private GameMap currentMap;
    private GameObject cameraBoundObj;
    public GameMap CurrentMap => currentMap;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 필요한 참조 초기화
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
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
        var wallTilemap = map.WallTilemap;
        if (wallTilemap == null)
        {
            Debug.LogError("Wall 타일맵을 찾을 수 없습니다!");
            return;
        }

        // 기존 CameraBound 제거
        if (cameraBoundObj != null)
        {
            Destroy(cameraBoundObj);
        }

        // 새 카메라 바운드 생성
        cameraBoundObj = new GameObject("CameraBound");

        // 타일맵 바운드 계산 - 더 효율적인 방법으로
        CalculateAndApplyBounds(wallTilemap, cameraBoundObj);
    }

    private void CalculateAndApplyBounds(Tilemap tilemap, GameObject boundObj)
    {
        BoundsInt cellBounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.layoutGrid.cellSize;

        // 가장자리만 체크해서 효율성 향상
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        bool foundTiles = false;

        // 타일맵 크기
        int xMin = cellBounds.xMin, xMax = cellBounds.xMax;
        int yMin = cellBounds.yMin, yMax = cellBounds.yMax;

        // 상단 가장자리만 체크
        for (int x = xMin; x < xMax; x++)
        {
            Vector3Int cellPos = new Vector3Int(x, yMax - 1, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // 하단 가장자리만 체크
        for (int x = xMin; x < xMax; x++)
        {
            Vector3Int cellPos = new Vector3Int(x, yMin, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                minY = Mathf.Min(minY, worldPos.y);
            }
        }

        // 좌측 가장자리만 체크
        for (int y = yMin; y < yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(xMin, y, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // 우측 가장자리만 체크
        for (int y = yMin; y < yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(xMax - 1, y, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // 타일을 찾지 못한 경우 맵 크기로 대체
        if (!foundTiles)
        {
            // 맵 크기 사용
            float halfWidth = currentMap.MapSize.x / 2f;
            float halfHeight = currentMap.MapSize.y / 2f;

            minX = -halfWidth;
            minY = -halfHeight;
            maxX = halfWidth;
            maxY = halfHeight;
        }

        // 여백 추가
        float padding = 0.1f;
        minX -= padding;
        minY -= padding;
        maxX += padding;
        maxY += padding;

        // 콜라이더 생성
        PolygonCollider2D collider = boundObj.AddComponent<PolygonCollider2D>();
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(minX, minY);
        points[1] = new Vector2(maxX, minY);
        points[2] = new Vector2(maxX, maxY);
        points[3] = new Vector2(minX, maxY);

        collider.points = points;

        // Cinemachine Confiner 업데이트
        UpdateCameraConfiner(collider);
    }

    // 카메라 컨파이너 업데이트 분리
    private void UpdateCameraConfiner(Collider2D boundingCollider)
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("CinemachineCamera를 찾을 수 없습니다!");
                return;
            }
        }

        var confiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            confiner = cinemachineCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        }

        confiner.BoundingShape2D = boundingCollider;
        confiner.Damping = 0.5f;
        confiner.SlowingDistance = 1.0f;
        confiner.InvalidateBoundingShapeCache();
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