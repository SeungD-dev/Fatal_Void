using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GameMap : MonoBehaviour
{
    [Header("Map Components")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Map Properties")]
    [SerializeField] private string mapName = "Default Map";

    [Header("Spawn Settings")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnEdgeOffset = 1f;

    private BoundsInt mapBounds;
    private Vector2 mapSize;

    public string MapName => mapName;
    public Vector2 MapSize => mapSize;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;

    private Dictionary<Vector2Int, bool> collisionCache = new Dictionary<Vector2Int, bool>();
    private bool useCachedCollisions = true;

    private void Awake()
    {
        InitializeMapBounds();

        // 자주 확인하는 충돌 위치 미리 캐싱
        if (wallTilemap != null && useCachedCollisions)
        {
            PrecomputeCollisions();
        }
    }
    private void PrecomputeCollisions()
    {
        BoundsInt bounds = wallTilemap.cellBounds;
        collisionCache = new Dictionary<Vector2Int, bool>(bounds.size.x * bounds.size.y);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                Vector2Int key = new Vector2Int(x, y);
                collisionCache[key] = wallTilemap.HasTile(cellPos);
            }
        }
    }


    private void InitializeMapBounds()
    {
        // 바닥 타일맵에서 맵 경계 계산
        if (floorTilemap != null)
        {
            mapBounds = floorTilemap.cellBounds;
            CalculateMapSize();
        }
        else if (wallTilemap != null)
        {
            mapBounds = wallTilemap.cellBounds;
            CalculateMapSize();
        }
        else
        {
            Debug.LogError("No tilemaps found in GameMap!");
        }
    }

    private void CalculateMapSize()
    {
        // 타일맵 크기를 월드 단위로 변환
        Vector3Int size = new Vector3Int(
            mapBounds.size.x,
            mapBounds.size.y,
            mapBounds.size.z);

        // 타일 크기를 고려하여 맵 크기 계산
        mapSize = new Vector2(
            size.x * floorTilemap.layoutGrid.cellSize.x,
            size.y * floorTilemap.layoutGrid.cellSize.y);
    }

    private void OnEnable()
    {
        // 자식 오브젝트에서 스폰 포인트 수집
        CollectSpawnPoints();
    }

    private void CollectSpawnPoints()
    {
        // 이미 스폰 포인트가 설정되어 있다면 사용
        if (spawnPoints != null && spawnPoints.Count > 0)
            return;

        // 자식 중에서 SpawnPoint 태그를 가진 오브젝트 찾기
        spawnPoints = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("SpawnPoint"))
            {
                spawnPoints.Add(child);
            }
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found in map. Will use generated positions.");
        }
    }

    // 스폰 포인트 가져오기
    public Vector2 GetSpawnPosition(int index = -1)
    {
        // 지정된 인덱스의 스폰 포인트 반환
        if (index >= 0 && index < spawnPoints.Count)
        {
            return spawnPoints[index].position;
        }

        // 랜덤 스폰 포인트 선택
        if (spawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex].position;
        }

        // 스폰 포인트가 없으면 맵 가장자리에서 랜덤 위치 생성
        return GetRandomEdgePosition();
    }

    // 맵 가장자리에서 랜덤 위치 반환
    public Vector2 GetRandomEdgePosition()
    {
        // 타일맵 기반 맵 경계 계산
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        int side = Random.Range(0, 4);
        Vector2 position;

        switch (side)
        {
            case 0: // 상단
                position = new Vector2(
                    Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                    halfHeight - spawnEdgeOffset);
                break;
            case 1: // 우측
                position = new Vector2(
                    halfWidth - spawnEdgeOffset,
                    Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset));
                break;
            case 2: // 하단
                position = new Vector2(
                    Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                    -halfHeight + spawnEdgeOffset);
                break;
            case 3: // 좌측
                position = new Vector2(
                    -halfWidth + spawnEdgeOffset,
                    Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset));
                break;
            default:
                position = Vector2.zero;
                break;
        }

        return position;
    }

    // 맵 내부에 랜덤 위치 생성
    public Vector2 GetRandomPositionInMap()
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // 맵 내부에서 랜덤 위치 선택
        Vector2 randomPosition;
        int maxAttempts = 10;

        do
        {
            randomPosition = new Vector2(
                Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset)
            );

            maxAttempts--;
        }
        while (IsPositionColliding(randomPosition) && maxAttempts > 0);

        return randomPosition;
    }

    // 해당 위치가 벽과 충돌하는지 확인
    public bool IsPositionColliding(Vector2 worldPosition)
    {
        if (wallTilemap == null) return false;

        Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
        Vector2Int key = new Vector2Int(cellPosition.x, cellPosition.y);

        if (useCachedCollisions && collisionCache.TryGetValue(key, out bool hasCollision))
        {
            return hasCollision;
        }

        return wallTilemap.HasTile(cellPosition);
    }
    // 위치가 맵 내부에 있는지 확인
    public bool IsPositionInMap(Vector2 position)
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        return position.x >= -halfWidth && position.x <= halfWidth &&
               position.y >= -halfHeight && position.y <= halfHeight;
    }

   #if UNITY_EDITOR
private void OnDrawGizmos()
{
    // 맵 경계 그리기
    if (Application.isPlaying && mapSize != Vector2.zero)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0f));
    }
    
    // 스폰 포인트 그리기
    Gizmos.color = Color.red;
    if (spawnPoints != null)
    {
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.5f);
            }
        }
    }
}
#endif
}