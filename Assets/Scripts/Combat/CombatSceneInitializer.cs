using Unity.Cinemachine;
using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;
    [SerializeField] private WaveManager waveManager;

    [Header("Camera Settings")]
    [SerializeField] private GameObject cameraBoundObject; // CameraBound 오브젝트 참조

    private void Start()
    {
        // 맵 로드 및 플레이어 위치 설정
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();
            if (map != null && playerStats != null)
            {
                // 플레이어를 원점에 배치
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;

                // 카메라 바운드 업데이트
                UpdateCameraBounds(map);

                // 디버그 정보 출력
                Debug.Log($"Map position: {map.transform.position}");
                Debug.Log($"Map bounds: min={map.FloorTilemap.cellBounds.min}, max={map.FloorTilemap.cellBounds.max}");
                Debug.Log($"Map world bounds: min={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.min)}, " +
                        $"max={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.max)}");
                Debug.Log($"Player position: {playerStats.transform.position}");
            }
        }

        // 게임 매니저 참조 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );

            // WaveManager 초기화
            if (waveManager != null)
            {
                waveManager.EnsureInitialized(MapManager.Instance.CurrentMap);
            }

            // 초기 일시정지 상태 설정
            GameManager.Instance.SetGameState(GameState.Paused);

            // 첫 상점 열기
            OpenInitialShopPhase();
        }
    }

    private void UpdateCameraBounds(GameMap map)
    {
        if (cameraBoundObject == null)
        {
            Debug.LogWarning("Camera bound reference not set in CombatSceneInitializer");
            return;
        }

        // 폴리곤 콜라이더 가져오기
        var polygonCollider = cameraBoundObject.GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
        {
            Debug.LogWarning("PolygonCollider2D not found on CameraBound object. Adding new one.");
            polygonCollider = cameraBoundObject.AddComponent<PolygonCollider2D>();
        }

        // 맵 크기 가져오기
        Vector2 mapSize = map.MapSize;

        // 맵 크기에 맞게 카메라 바운드 설정
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // 살짝 여유를 두면 화면 가장자리에서 경계가 보이지 않음
        // float padding = 0.5f; // 필요하면 패딩 추가

        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-halfWidth, -halfHeight);
        points[1] = new Vector2(halfWidth, -halfHeight);
        points[2] = new Vector2(halfWidth, halfHeight);
        points[3] = new Vector2(-halfWidth, halfHeight);

        polygonCollider.points = points;

       
        var confiner = cameraBoundObject.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();
        }

        Debug.Log($"Updated camera bounds to match map size: {mapSize}");
    }

    private void OpenInitialShopPhase()
    {
        if (shopController != null)
        {
            // 첫 상점 표시
            shopController.isFirstShop = true;
            shopController.InitializeShop();
        }
    }
}