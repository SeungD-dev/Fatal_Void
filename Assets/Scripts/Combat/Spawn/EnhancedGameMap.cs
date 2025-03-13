using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnhancedGameMap : MonoBehaviour
{
    [Header("Map Components")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Spawn Settings")]
    [SerializeField] private bool useFixedSpawnPoints = false;
    [SerializeField] private List<Transform> manualSpawnPoints = new List<Transform>();
    [SerializeField] private int randomSpawnPointsCount = 50;
    [SerializeField] private float edgeSpawnChance = 0.7f; // 가장자리에서 스폰할 확률
    [SerializeField] private float minDistanceFromPlayer = 8f; // 플레이어로부터 최소 거리

    // 캐싱된 스폰 위치들
    private List<Vector2> cachedFloorPositions = new List<Vector2>();
    private List<Vector2> cachedEdgePositions = new List<Vector2>();
    private BoundsInt mapBounds;
    private Vector2 mapSize;

    private Transform playerTransform;

    private void Awake()
    {
        InitializeMapBounds();
    }

    private void Start()
    {
        // 플레이어 참조 얻기
        if (GameManager.Instance != null)
        {
            playerTransform = GameManager.Instance.PlayerTransform;
        }

        // 스폰 위치 캐싱
        CacheSpawnPositions();
    }

    private void InitializeMapBounds()
    {
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
    }

    private void CalculateMapSize()
    {
        Vector3Int size = new Vector3Int(mapBounds.size.x, mapBounds.size.y, mapBounds.size.z);

        mapSize = new Vector2(
            size.x * floorTilemap.layoutGrid.cellSize.x,
            size.y * floorTilemap.layoutGrid.cellSize.y);
    }

    // 스폰에 사용할 위치 미리 계산하고 캐싱
    private void CacheSpawnPositions()
    {
        if (floorTilemap == null) return;

        cachedFloorPositions.Clear();
        cachedEdgePositions.Clear();

        // 모든 유효한 바닥 타일 위치 수집
        BoundsInt bounds = floorTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);

                if (floorTilemap.HasTile(cellPos) && !IsPositionColliding(floorTilemap.GetCellCenterWorld(cellPos)))
                {
                    Vector3 worldPos = floorTilemap.GetCellCenterWorld(cellPos);

                    // 가장자리 타일 여부 확인
                    bool isEdgeTile = IsEdgeTile(cellPos);

                    if (isEdgeTile)
                    {
                        cachedEdgePositions.Add(worldPos);
                    }
                    else
                    {
                        cachedFloorPositions.Add(worldPos);
                    }
                }
            }
        }

        // 정해진 수 이상이면 랜덤하게 추출
        ShuffleAndLimitPositions(cachedFloorPositions, randomSpawnPointsCount / 2);
        ShuffleAndLimitPositions(cachedEdgePositions, randomSpawnPointsCount / 2);

        Debug.Log($"Cached {cachedFloorPositions.Count} floor positions and {cachedEdgePositions.Count} edge positions for spawning");
    }

    // 리스트 섞고 크기 제한
    private void ShuffleAndLimitPositions(List<Vector2> positions, int limit)
    {
        // 피셔-예이츠 셔플
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        // 크기 제한
        if (positions.Count > limit)
        {
            positions.RemoveRange(limit, positions.Count - limit);
        }
    }

    // 타일이 가장자리인지 확인
    private bool IsEdgeTile(Vector3Int cellPos)
    {
        // 상하좌우 인접 타일 중 하나라도 타일이 없으면 가장자리로 판단
        Vector3Int[] neighbors = {
            new Vector3Int(cellPos.x + 1, cellPos.y, 0),
            new Vector3Int(cellPos.x - 1, cellPos.y, 0),
            new Vector3Int(cellPos.x, cellPos.y + 1, 0),
            new Vector3Int(cellPos.x, cellPos.y - 1, 0)
        };

        foreach (var neighbor in neighbors)
        {
            if (!floorTilemap.HasTile(neighbor) ||
                (wallTilemap != null && wallTilemap.HasTile(neighbor)))
            {
                return true;
            }
        }

        return false;
    }

    // 스폰 위치 얻기 - 웨이브 및 상황에 따라 다양한 방식 적용
    public Vector2 GetSpawnPosition(bool forceEdgeSpawn = false)
    {
        // 고정 스폰 포인트 사용 모드
        if (useFixedSpawnPoints && manualSpawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, manualSpawnPoints.Count);
            return manualSpawnPoints[randomIndex].position;
        }

        // 랜덤 스폰 위치 선택 (가장자리 vs 내부)
        bool useEdgeSpawn = forceEdgeSpawn || Random.value < edgeSpawnChance;

        // 후보 위치 리스트
        List<Vector2> candidatePositions = useEdgeSpawn ? cachedEdgePositions : cachedFloorPositions;

        // 위치가 없으면 다른 리스트 사용
        if (candidatePositions.Count == 0)
        {
            candidatePositions = useEdgeSpawn ? cachedFloorPositions : cachedEdgePositions;
        }

        // 그래도 없으면 랜덤 위치 반환
        if (candidatePositions.Count == 0)
        {
            return GetFallbackSpawnPosition();
        }

        // 플레이어와의 거리 고려하여 위치 선택
        if (playerTransform != null)
        {
            // 적합한 위치들 필터링
            List<Vector2> validPositions = new List<Vector2>();
            Vector2 playerPos = playerTransform.position;

            foreach (Vector2 pos in candidatePositions)
            {
                if (Vector2.Distance(pos, playerPos) >= minDistanceFromPlayer)
                {
                    validPositions.Add(pos);
                }
            }

            // 적합한 위치가 있으면 그 중에서 선택
            if (validPositions.Count > 0)
            {
                return validPositions[Random.Range(0, validPositions.Count)];
            }
        }

        // 조건을 만족하는 위치가 없으면 랜덤 선택
        return candidatePositions[Random.Range(0, candidatePositions.Count)];
    }

    // 폴백: 간단한 랜덤 위치 생성
    private Vector2 GetFallbackSpawnPosition()
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // 맵 가장자리에서 랜덤 위치
        int side = Random.Range(0, 4);
        Vector2 position;

        switch (side)
        {
            case 0: // 상단
                position = new Vector2(Random.Range(-halfWidth + 1f, halfWidth - 1f), halfHeight - 1f);
                break;
            case 1: // 우측
                position = new Vector2(halfWidth - 1f, Random.Range(-halfHeight + 1f, halfHeight - 1f));
                break;
            case 2: // 하단
                position = new Vector2(Random.Range(-halfWidth + 1f, halfWidth - 1f), -halfHeight + 1f);
                break;
            case 3: // 좌측
                position = new Vector2(-halfWidth + 1f, Random.Range(-halfHeight + 1f, halfHeight - 1f));
                break;
            default:
                position = Vector2.zero;
                break;
        }

        return position;
    }

    // 해당 위치가 벽과 충돌하는지 확인
    public bool IsPositionColliding(Vector2 worldPosition)
    {
        if (wallTilemap == null) return false;

        Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
        return wallTilemap.HasTile(cellPosition);
    }

    // 더 동적이고 흥미로운 스폰을 위한 추가 메서드

    // 플레이어 주변에서 스폰 (보스 등장 등에 사용)
    public Vector2 GetPositionAroundPlayer(float minDistance, float maxDistance)
    {
        if (playerTransform == null) return Vector2.zero;

        Vector2 playerPos = playerTransform.position;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minDistance, maxDistance);

        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );

        Vector2 position = playerPos + offset;

        // 맵 내부로 제한
        position.x = Mathf.Clamp(position.x, -mapSize.x / 2 + 1f, mapSize.x / 2 - 1f);
        position.y = Mathf.Clamp(position.y, -mapSize.y / 2 + 1f, mapSize.y / 2 - 1f);

        return position;
    }

    // 특정 방향에서 여러 적 스폰하기 (공격대 등에 사용)
    public List<Vector2> GetPositionsInDirection(Vector2 direction, int count, float spacing)
    {
        List<Vector2> positions = new List<Vector2>();
        direction = direction.normalized;

        // 맵 경계 계산
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // 맵 가장자리에서 시작점 찾기
        Vector2 startPoint;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // 좌/우 방향
            float x = direction.x > 0 ? -halfWidth + 1f : halfWidth - 1f;
            float t = (direction.x > 0 ? halfWidth * 2 : -halfWidth * 2) / direction.x;
            float y = Random.Range(-halfHeight + 1f, halfHeight - 1f);
            startPoint = new Vector2(x, y);
        }
        else
        {
            // 상/하 방향
            float y = direction.y > 0 ? -halfHeight + 1f : halfHeight - 1f;
            float t = (direction.y > 0 ? halfHeight * 2 : -halfHeight * 2) / direction.y;
            float x = Random.Range(-halfWidth + 1f, halfWidth - 1f);
            startPoint = new Vector2(x, y);
        }

        // 방향에 따라 여러 위치 계산
        for (int i = 0; i < count; i++)
        {
            Vector2 position = startPoint + direction * i * spacing;

            // 맵 내부로 제한
            if (IsPositionInMap(position))
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    // 위치가 맵 내부에 있는지 확인
    public bool IsPositionInMap(Vector2 position)
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        return position.x >= -halfWidth && position.x <= halfWidth &&
               position.y >= -halfHeight && position.y <= halfHeight;
    }
}