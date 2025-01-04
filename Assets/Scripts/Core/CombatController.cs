using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;

    [Header("Magnet Effect Setting")]
    [SerializeField] private float magnetForce = 20f;

    private List<CollectibleItem> activeCollectibles = new List<CollectibleItem>();
    private PlayerStats playerStats;
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
            playerStats.OnPlayerDeath += HandlePlayerDeath;
            InitializeItemPools();
            isInitialized = true;
        }
    }

    public void SpawnDrops(Vector3 position, EnemyDropTable dropTable)
    {
        if (!isInitialized)
        {
            Debug.LogError("CombatController is not initialized!");
            return;
        }

        if (dropTable == null)
        {
            Debug.LogError("DropTable is null!");
            return;
        }

        bool essentialDropSpawned = false;
        int maxAttempts = 3;  // 최대 시도 횟수
        int attempts = 0;

        // Essential Drop (Experience or Gold) - 성공할 때까지 시도
        while (!essentialDropSpawned && attempts < maxAttempts)
        {
            float randomValue = Random.Range(0f, 100f);
            if (randomValue <= dropTable.experienceDropRate)
            {
                GameObject expDrop = SpawnExperienceDrop(position, dropTable.experienceInfo);
                essentialDropSpawned = (expDrop != null);
            }
            else
            {
                GameObject goldDrop = SpawnGoldDrop(position, dropTable.goldInfo);
                essentialDropSpawned = (goldDrop != null);
            }
            attempts++;
        }

        if (!essentialDropSpawned)
        {
            Debug.LogError("Failed to spawn essential drop after multiple attempts!");
        }

        // Additional Drop - Enemy의 Die()에서 처리하도록 제거
    }


    private GameObject SpawnExperienceDrop(Vector3 position, ExperienceDropInfo expInfo)
    {
        if (expInfo == null)
        {
            Debug.LogError("ExperienceDropInfo is null!");
            return null;
        }

        float randomValue = Random.Range(0f, 100f);
        ItemType selectedType;

        if (randomValue <= expInfo.smallExpRate)
        {
            selectedType = ItemType.ExperienceSmall;
        }
        else if (randomValue <= expInfo.smallExpRate + expInfo.mediumExpRate)
        {
            selectedType = ItemType.ExperienceMedium;
        }
        else
        {
            selectedType = ItemType.ExperienceLarge;
        }

        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject spawnedObj = ObjectPool.Instance.SpawnFromPool(
            selectedType.ToString(),
            spawnPos,
            Quaternion.identity
        );

        if (spawnedObj == null)
        {
            Debug.LogError($"Failed to spawn experience item of type: {selectedType}");
        }

        return spawnedObj;
    }


    private GameObject SpawnGoldDrop(Vector3 position, GoldDropInfo goldInfo)
    {
        if (goldInfo == null)
        {
            Debug.LogError("GoldDropInfo is null!");
            return null;
        }

        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject goldObj = ObjectPool.Instance.SpawnFromPool(
            ItemType.Gold.ToString(),
            spawnPos,
            Quaternion.identity
        );

        if (goldObj != null)
        {
            int goldAmount = Random.Range(goldInfo.minGoldAmount, goldInfo.maxGoldAmount + 1);
            if (goldObj.TryGetComponent<CollectibleItem>(out var collectible))
            {
                collectible.SetGoldAmount(goldAmount);
            }
        }
        else
        {
            Debug.LogError("Failed to spawn gold item");
        }

        return goldObj;
    }

    public void SpawnAdditionalDrop(Vector3 position, AdditionalDrop dropInfo)
    {
        GameObject item = SpawnItem(position, dropInfo.itemType);
        if (item.TryGetComponent<CollectibleItem>(out var collectible))
        {
            collectible.Initialize(dropInfo);
        }
    }

    private GameObject SpawnItem(Vector3 position, ItemType itemType)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        return ObjectPool.Instance.SpawnFromPool(
            itemType.ToString(),
            spawnPos,
            Quaternion.identity
        );
    }

    public void ApplyItemEffect(ItemType itemType, int goldAmount = 0)
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
            case ItemType.ExperienceMedium:
                playerStats.AddExperience(7f);
                break;
            case ItemType.ExperienceLarge:
                playerStats.AddExperience(25f);
                break;
            case ItemType.Gold:
                playerStats.AddCoins(goldAmount);
                break;
            case ItemType.HealthPotion:
                playerStats.Heal(healthPotionAmount);
                break;
            case ItemType.Magnet:
                ApplyMagnetEffect();
                break;
            default:
                Debug.LogWarning($"Unknown item type: {itemType}");
                break;
        }
    }

    private void ApplyMagnetEffect()
    {
        foreach (var item in activeCollectibles.ToList())
        {
            if (item != null)
            {
                item.PullToPlayer(magnetForce);
            }
        }
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

    private void HandlePlayerDeath()
    {
        isInitialized = false;
        GameManager.Instance.SetGameState(GameState.GameOver);
    }

    private void InitializeItemPools()
    {
        var dropTables = Resources.LoadAll<EnemyDropTable>("");
        var processedPrefabs = new HashSet<GameObject>();

        foreach (var table in dropTables)
        {
            // 기본 경험치 풀 초기화
            if (table.experienceInfo != null)
            {
                if (!processedPrefabs.Contains(table.experienceInfo.smallExpPrefab))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceSmall.ToString(),
                        table.experienceInfo.smallExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.smallExpPrefab);
                }
                if (!processedPrefabs.Contains(table.experienceInfo.mediumExpPrefab))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceMedium.ToString(),
                        table.experienceInfo.mediumExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.mediumExpPrefab);
                }
                if (!processedPrefabs.Contains(table.experienceInfo.largeExpPrefab))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceLarge.ToString(),
                        table.experienceInfo.largeExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.largeExpPrefab);
                }
            }

            // 골드 풀 초기화
            if (table.goldInfo != null && !processedPrefabs.Contains(table.goldInfo.goldPrefab))
            {
                ObjectPool.Instance.CreatePool(ItemType.Gold.ToString(),
                    table.goldInfo.goldPrefab, 10);
                processedPrefabs.Add(table.goldInfo.goldPrefab);
            }

            // 추가 아이템 풀 초기화
            if (table.additionalDrops != null)
            {
                foreach (var drop in table.additionalDrops)
                {
                    if (drop.itemPrefab != null && !processedPrefabs.Contains(drop.itemPrefab))
                    {
                        ObjectPool.Instance.CreatePool(drop.itemType.ToString(), drop.itemPrefab, 10);
                        processedPrefabs.Add(drop.itemPrefab);
                    }
                }
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
}