using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    #region Delegates
    public delegate void StatChangeHandler(float value);
    public delegate void IntChangeHandler(int value);
    public delegate void MovementSpeedChangeHandler(float newSpeed);
    public delegate void LevelChangeHandler(int value);
    public delegate void VoidHandler();
    public delegate void StatChangeDelegate();

    // Events
    public StatChangeHandler OnHealthChanged;
    public StatChangeHandler OnExpChanged;
    public LevelChangeHandler OnLevelUp;
    public IntChangeHandler OnKillCountChanged;
    public IntChangeHandler OnCoinChanged;
    public VoidHandler OnPlayerDeath;
    public event StatChangeDelegate OnPowerChanged;
    public event StatChangeDelegate OnCooldownReduceChanged;
    public event StatChangeDelegate OnKnockbackChanged;
    public event StatChangeDelegate OnAreaOfEffectChanged;
    public event MovementSpeedChangeHandler OnMovementSpeedChanged;
    #endregion

    #region Serialized Fields
    [Header("Level Settings")]
    [SerializeField] private int initialRequiredExp = 100;
    
    [Header("Economy Settings")]
    [SerializeField] private int initialCoinCount = 0;

    [Header("Base Stats")]
    [SerializeField] private float baseHealth = 100f;
    [SerializeField] private float baseHealthRegen = 1f;
    [SerializeField] private float basePower = 10f;
    [SerializeField] private float baseMovementSpeed = 5f;
    [SerializeField] private float baseCooldownReduce = 0f;
    [SerializeField] private float baseKnockback = 1f;
    [SerializeField] private float baseAreaOfEffect = 1f;

    [Header("Stats Per Level - DISABLED")]
    [SerializeField] private float healthPerLevel = 0f; // 비활성화: 레벨업 시 체력 증가 없음
    [SerializeField] private float healthRegenPerLevel = 0f; // 비활성화: 레벨업 시 체력재생 증가 없음
    [SerializeField] private float powerPerLevel = 0f; // 비활성화: 레벨업 시 공격력 증가 없음
    [SerializeField] private float movementSpeedPerLevel = 0f; // 비활성화: 레벨업 시 이속 증가 없음
    [SerializeField] private float cooldownReducePerLevel = 0f; // 비활성화: 레벨업 시 쿨감 증가 없음
    [SerializeField] private float knockbackIncreasePerLevel = 0f; // 비활성화: 레벨업 시 넉백 증가 없음
    [SerializeField] private float aoeIncreasePerLevel = 0f; // 비활성화: 레벨업 시 범위 증가 없음

    [Header("Item Pickup")]
    [SerializeField] private float basePickupRange = 5f;

    [Header("Hit Effect")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    #endregion

    #region Private Fields
    private int level = 1;
    private int currentExp;
    private int requiredExp = 1;
    private float currentHealth;
    private int killCount;
    private int coinCount;

    // Current Stats
    private float maxHealth;
    private float healthRegen;
    private float power;
    private float movementSpeed;
    private float cooldownReduce;
    private float knockback;
    private float aoe;
    private float pickupRange;

    // Temporary Buffs (for weapon effects like Phantom Saber)
    private float temporaryPower = 0f;
    private float temporaryHaste = 0f; // Haste = CooldownReduce
    private float temporaryMovementSpeed = 0f;

    // Cached Components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private ShopController cachedShopController;

    // State Flags
    private bool isInitialized;
    private bool isModifyingStats;
    private bool isLevelingUp;
    private bool isFlashing;

    // Optimization
    private static readonly WaitForSeconds HitFlashWait;
    private const float StatUpdateThreshold = 0.1f;
    private float lastStatUpdateTime;

    // Health Regeneration
    private Coroutine healthRegenCoroutine;
    private float lastHealthRegenTime;
    #endregion

    #region Properties
    public bool IsInitialized => isInitialized;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int Level => level;
    public float CurrentExp => currentExp;
    public float RequiredExp => requiredExp;
    public int KillCount => killCount;
    public int CoinCount => coinCount;
    public float Power => power;
    public float MovementSpeed => movementSpeed;
    public float HealthRegen => healthRegen;
    public float CooldownReduce => cooldownReduce;
    public float Knockback => knockback;
    public float AreaOfEffect => aoe;
    public float PickupRange => pickupRange;
    
    // Total stats including temporary buffs
    public float TotalPower => power + temporaryPower;
    public float TotalMovementSpeed => movementSpeed + temporaryMovementSpeed;
    public float TotalHaste => cooldownReduce + temporaryHaste;
    #endregion

    static PlayerStats()
    {
        HitFlashWait = new WaitForSeconds(0.1f);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        cachedShopController = GameManager.Instance?.ShopController;
    }

    public void InitializeStats()
    {
        if (isInitialized) return;

        level = 0;
        currentExp = 0;
        requiredExp = initialRequiredExp;
        killCount = 0;
        coinCount = initialCoinCount;
        pickupRange = basePickupRange;

        // 첫 초기화에서만 체력을 최대체력으로 설정
        UpdateStats();
        LevelUp();  // 첫 초기화에서 레벨 설정

        // 기본 체력 재생 코루틴 시작
        if (healthRegen > 0 && healthRegenCoroutine == null)
        {
            healthRegenCoroutine = StartCoroutine(HealthRegenCoroutine());
        }

        isInitialized = true;
    }

    private void UpdateStats()
    {
        if (Time.time - lastStatUpdateTime < StatUpdateThreshold) return;

        float previousMovementSpeed = movementSpeed;
        float previousPower = power;
        float previousCooldownReduce = cooldownReduce;
        float previousKnockback = knockback;
        float previousAoe = aoe;

        // 레벨 기반 스탯 증가 제거 - 이제 기본 스탯 + 아이템 효과만 적용
        maxHealth = baseHealth;
        healthRegen = baseHealthRegen;

        // 임시 버프를 고려하여 스탯 계산 (레벨 보너스 제거)
        power = basePower + temporaryPower;
        movementSpeed = baseMovementSpeed + temporaryMovementSpeed;
        cooldownReduce = baseCooldownReduce + temporaryHaste;

        knockback = baseKnockback;
        aoe = baseAreaOfEffect;

        bool statsChanged = false;
        if (previousMovementSpeed != movementSpeed)
        {
            OnMovementSpeedChanged?.Invoke(movementSpeed);
            statsChanged = true;
        }
        if (previousPower != power)
        {
            OnPowerChanged?.Invoke();
            statsChanged = true;
        }
        if (previousCooldownReduce != cooldownReduce)
        {
            OnCooldownReduceChanged?.Invoke();
            statsChanged = true;
        }
        if (previousKnockback != knockback)
        {
            OnKnockbackChanged?.Invoke();
            statsChanged = true;
        }
        if (previousAoe != aoe)
        {
            OnAreaOfEffectChanged?.Invoke();
            statsChanged = true;
        }

        if (statsChanged)
        {
            lastStatUpdateTime = Time.time;
        }

        // Initialize health only during first initialization
        if (!isInitialized)
        {
            currentHealth = maxHealth;
        }
        else
        {
            // During level up, maintain current health ratio or cap at new max
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }

    #region Resource Management
    public void AddKill()
    {
        killCount++;
        OnKillCountChanged?.Invoke(killCount);
    }

    public void AddCoins(int amount)
    {
        coinCount += amount;
        OnCoinChanged?.Invoke(coinCount);
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (Time.time - lastStatUpdateTime >= StatUpdateThreshold)
        {
            OnHealthChanged?.Invoke(currentHealth);
            lastStatUpdateTime = Time.time;
        }

        if (!isFlashing)
        {
            PlayHitEffect();
        }

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

    public bool SpendCoins(int amount)
    {
        if (coinCount >= amount)
        {
            coinCount -= amount;
            OnCoinChanged?.Invoke(coinCount);
            return true;
        }
        return false;
    }

    public void Heal(float amount)
    {
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (currentHealth != oldHealth && Time.time - lastStatUpdateTime >= StatUpdateThreshold)
        {
            OnHealthChanged?.Invoke(currentHealth);
            lastStatUpdateTime = Time.time;
        }
    }

    private void Die()
    {
        OnPlayerDeath?.Invoke();
        GameManager.Instance.SetGameState(GameState.GameOver);
    }
    #endregion

    #region Level and Experience
    public void AddExperience(float expAmount)
    {
        if (expAmount <= 0 || isLevelingUp) return;

        int expToAdd = Mathf.RoundToInt(expAmount);
        currentExp += expToAdd;

        while (currentExp >= requiredExp && !isLevelingUp)
        {
            int overflow = currentExp - requiredExp;
            LevelUp();
            currentExp = overflow;
        }

        // ExpBar 실시간 업데이트를 위해 임계값 무시하고 즉시 이벤트 발생
        OnExpChanged?.Invoke(currentExp);
    }

    public void LevelUp()
    {
        if (isLevelingUp) return;

        isLevelingUp = true;

        level++;
        requiredExp = Mathf.RoundToInt(requiredExp * 1.2f);

        // 레벨업 시 스탯 증가 없음 - 레벨은 X-Tier 업그레이드 재화로만 사용
        // UpdateStats(); // 비활성화: 레벨업 시 스탯 변경 안함

        OnLevelUp?.Invoke(level);

        // 레벨업 시 ExpBar 즉시 업데이트 (현재 경험치로 리셋)
        OnExpChanged?.Invoke(currentExp);

        // 체력은 기존 임계값 조건 유지
        if (Time.time - lastStatUpdateTime >= StatUpdateThreshold)
        {
            OnHealthChanged?.Invoke(currentHealth);
            lastStatUpdateTime = Time.time;
        }

        //if (GameManager.Instance != null)
        //{
        //    GameManager.Instance.SetGameState(GameState.Paused);
        //    ShowShopUI();
        //}

        isLevelingUp = false;
    }

    private void ShowShopUI()
    {
        if (cachedShopController == null)
        {
            cachedShopController = GameManager.Instance?.ShopController;
        }

        if (cachedShopController != null)
        {
            if (level >= 2)
            {
                cachedShopController.isFirstShop = false;
            }
            cachedShopController.InitializeShop();
        }
    }
    #endregion

    #region Stat Modification
    public void ModifyPower(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            power += amount;
            power = Mathf.Max(basePower, power);
            OnPowerChanged?.Invoke();
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyMaxHealth(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            float oldMaxHealth = maxHealth;
            maxHealth += amount;
            maxHealth = Mathf.Max(baseHealth, maxHealth);

            if (oldMaxHealth > 0)
            {
                float healthRatio = currentHealth / oldMaxHealth;
                currentHealth = maxHealth * healthRatio;
                OnHealthChanged?.Invoke(currentHealth);
            }
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyCooldownReduce(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            cooldownReduce += amount;
            cooldownReduce = Mathf.Max(baseCooldownReduce, cooldownReduce);
            OnCooldownReduceChanged?.Invoke();
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyKnockback(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            knockback += amount;
            knockback = Mathf.Max(baseKnockback, knockback);
            OnKnockbackChanged?.Invoke();
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyAreaOfEffect(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            aoe += amount;
            aoe = Mathf.Max(baseAreaOfEffect, aoe);
            OnAreaOfEffectChanged?.Invoke();
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyPickupRange(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            pickupRange += amount;
            pickupRange = Mathf.Max(basePickupRange, pickupRange);
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyMovementSpeed(float amount, bool isPercentage)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            if (isPercentage)
            {
                movementSpeed += baseMovementSpeed * (amount / 100f);
            }
            else
            {
                movementSpeed += amount;
            }
            movementSpeed = Mathf.Max(baseMovementSpeed * 0.5f, movementSpeed);
            OnMovementSpeedChanged?.Invoke(movementSpeed);
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void ModifyHealthRegen(float amount)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            healthRegen += amount;
            healthRegen = Mathf.Max(baseHealthRegen, healthRegen);

            // 체력 재생이 0보다 크면 재생 코루틴 시작
            if (healthRegen > 0 && healthRegenCoroutine == null)
            {
                healthRegenCoroutine = StartCoroutine(HealthRegenCoroutine());
            }
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void SetMovementSpeed(float newSpeed)
    {
        if (movementSpeed != newSpeed)
        {
            movementSpeed = newSpeed;
            OnMovementSpeedChanged?.Invoke(movementSpeed);
        }
    }

    public void ResetMovementSpeed()
    {
        // 레벨 기반 이동속도 증가 제거 - 기본 속도 + 임시 버프만 적용
        float newSpeed = baseMovementSpeed + temporaryMovementSpeed;
        if (movementSpeed != newSpeed)
        {
            movementSpeed = newSpeed;
            OnMovementSpeedChanged?.Invoke(movementSpeed);
        }
    }
    #endregion

    private Coroutine periodicMagnetCoroutine;

    public void EnablePeriodicMagnetEffect(bool enable)
    {
        if (enable)
        {
            if (periodicMagnetCoroutine == null)
            {
                periodicMagnetCoroutine = StartCoroutine(PeriodicMagnetEffectCoroutine());
            }
        }
        else
        {
            if (periodicMagnetCoroutine != null)
            {
                StopCoroutine(periodicMagnetCoroutine);
                periodicMagnetCoroutine = null;
            }
        }
    }

    private IEnumerator PeriodicMagnetEffectCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);  // 30초 주기

            // 30초마다 즉시 자석 효과 발생
            if (GameManager.Instance?.CombatController != null)
            {
                GameManager.Instance.CombatController.ApplyItemEffect(ItemType.Magnet);
            }
        }
    }

    #region Temporary Buff Management
    /// <summary>
    /// 임시 Power 버프를 추가합니다 (Phantom Saber 등의 무기 효과용)
    /// </summary>
    /// <param name="amount">추가할 Power 수치</param>
    public void AddTemporaryPower(float amount)
    {
        temporaryPower += amount;
        UpdateStats();
    }

    /// <summary>
    /// 임시 Power 버프를 제거합니다
    /// </summary>
    /// <param name="amount">제거할 Power 수치</param>
    public void RemoveTemporaryPower(float amount)
    {
        temporaryPower = Mathf.Max(0f, temporaryPower - amount);
        UpdateStats();
    }

    /// <summary>
    /// 임시 Haste(쿨다운 감소) 버프를 추가합니다
    /// </summary>
    /// <param name="amount">추가할 Haste 수치</param>
    public void AddTemporaryHaste(float amount)
    {
        temporaryHaste += amount;
        UpdateStats();
    }

    /// <summary>
    /// 임시 Haste 버프를 제거합니다
    /// </summary>
    /// <param name="amount">제거할 Haste 수치</param>
    public void RemoveTemporaryHaste(float amount)
    {
        temporaryHaste = Mathf.Max(0f, temporaryHaste - amount);
        UpdateStats();
    }

    /// <summary>
    /// 임시 이동속도 버프를 추가합니다
    /// </summary>
    /// <param name="amount">추가할 이동속도 수치</param>
    public void AddTemporaryMovementSpeed(float amount)
    {
        temporaryMovementSpeed += amount;
        // movementSpeed는 이미 temporaryMovementSpeed를 포함하고 있으므로 UpdateStats() 호출
        UpdateStats();
    }

    /// <summary>
    /// 임시 이동속도 버프를 제거합니다
    /// </summary>
    /// <param name="amount">제거할 이동속도 수치</param>
    public void RemoveTemporaryMovementSpeed(float amount)
    {
        temporaryMovementSpeed = Mathf.Max(0f, temporaryMovementSpeed - amount);
        // movementSpeed는 이미 temporaryMovementSpeed를 포함하고 있으므로 UpdateStats() 호출
        UpdateStats();
    }

    /// <summary>
    /// 모든 임시 버프를 제거합니다
    /// </summary>
    public void ClearAllTemporaryBuffs()
    {
        bool hasAnyBuff = temporaryPower > 0f || temporaryHaste > 0f || temporaryMovementSpeed > 0f;

        temporaryPower = 0f;
        temporaryHaste = 0f;
        temporaryMovementSpeed = 0f;

        // 변경사항이 있으면 통합적으로 스탯 업데이트
        if (hasAnyBuff)
        {
            UpdateStats();
        }
    }
    #endregion

    /// <summary>
    /// X-Ƽ�� ���׷��̵带 ���� �÷��̾� ������ �����մϴ�.
    /// </summary>
    /// <param name="levels">������ ���� ��</param>
    /// <returns>���� ���� ����</returns>
    public bool SubtractLevels(int levels)
    {
        if (levels <= 0)
        {
            return false;
        }

        // ���� ������ ������ �������� ū�� Ȯ��
        if (level <= levels)
        {
            Debug.LogWarning("������ �����Ͽ� ������ �� �����ϴ�.");
            return false;
        }

        // 레벨 차감 (최소 1 유지)
        int newLevel = Mathf.Max(1, level - levels);
        level = newLevel;

        // 레벨 변경 이벤트 발생 (스탯은 변경하지 않음)
        OnLevelUp?.Invoke(newLevel);

        Debug.Log($"�÷��̾� ������ {levels}��ŭ �����߽��ϴ�. �� ����: {newLevel}");
        return true;
    }

    private void OnDestroy()
    {
        // �̺�Ʈ �ڵ鷯 ����
        // 주기적 자석 효과 코루틴 정리
        if (periodicMagnetCoroutine != null)
        {
            StopCoroutine(periodicMagnetCoroutine);
            periodicMagnetCoroutine = null;
        }

        OnHealthChanged = null;
        OnExpChanged = null;
        OnLevelUp = null;
        OnKillCountChanged = null;
        OnCoinChanged = null;
        OnPlayerDeath = null;
        OnMovementSpeedChanged = null;
        OnPowerChanged = null;
        OnCooldownReduceChanged = null;
        OnKnockbackChanged = null;
        OnAreaOfEffectChanged = null;

        // 체력 재생 코루틴 정리
        if (healthRegenCoroutine != null)
        {
            StopCoroutine(healthRegenCoroutine);
            healthRegenCoroutine = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearSceneReferences();
        }
    }

    /// <summary>
    /// 체력 자동 재생 코루틴
    /// </summary>
    private IEnumerator HealthRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // 1초마다 체력 재생

            if (currentHealth < maxHealth && healthRegen > 0)
            {
                float regenAmount = healthRegen;
                Heal(regenAmount);
            }
        }
    }
}