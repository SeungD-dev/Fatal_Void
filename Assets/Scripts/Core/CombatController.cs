using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class CombatController : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private float healthPotionAmount = 20f;

    [Header("Magnet Effect Setting")]
    [SerializeField] private float magnetForce = 20f;

    [Header("Death Effect Settings")]
    [SerializeField] private string deathEffectPoolTag = "DeathParticle";
    [SerializeField] private int particlesPerEffect = 5;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);
    [SerializeField]
    private Color[] deathParticleColors = new Color[]
    {
        new Color(1f, 0f, 0f),      // 빨강
        new Color(0f, 0f, 0f),      // 검정
        new Color(65/255f, 65/255f, 65/255f)  // 회색
    };

    // 파티클 최적화를 위한 설정
    private int maxConcurrentDeathEffects = 5;
    private int activeDeathEffectsCount = 0;

    // 캐싱
    private static readonly WaitForSeconds particleDelay = new WaitForSeconds(0.02f);

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

    public void PlayEnemyDeathEffect(Vector3 position, Color? customColor = null,float enemyScale = 1f)
    {
        if (!isInitialized || ObjectPool.Instance == null)
            return;

        // 최대 동시 이펙트 수 제한
        if (activeDeathEffectsCount >= maxConcurrentDeathEffects)
            return;

        // 이펙트 풀 확인
        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            Debug.LogWarning($"Death effect pool '{deathEffectPoolTag}' not found!");
            return;
        }

        StartCoroutine(CreateDeathEffect(position, customColor,enemyScale));
    }

    private IEnumerator CreateDeathEffect(Vector3 position, Color? customColor,float enemyScale)
    {
        activeDeathEffectsCount++;

        for (int i = 0; i < particlesPerEffect; i++)
        {
            // 오브젝트 풀에서 파티클 가져오기
            GameObject particle = ObjectPool.Instance.SpawnFromPool(
                deathEffectPoolTag,
                position,
                Quaternion.identity
            );

            if (particle != null)
            {
                // 파티클 설정 및 애니메이션
                AnimateDeathParticle(particle, position, customColor,enemyScale);
            }

            // 약간의 시간차를 두고 파티클 생성
            yield return particleDelay;
        }

        // 이펙트가 끝날 때까지 대기
        yield return new WaitForSeconds(explosionDuration);

        activeDeathEffectsCount--;
    }

    private void AnimateDeathParticle(GameObject particle, Vector3 position, Color? customColor,float enemyScale)
    {
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        // 적 크기에 맞게 파티클 크기 조정
        float baseSize = Random.Range(particleSizeRange.x, particleSizeRange.y);
        float scaledSize = baseSize * enemyScale; // 적 크기에 비례하여 조정

        // 폭발 범위도 적 크기에 비례하여 조정
        float adjustedRadius = explosionRadius * enemyScale;

        // 나머지 설정
        Color color = customColor ?? deathParticleColors[Random.Range(0, deathParticleColors.Length)];
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = adjustedRadius * Random.Range(0.5f, 1f);

        // 목표 위치 계산
        Vector3 targetPos = position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );

        // 기존 트윈 정리
        DOTween.Kill(particle.transform);
        DOTween.Kill(renderer);

        // 초기 설정
        particle.transform.localScale = Vector3.zero;
        renderer.color = new Color(color.r, color.g, color.b, 1f);

        // 애니메이션 시퀀스 생성
        Sequence seq = DOTween.Sequence();

        // 크기 애니메이션
        seq.Append(particle.transform.DOScale(new Vector3(scaledSize, scaledSize, 1f), explosionDuration * 0.2f));

        // 이동 애니메이션
        seq.Join(particle.transform.DOMove(targetPos, explosionDuration)
            .SetEase(Ease.OutQuad));

        // 회전 애니메이션
        seq.Join(particle.transform.DORotate(
            new Vector3(0, 0, Random.Range(-180f, 180f)),
            explosionDuration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad));

        // 페이드 아웃
        seq.Join(renderer.DOFade(0f, explosionDuration)
            .SetEase(Ease.InQuad));

        // 완료 후 풀로 반환
        seq.OnComplete(() => {
            ObjectPool.Instance.ReturnToPool(deathEffectPoolTag, particle);
        });

        // 타임스케일에 영향받지 않도록 설정
        seq.SetUpdate(true);
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
                if (!processedPrefabs.Contains(table.experienceInfo.smallExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceSmall.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceSmall.ToString(),
                        table.experienceInfo.smallExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.smallExpPrefab);
                }

                if (!processedPrefabs.Contains(table.experienceInfo.mediumExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceMedium.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceMedium.ToString(),
                        table.experienceInfo.mediumExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.mediumExpPrefab);
                }

                if (!processedPrefabs.Contains(table.experienceInfo.largeExpPrefab) &&
                    !ObjectPool.Instance.DoesPoolExist(ItemType.ExperienceLarge.ToString()))
                {
                    ObjectPool.Instance.CreatePool(ItemType.ExperienceLarge.ToString(),
                        table.experienceInfo.largeExpPrefab, 10);
                    processedPrefabs.Add(table.experienceInfo.largeExpPrefab);
                }
            }

            // 골드 풀 초기화
            if (table.goldInfo != null && !processedPrefabs.Contains(table.goldInfo.goldPrefab) &&
                !ObjectPool.Instance.DoesPoolExist(ItemType.Gold.ToString()))
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
                    if (drop.itemPrefab != null && !processedPrefabs.Contains(drop.itemPrefab) &&
                        !ObjectPool.Instance.DoesPoolExist(drop.itemType.ToString()))
                    {
                        ObjectPool.Instance.CreatePool(drop.itemType.ToString(), drop.itemPrefab, 10);
                        processedPrefabs.Add(drop.itemPrefab);
                    }
                }
            }
        }

        // 사망 이펙트 풀 초기화 (기존 풀이 없는 경우)
        if (!ObjectPool.Instance.DoesPoolExist(deathEffectPoolTag))
        {
            GameObject particlePrefab = Resources.Load<GameObject>($"Prefabs/VFX/{deathEffectPoolTag}");
            if (particlePrefab != null)
            {
                // 적당한 풀 크기 계산: 동시 이펙트 수 * 파티클 수
                int poolSize = maxConcurrentDeathEffects * particlesPerEffect;
                ObjectPool.Instance.CreatePool(deathEffectPoolTag, particlePrefab, poolSize);
                Debug.Log($"Death effect pool initialized with {poolSize} particles");
            }
            else
            {
                Debug.LogWarning($"Death particle prefab not found: {deathEffectPoolTag}");
            }
        }
    }
    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        DOTween.Kill(transform);
    }
}