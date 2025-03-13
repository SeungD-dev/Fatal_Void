using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;

    private void Start()
    {
        // 맵 로드
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();

            // 맵 로드 후 플레이어 위치 설정
            if (map != null && playerStats != null)
            {
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;
            }
        }

        // 게임 매니저에 컴포넌트 참조 전달
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );
        }
    }
}