using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;

    [Header("Magnet Effect Setting")]
    [SerializeField] private float magnetForce = 20f;

    private List<CollectibleItem> activeCollectibles = new List<CollectibleItem>();

    private PlayerStats playerStats;
    private Dictionary<ItemType, Queue<GameObject>> itemPools;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    private IEnumerator WaitForInitialization()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (!GameManager.Instance.IsInitialized && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("Scene initialization timed out!");
            yield break;
        }

        InitializeCombatSystem();
    }

    private void InitializeCombatSystem()
    {
        playerStats = GameManager.Instance.PlayerStats;

        if (playerStats != null)
        {
            // 전투 관련 이벤트만 구독
            playerStats.OnPlayerDeath += HandlePlayerDeath;
            InitializeItemPools();
            isInitialized = true;
        }
    }

    public void SpawnDrops(Vector3 position, DropTable dropTable)
    {
        if (!isInitialized || dropTable == null) return;

        float randomValue = Random.Range(0f, 100f);
        float currentRate = 0f;

        foreach (var drop in dropTable.possibleDrops)
        {
            currentRate += drop.dropRate;
            if (randomValue <= currentRate)
            {
                SpawnDropItem(position, drop);
                break;
            }
        }
    }

    private void SpawnDropItem(Vector3 position, DropInfo dropInfo)
    {
        int amount = Random.Range(dropInfo.minAmount, dropInfo.maxAmount + 1);

        for (int i = 0; i < amount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

            GameObject item = ObjectPool.Instance.SpawnFromPool(
                dropInfo.itemType.ToString(),
                spawnPos,
                Quaternion.identity
            );

            if (item.TryGetComponent<CollectibleItem>(out var collectible))
            {
                collectible.Initialize(dropInfo);
            }
        }
    }
    public void ApplyItemEffect(ItemType itemType)
    {
        if (GameManager.Instance?.PlayerStats == null)
        {
            Debug.LogError("PlayerStats is null when trying to apply item effect!");
            return;
        }

        var playerStats = GameManager.Instance.PlayerStats;

        switch (itemType)
        {
            case ItemType.ExperienceSmall:
                playerStats.AddExperience(1f);
                break;

            case ItemType.ExperienceLarge:
                playerStats.AddExperience(10f);
                break;

            case ItemType.HealthPotion:
                playerStats.Heal(healthPotionAmount);
                break;

            case ItemType.Coin:
                playerStats.AddCoins(1);
                break;
            case ItemType.Magnet:
                ApplyMagnetEffect();
                break;

            default:
                Debug.LogWarning($"Unknown item type: {itemType}");
                break;
        }
    }

    private void HandlePlayerDeath()
    {
        // 전투 시스템 정리
        isInitialized = false;
        // 게임오버 처리
        GameManager.Instance.SetGameState(GameState.GameOver);
    }

    public void RegisterCollectible(CollectibleItem item)
    {
        if (!activeCollectibles.Contains(item))
        {
            activeCollectibles.Add(item);
        }
    }

    public void UnregisterCollectible(CollectibleItem item)
    {
        activeCollectibles.Remove(item);
    }

    private void ApplyMagnetEffect()
    {
        foreach (var item in activeCollectibles.ToList())  // ToList()로 복사본 생성하여 순회
        {
            if (item != null)
            {
                item.PullToPlayer(magnetForce);
            }
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void InitializeItemPools()
    {
        var dropTables = Resources.LoadAll<DropTable>("");
        foreach (var table in dropTables)
        {
            foreach (var drop in table.possibleDrops)
            {
                if (drop.itemPrefab != null)
                {
                    ObjectPool.Instance.CreatePool(
                        drop.itemType.ToString(),
                        drop.itemPrefab,
                        10
                    );
                }
            }
        }
    }
}