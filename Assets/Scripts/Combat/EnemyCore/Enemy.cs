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
    public float Damage => enemyData?.baseDamage ?? 0f;
    public float MoveSpeed => enemyData?.moveSpeed ?? 0f;
    public bool IsKnockbackImmune => isKnockbackImmune;
    public string EnemyName => enemyData?.enemyName ?? "Unknown Enemy";
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
        var enemyAI = GetComponent<EnemyAI>();
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
        cachedTransform.localScale = originalScale;
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

        // 플레이어 참조가 없는 경우 GameManager에서 가져오기
        if (targetTransform == null && GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            Initialize(GameManager.Instance.PlayerTransform);
        }

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

        int playerLevel = GameManager.Instance.PlayerStats.Level;
        calculatedMaxHealth = Mathf.Min(
            enemyData.baseHealth * playerLevel,
            enemyData.maxPossibleHealth
        );
        currentHealth = calculatedMaxHealth;
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
        if (floatingTextManager != null && floatingTextManager.isFloatingTextEnabled)
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

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
        ResetBounceEffect();
    }
}