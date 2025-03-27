using Unity.Cinemachine;
using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;
    [SerializeField] private OptionController optionController;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerUIController playerUIController;
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
            }
        }
        
        GameObject optionPanel = null;
        if (playerUIController != null)
        {
            optionPanel = playerUIController.GetOptionPanel();
        }

        // 게임 매니저 참조 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController,
                optionPanel
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