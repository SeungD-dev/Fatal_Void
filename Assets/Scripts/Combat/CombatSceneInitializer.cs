using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;
    [SerializeField] private WaveManager waveManager;

    // CombatSceneInitializer.cs
    private void Start()
    {
        // Load map and place player
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();
            if (map != null && playerStats != null)
            {
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;
            }

            // CombatSceneInitializer.cs의 Start() 메서드 끝부분에 추가
            if (map != null && playerStats != null)
            {
                Debug.Log($"Map position: {map.transform.position}");
                Debug.Log($"Map bounds: min={map.FloorTilemap.cellBounds.min}, max={map.FloorTilemap.cellBounds.max}");
                Debug.Log($"Map world bounds: min={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.min)}, " +
                          $"max={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.max)}");
                Debug.Log($"Player position: {playerStats.transform.position}");
            }
        }

        // Game manager references
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );

            // WaveManager 초기화 확인
            if (waveManager != null)
            {
                waveManager.EnsureInitialized(MapManager.Instance.CurrentMap);
            }

            // Initial pause
            GameManager.Instance.SetGameState(GameState.Paused);

            // Open shop for first time (이미 초기화된 WaveManager에게 알림)
            OpenInitialShopPhase();
        }


    }

    private void OpenInitialShopPhase()
    {
        if (shopController != null)
        {
            // Set flag to indicate this is the first shop
            shopController.isFirstShop = true;

            // Initialize and open shop
            shopController.InitializeShop();
        }
    }
}