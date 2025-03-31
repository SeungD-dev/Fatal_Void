using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;

    [Header("Magnet Effect Setting")]
    [SerializeField] private float magnetForce = 20f;
    [SerializeField] private float magnetEffectDuration = 5f;  // Duration of magnet power-up

    [Header("Death Effect Settings")]
    [SerializeField] private string deathEffectPoolTag = "DeathParticle";
    [SerializeField] private int particlesPerEffect = 5;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);
    [SerializeField]
    private Color[] deathParticleColors = new Color[]
    {
        new Color(1f, 0f, 0f),      // Red
        new Color(0f, 0f, 0f),      // Black
        new Color(65/255f, 65/255f, 65/255f)  // Gray
    };

    // Optimization settings
    private int maxConcurrentDeathEffects = 5;
    private int activeDeathEffectsCount = 0;
    private const int COLLECTIBLES_INITIAL_CAPACITY = 100;  // Reserve capacity to avoid resizing

    // Cached WaitForSeconds objects
    private static readonly WaitForSeconds particleDelay = new WaitForSeconds(0.02f);
    private static readonly WaitForSeconds magnetDuration = new WaitForSeconds(5f);

    // Using HashSet for faster lookup operations
    private HashSet<CollectibleItem> activeCollectibles;
    private PlayerStats playerStats;
    private bool isInitialized = false;
    private Coroutine magnetEffectCoroutine;

    private void Awake()
    {
        // Initialize collections with capacity to avoid resizing
        activeCollectibles = new HashSet<CollectibleItem>(COLLECTIBLES_INITIAL_CAPACITY);
    }

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

    public void PlayEnemyDeathEffect(Vector3 position, Color? customColor = null, float enemyScale = 1f)
    {
        if (!isInitialized || ObjectPool.Instance == null)
            return;

        // Limit concurrent effects for performance
        if (activeDeathEffectsCount >= maxConcurrentDeathEffects)
            return;

        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            Debug.LogWarning($"Death effect pool '{deathEffectPoolTag}' not found!");
            return;
        }

        StartCoroutine(CreateDeathEffect(position, customColor, enemyScale));
    }

    private IEnumerator CreateDeathEffect(Vector3 position, Color? customColor, float enemyScale)
    {
        activeDeathEffectsCount++;

        for (int i = 0; i < particlesPerEffect; i++)
        {
            GameObject particle = ObjectPool.Instance.SpawnFromPool(
                deathEffectPoolTag,
                position,
                Quaternion.identity
            );

            if (particle != null)
            {
                AnimateDeathParticle(particle, position, customColor, enemyScale);
            }

            yield return particleDelay;
        }

        yield return new WaitForSeconds(explosionDuration);
        activeDeathEffectsCount--;
    }

    private void AnimateDeathParticle(GameObject particle, Vector3 position, Color? customColor, float enemyScale)
    {
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        // Scale particle based on enemy size
        float baseSize = Random.Range(particleSizeRange.x, particleSizeRange.y);
        float scaledSize = baseSize * enemyScale;
        float adjustedRadius = explosionRadius * enemyScale;

        // Configure appearance
        Color color = customColor ?? deathParticleColors[Random.Range(0, deathParticleColors.Length)];
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = adjustedRadius * Random.Range(0.5f, 1f);

        // Calculate target position
        Vector3 targetPos = position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );

        // Clean up any existing tweens
        DOTween.Kill(particle.transform);
        DOTween.Kill(renderer);

        // Set initial state
        particle.transform.localScale = Vector3.zero;
        renderer.color = new Color(color.r, color.g, color.b, 1f);

        // Create animation sequence
        Sequence seq = DOTween.Sequence();

        // Scale animation
        seq.Append(particle.transform.DOScale(new Vector3(scaledSize, scaledSize, 1f), explosionDuration * 0.2f));

        // Movement animation
        seq.Join(particle.transform.DOMove(targetPos, explosionDuration)
            .SetEase(Ease.OutQuad));

        // Rotation animation
        seq.Join(particle.transform.DORotate(
            new Vector3(0, 0, Random.Range(-180f, 180f)),
            explosionDuration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad));

        // Fade out
        seq.Join(renderer.DOFade(0f, explosionDuration)
            .SetEase(Ease.InQuad));

        // Return to pool when complete
        seq.OnComplete(() => {
            ObjectPool.Instance.ReturnToPool(deathEffectPoolTag, particle);
        });

        // Make animation independent of time scale
        seq.SetUpdate(true);
    }

    public void SpawnDrops(Vector3 position, EnemyDropTable dropTable)
    {
        if (!isInitialized || dropTable == null) return;

        bool essentialDropSpawned = false;
        int maxAttempts = 3;
        int attempts = 0;

        // Try to spawn essential drop (experience or gold)
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
            Debug.LogWarning("Failed to spawn essential drop after multiple attempts!");
        }
    }

    private GameObject SpawnExperienceDrop(Vector3 position, ExperienceDropInfo expInfo)
    {
        if (expInfo == null) return null;

        float randomValue = Random.Range(0f, 100f);
        ItemType selectedType;

        // Determine experience size based on drop rates
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

        // Add random offset to prevent items from stacking
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = position + new Vector3(randomOffset.x, randomOffset.y, 0);

        // Spawn from pool
        GameObject spawnedObj = ObjectPool.Instance.SpawnFromPool(
            selectedType.ToString(),
            spawnPos,
            Quaternion.identity
        );

        return spawnedObj;
    }

    private GameObject SpawnGoldDrop(Vector3 position, GoldDropInfo goldInfo)
    {
        if (goldInfo == null) return null;

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

        return goldObj;
    }

    public void SpawnAdditionalDrop(Vector3 position, AdditionalDrop dropInfo)
    {
        GameObject item = SpawnItem(position, dropInfo.itemType);
        if (item != null && item.TryGetComponent<CollectibleItem>(out var collectible))
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
        if (GameManager.Instance?.PlayerStats == null) return;

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
                StartMagnetEffect();
                break;
            default:
                Debug.LogWarning($"Unknown item type: {itemType}");
                break;
        }
    }

    private void StartMagnetEffect()
    {
        // Apply immediate effect
        ApplyMagnetEffect();

        // Activate magnet effect on player
        if (playerStats != null)
        {
            playerStats.SetMagnetEffect(true);
        }

        // Stop any existing coroutine
        if (magnetEffectCoroutine != null)
        {
            StopCoroutine(magnetEffectCoroutine);
        }

        // Start new coroutine for timed effect
        magnetEffectCoroutine = StartCoroutine(MagnetEffectRoutine());
    }

    private IEnumerator MagnetEffectRoutine()
    {
        yield return new WaitForSeconds(magnetEffectDuration);

        // Turn off magnet effect when duration expires
        if (playerStats != null)
        {
            playerStats.SetMagnetEffect(false);
        }

        magnetEffectCoroutine = null;
    }

    private void ApplyMagnetEffect()
    {
        // Use a temporary list to prevent errors if collection changes during iteration
        if (activeCollectibles.Count > 0)
        {
            // Avoid foreach to prevent potential allocations
            CollectibleItem[] items = new CollectibleItem[activeCollectibles.Count];
            activeCollectibles.CopyTo(items);

            for (int i = 0; i < items.Length; i++)
            {
                CollectibleItem item = items[i];
                if (item != null && item.gameObject.activeInHierarchy)
                {
                    item.PullToPlayer(magnetForce);
                }
            }
        }
    }

    public void RegisterCollectible(CollectibleItem item)
    {
        if (item != null && !activeCollectibles.Contains(item))
        {
            activeCollectibles.Add(item);

            // If magnet effect is active, immediately apply to newly registered items
            if (playerStats != null && playerStats.IsMagnetActive)
            {
                item.PullToPlayer(magnetForce);
            }
        }
    }

    public void UnregisterCollectible(CollectibleItem item)
    {
        if (item != null)
        {
            activeCollectibles.Remove(item);
        }
    }

    private void HandlePlayerDeath()
    {
        if (magnetEffectCoroutine != null)
        {
            StopCoroutine(magnetEffectCoroutine);
            magnetEffectCoroutine = null;
        }

        isInitialized = false;
        GameManager.Instance.SetGameState(GameState.GameOver);
    }

    private void InitializeItemPools()
    {
        var dropTables = Resources.LoadAll<EnemyDropTable>("");
        var processedPrefabs = new HashSet<GameObject>();

        foreach (var table in dropTables)
        {
            // Initialize experience pools
            if (table.experienceInfo != null)
            {
                TryCreatePool(ItemType.ExperienceSmall.ToString(),
                    table.experienceInfo.smallExpPrefab, 20, processedPrefabs);

                TryCreatePool(ItemType.ExperienceMedium.ToString(),
                    table.experienceInfo.mediumExpPrefab, 15, processedPrefabs);

                TryCreatePool(ItemType.ExperienceLarge.ToString(),
                    table.experienceInfo.largeExpPrefab, 10, processedPrefabs);
            }

            // Initialize gold pool
            if (table.goldInfo != null)
            {
                TryCreatePool(ItemType.Gold.ToString(),
                    table.goldInfo.goldPrefab, 20, processedPrefabs);
            }

            // Initialize additional item pools
            if (table.additionalDrops != null)
            {
                foreach (var drop in table.additionalDrops)
                {
                    TryCreatePool(drop.itemType.ToString(),
                        drop.itemPrefab, 5, processedPrefabs);
                }
            }
        }

        // Initialize death effect pool
        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            GameObject particlePrefab = Resources.Load<GameObject>($"Prefabs/VFX/{deathEffectPoolTag}");
            if (particlePrefab != null)
            {
                int poolSize = maxConcurrentDeathEffects * particlesPerEffect;
                ObjectPool.Instance.CreatePool(deathEffectPoolTag, particlePrefab, poolSize);
            }
            else
            {
                Debug.LogWarning($"Death particle prefab not found: {deathEffectPoolTag}");
            }
        }
    }

    private void TryCreatePool(string poolName, GameObject prefab, int size, HashSet<GameObject> processedPrefabs)
    {
        if (prefab != null && !processedPrefabs.Contains(prefab) &&
            !ObjectPool.Instance.DoesPoolExist(poolName))
        {
            ObjectPool.Instance.CreatePool(poolName, prefab, size);
            processedPrefabs.Add(prefab);
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        // Clean up all active tweens
        DOTween.Kill(transform);

        if (magnetEffectCoroutine != null)
        {
            StopCoroutine(magnetEffectCoroutine);
        }

        // Clear collectibles
        activeCollectibles.Clear();
    }
}