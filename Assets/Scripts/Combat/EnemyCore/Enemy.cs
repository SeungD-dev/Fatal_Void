using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IPooledObject
{
    #region Serialized Fields
    [Header("Hit Effect")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    [Header("Bounce Effect")]
    [SerializeField] private float bounceSpeed;
    [SerializeField] private float bounceAmount;

    [Header("Knockback Properties")]
    [SerializeField] private float knockbackRecoveryTime = 0.1f;
    [SerializeField] private EnemyData enemyData;
    #endregion

    #region Private Fields
    private Transform cachedTransform;
    private Vector3 originalScale;
    private float bounceTime;
    private bool isXBounce;

    private bool isKnockedBack;
    private Coroutine knockbackCoroutine;
    private bool isKnockbackImmune = false;

    private float currentHealth;
    private float calculatedMaxHealth;
    private float lastDamageTime;
    private const float damageDelay = 1f;
    private Transform targetTransform;
    private bool isFlashing;
    
    // 이동속도 디버프 관련
    private bool isSlowed = false;
    private float speedDebuffAmount = 0f;

    // 캐시된 컴포넌트
    private EnemyCullingManager cullingManager;
    private EnemyAI enemyAI;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D rb;

    // 재사용 가능한 벡터
    private readonly Vector2 tempVector = Vector2.zero;

    // 캐시된 WaitForSeconds
    private static readonly WaitForSeconds HitFlashWait;
    private static readonly WaitForSeconds KnockbackWait;
    #endregion

    #region Properties
    public bool IsKnockBack => isKnockedBack;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => calculatedMaxHealth;
    public float Damage => enemyData?.enemyType == EnemyType.Boss ? 
        (enemyData?.bossDamage ?? 0f) : 
        (enemyData?.baseDamage ?? 0f);
    public float MoveSpeed => isSlowed ? 
        (enemyData?.moveSpeed ?? 0f) * (1f - speedDebuffAmount) : 
        enemyData?.moveSpeed ?? 0f;
    public bool IsKnockbackImmune => isKnockbackImmune;
    public string EnemyName => enemyData?.enemyName ?? "Unknown Enemy";
    public EnemyData EnemyData => enemyData;
    #endregion

    static Enemy()
    {
        HitFlashWait = new WaitForSeconds(0.1f);
        KnockbackWait = new WaitForSeconds(0.1f);
    }

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        cachedTransform = transform;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        originalScale = cachedTransform.localScale;
    }

    private void OnEnable()
    {
        if(cullingManager != null)
        {
            cullingManager.RegisterEnemy(this);
        }
    }
    public void SetCullingManager(EnemyCullingManager manager)
    {
        cullingManager = manager;
        if (gameObject.activeInHierarchy)
        {
            cullingManager.RegisterEnemy(this);
        }
    }

    public void SetCullingState(bool active)
    {
        // AI 비활성화
        if (enemyAI != null)
        {
            enemyAI.enabled = active;
        }

        // 물리 시뮬레이션 비활성화
        if (rb != null)
        {
            rb.simulated = active;
        }

        // 렌더링 비활성화
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }
    }
    public void Initialize(Transform target)
    {
        if (target == null) return;

        targetTransform = target;
        if (enemyAI != null)
        {
            enemyAI.Initialize(target);
        }
    }
    public void UpdateBounceEffect()
    {
        if (!gameObject.activeSelf) return;

        bounceTime += Time.deltaTime * bounceSpeed;
        float bounce = Mathf.Abs(Mathf.Sin(bounceTime)) * bounceAmount;

        if (bounceTime >= Mathf.PI)
        {
            bounceTime = 0f;
            isXBounce = !isXBounce;
        }

        var newScale = originalScale;
        if (isXBounce)
        {
            newScale.x += bounce;
        }
        else
        {
            newScale.y += bounce;
        }

        cachedTransform.localScale = newScale;
    }

    public void ResetBounceEffect()
    {
        // ObjectPool 생성 시 OnDisable이 Awake보다 먼저 호출될 수 있음
        if (cachedTransform != null)
        {
            cachedTransform.localScale = originalScale;
        }
        bounceTime = 0f;
        isXBounce = false;
    }

    public void SetEnemyData(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError("Attempted to set null EnemyData!");
            return;
        }

        enemyData = data;

        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }

        InitializeStats();
    }

    public void OnObjectSpawn()
    {
        if (enemyData != null)
        {
            InitializeStats();
        }
        else
        {
            Debug.LogWarning("Enemy spawned without EnemyData!");
        }

        // 플레이어 참조 설정은 WaveManager에서 처리됨

        // 넉백 상태 확실히 리셋
        isKnockedBack = false;
        knockbackCoroutine = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
        ResetBounceEffect();
    }

    private void InitializeStats()
    {
        if (enemyData == null) return;

        if (enemyData.enemyType == EnemyType.Boss)
        {
            // 보스는 고정 스탯 사용
            calculatedMaxHealth = enemyData.bossHealth;
            currentHealth = calculatedMaxHealth;
        }
        else
        {
            // 일반 적은 레벨 기반 스탯 계산
            int playerLevel = GameManager.Instance.PlayerStats.Level;
            calculatedMaxHealth = Mathf.Min(
                enemyData.baseHealth * playerLevel,
                enemyData.maxPossibleHealth
            );
            currentHealth = calculatedMaxHealth;
        }
        
        lastDamageTime = 0f;
    }

    public void TakeDamage(float damage)
    {
        if (!gameObject.activeSelf) return;

        currentHealth -= damage;

        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.PlaySound("EnemyHit_sfx", 1f, false);
        }

        var floatingTextManager = FloatingTextManager.Instance;
        if (floatingTextManager != null && floatingTextManager.isFloatingTextEnabled && cachedTransform != null)
        {
            floatingTextManager.ShowFloatingText(
                damage.ToString("F0"),
                cachedTransform.position,
                Color.white
            );
        }

        PlayHitEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect()
    {
        if (spriteRenderer != null && !isFlashing)
        {
            StartCoroutine(HitFlashCoroutine());
        }
    }

    private IEnumerator HitFlashCoroutine()
    {
        isFlashing = true;
        spriteRenderer.color = hitColor;
        yield return HitFlashWait;
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }

    public void ApplyKnockback(Vector2 force)
    {
        // 넉백 면역 상태면 넉백을 적용하지 않음
        if (isKnockbackImmune) return;

        if (!gameObject.activeInHierarchy || currentHealth <= 0 || rb == null)
            return;

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }

        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);

        if (gameObject.activeInHierarchy)
        {
            knockbackCoroutine = StartCoroutine(KnockbackRecovery());
        }
    }

    private IEnumerator KnockbackRecovery()
    {
        yield return KnockbackWait;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isKnockedBack = false;
        knockbackCoroutine = null;
    }
    public void SetKnockbackImmunity(bool immune)
    {
        isKnockbackImmune = immune;
    }

    /// <summary>
    /// 이동속도 디버프를 적용합니다 (중첩되지 않음)
    /// </summary>
    /// <param name="debuffAmount">감소율 (0.0~1.0, 예: 0.3f = 30% 감소)</param>
    public void ApplySpeedDebuff(float debuffAmount)
    {
        if (debuffAmount < 0f || debuffAmount > 1f)
        {
            Debug.LogWarning($"Invalid speed debuff amount: {debuffAmount}. Should be between 0.0 and 1.0");
            return;
        }

        isSlowed = true;
        speedDebuffAmount = debuffAmount;
    }

    /// <summary>
    /// 이동속도 디버프를 제거합니다
    /// </summary>
    public void RemoveSpeedDebuff()
    {
        isSlowed = false;
        speedDebuffAmount = 0f;
    }

    /// <summary>
    /// 현재 이동속도 디버프 상태를 확인합니다
    /// </summary>
    public bool IsSlowed => isSlowed;
    
    /// <summary>
    /// 플레이어와의 거리를 계산합니다. 보스의 경우 바운딩 박스를 고려합니다.
    /// </summary>
    /// <param name="playerPosition">플레이어 위치</param>
    /// <returns>실제 거리의 제곱값</returns>
    public float GetSquaredDistanceToPlayer(Vector2 playerPosition)
    {
        Vector2 enemyPosition = cachedTransform.position;
        
        // 보스인 경우 바운딩 박스를 고려한 거리 계산
        if (enemyData != null && enemyData.enemyType == EnemyType.Boss)
        {
            // SpriteRenderer의 bounds를 사용하여 가장 가까운 점 계산
            if (spriteRenderer != null)
            {
                Bounds bounds = spriteRenderer.bounds;
                Vector2 closestPoint = bounds.ClosestPoint(playerPosition);
                return (closestPoint - playerPosition).sqrMagnitude;
            }
        }
        
        // 일반 적은 기존 방식 사용 (pivot 기준)
        return (enemyPosition - playerPosition).sqrMagnitude;
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!gameObject.activeSelf) return;

        if (collision.CompareTag("Player") && Time.time >= lastDamageTime + damageDelay)
        {
            if (collision.TryGetComponent(out PlayerStats playerStats))
            {
                playerStats.TakeDamage(enemyData.baseDamage);
                lastDamageTime = Time.time;
            }
        }
    }

    private void Die()
    {
        var combatController = GameManager.Instance?.CombatController;
        if (combatController == null) return;

        var position = cachedTransform.position;

        float enemyScale = cachedTransform.localScale.x;

        // 사망 이펙트 재생 - 직접 CombatController 참조
        combatController.PlayEnemyDeathEffect(position,null,enemyScale);

        // 드롭 생성 (기존 코드)
        var dropTable = enemyData?.dropTable;
        if (dropTable != null)
        {
            combatController.SpawnDrops(position, dropTable);

            if (ShouldSpawnAdditionalDrop())
            {
                HandleAdditionalDrops(position, dropTable);
            }
        }

        GameManager.Instance.PlayerStats?.AddKill();
        ReturnToPool();
    }

    private bool ShouldSpawnAdditionalDrop()
    {
        return enemyData.additionalDropRate > 0 &&
               enemyData.dropTable?.additionalDrops != null &&
               enemyData.dropTable.additionalDrops.Length > 0 &&
               Random.value <= enemyData.additionalDropRate / 100f;
    }

    private void HandleAdditionalDrops(Vector3 position, EnemyDropTable dropTable)
    {
        float totalWeight = 0f;
        var additionalDrops = dropTable.additionalDrops;

        for (int i = 0; i < additionalDrops.Length; i++)
        {
            var drop = additionalDrops[i];
            if (IsValidDropType(drop.itemType))
            {
                totalWeight += drop.dropRate;
            }
        }

        if (totalWeight <= 0) return;

        float randomSelection = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        for (int i = 0; i < additionalDrops.Length; i++)
        {
            var drop = additionalDrops[i];
            if (!IsValidDropType(drop.itemType)) continue;

            currentSum += drop.dropRate;
            if (randomSelection <= currentSum)
            {
                GameManager.Instance.CombatController.SpawnAdditionalDrop(
                    position,
                    drop
                );
                break;
            }
        }
    }

    private bool IsValidDropType(ItemType itemType)
    {
        return itemType == ItemType.HealthPotion || itemType == ItemType.Magnet;
    }

    public void ReturnToPool()
    {
        if (enemyData == null)
        {
            Debug.LogError("Trying to return enemy to pool but enemyData is null!");
            return;
        }

        currentHealth = 0;
        lastDamageTime = 0;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        targetTransform = null;
        ObjectPool.Instance.ReturnToPool(enemyData.enemyName, gameObject);
    }

    private void OnDisable()
    {
        if (cullingManager != null)
        {
            cullingManager.UnregisterEnemy(this);
        }

        StopAllCoroutines();

        // 넉백 상태 리셋
        isKnockedBack = false;
        knockbackCoroutine = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
        ResetBounceEffect();
    }
}