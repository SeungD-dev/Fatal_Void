using UnityEngine;

public class CombatSceneManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;

    private void Start()
    {
        // GameManager에 참조 전달
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(playerStats, shopController, combatController,gameOverController);
        }
    }
}
