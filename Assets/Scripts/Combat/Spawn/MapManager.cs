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
    Debug.Log("타일맵 경계에 정확히 맞추어 카메라 경계 업데이트 시도...");

    // 먼저 타일맵을 가져옵니다
    var wallTilemap = map.WallTilemap;
    if (wallTilemap == null)
    {
        Debug.LogError("Wall 타일맵을 찾을 수 없습니다!");
        return;
    }

        // 모든 CameraBound 객체 찾기 및 제거
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
    {
        if (obj.name.Contains("CameraBound"))
        {
            Debug.Log($"기존 '{obj.name}' 오브젝트를 제거합니다.");
            Object.DestroyImmediate(obj);
        }
    }

    // 새 CameraBound 객체 생성
    GameObject cameraBoundObj = new GameObject("CameraBound");
    Debug.Log("새 CameraBound 오브젝트를 생성했습니다");

    // 타일맵을 순회하여 실제 사용된 타일의 경계 계산
    BoundsInt cellBounds = wallTilemap.cellBounds;
    Vector3 cellSize = wallTilemap.layoutGrid.cellSize;
    
    // 외곽 타일의 좌표를 저장할 변수
    float minX = float.MaxValue;
    float minY = float.MaxValue;
    float maxX = float.MinValue;
    float maxY = float.MinValue;
    
    bool foundTiles = false;
    
    // 모든 타일을 순회하며 월드 좌표 계산
    for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
    {
        for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(x, y, 0);
            
            if (wallTilemap.HasTile(cellPos))
            {
                foundTiles = true;
                
                // 타일의 월드 좌표 계산 - 맵 중심 이동 고려
                Vector3 worldPos = wallTilemap.CellToWorld(cellPos);
                
                // 타일맵이 맵의 자식이라면 맵의 위치도 고려
                worldPos = wallTilemap.transform.TransformPoint(wallTilemap.CellToLocal(cellPos));
                
                // 좌표 업데이트
                minX = Mathf.Min(minX, worldPos.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }
    }
    
    if (!foundTiles)
    {
        Debug.LogError("타일맵에서 타일을 찾을 수 없습니다!");
        Object.DestroyImmediate(cameraBoundObj);
        return;
    }
    
    // 여백 추가 (필요에 따라 조정)
    float paddingX = 0.1f;
    float paddingY = 0.1f;
    minX -= paddingX;
    minY -= paddingY;
    maxX += paddingX;
    maxY += paddingY;
    
    Debug.Log($"계산된 타일 경계: min=({minX}, {minY}), max=({maxX}, {maxY})");
    
    // 월드 경계를 사각형 콜라이더로 변환
    PolygonCollider2D collider = cameraBoundObj.AddComponent<PolygonCollider2D>();
    
    Vector2[] points = new Vector2[4];
    points[0] = new Vector2(minX, minY); // 좌하단
    points[1] = new Vector2(maxX, minY); // 우하단
    points[2] = new Vector2(maxX, maxY); // 우상단
    points[3] = new Vector2(minX, maxY); // 좌상단
    
    collider.points = points;
    
    Debug.Log($"경계 포인트 설정: " +
              $"좌하단({points[0].x}, {points[0].y}), " +
              $"우하단({points[1].x}, {points[1].y}), " +
              $"우상단({points[2].x}, {points[2].y}), " +
              $"좌상단({points[3].x}, {points[3].y})");
    
    // Cinemachine Confiner2D 참조 업데이트
    var cinemachineCamera = GameObject.FindFirstObjectByType<CinemachineCamera>();
    if (cinemachineCamera != null)
    {
        // 기존 컨파이너 제거
        var existingConfiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
        if (existingConfiner != null)
        {
            Object.DestroyImmediate(existingConfiner);
        }
        
        // 새 컨파이너 추가
        var confiner = cinemachineCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = collider;
        confiner.Damping = 0.5f;
        confiner.SlowingDistance = 1.0f;
        confiner.InvalidateBoundingShapeCache();
    }
    else
    {
        Debug.LogError("CinemachineCamera를 찾을 수 없습니다!");
    }
    
    Debug.Log("카메라 경계 업데이트 완료");
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
