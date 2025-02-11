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
    public delegate void MagnetEffectHandler(bool isActive);

    // Events
    public StatChangeHandler OnHealthChanged;
    public StatChangeHandler OnExpChanged;
    public LevelChangeHandler OnLevelUp;
    public IntChangeHandler OnKillCountChanged;
    public IntChangeHandler OnCoinChanged;
    public VoidHandler OnPlayerDeath;
    public event MagnetEffectHandler OnMagnetEffectChanged;
    public event StatChangeDelegate OnPowerChanged;
    public event StatChangeDelegate OnCooldownReduceChanged;
    public event StatChangeDelegate OnKnockbackChanged;
    public event StatChangeDelegate OnAreaOfEffectChanged;
    public event MovementSpeedChangeHandler OnMovementSpeedChanged;
    #endregion

    #region Serialized Fields
    [Header("Level Settings")]
    [SerializeField] private int initialRequiredExp = 100;

    [Header("Base Stats")]
    [SerializeField] private float baseHealth = 100f;
    [SerializeField] private float baseHealthRegen = 1f;
    [SerializeField] private float basePower = 10f;
    [SerializeField] private float baseMovementSpeed = 5f;
    [SerializeField] private float baseCooldownReduce = 0f;
    [SerializeField] private float baseKnockback = 1f;
    [SerializeField] private float baseAreaOfEffect = 1f;

    [Header("Stats Per Level")]
    [SerializeField] private float healthPerLevel = 10f;
    [SerializeField] private float healthRegenPerLevel = 0.2f;
    [SerializeField] private float powerPerLevel = 2f;
    [SerializeField] private float movementSpeedPerLevel = 0.2f;
    [SerializeField] private float cooldownReducePerLevel = 0.05f;
    [SerializeField] private float knockbackIncreasePerLevel = 0.1f;
    [SerializeField] private float aoeIncreasePerLevel = 0.2f;

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

    // Cached Components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private ShopController cachedShopController;

    // State Flags
    private bool isInitialized;
    private bool isModifyingStats;
    private bool isLevelingUp;
    private bool isFlashing;
    private bool hasMagnetEffect;

    // Optimization
    private static readonly WaitForSeconds HitFlashWait;
    private static readonly WaitForSeconds MagnetEffectDuration = new WaitForSeconds(3f);
    private static readonly WaitForSeconds MagnetEffectCooldown = new WaitForSeconds(27f);
    private const float StatUpdateThreshold = 0.1f;
    private float lastStatUpdateTime;
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
    public bool HasMagnetEffect => hasMagnetEffect;
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
        coinCount = 0;
        pickupRange = basePickupRange;

        UpdateStats();
        LevelUp();  // 첫 레벨업으로 상점 열기

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

        int levelMinus1 = level - 1;
        maxHealth = baseHealth + (healthPerLevel * levelMinus1);
        healthRegen = baseHealthRegen + (healthRegenPerLevel * levelMinus1);
        power = basePower + (powerPerLevel * levelMinus1);
        movementSpeed = baseMovementSpeed + (movementSpeedPerLevel * levelMinus1);
        cooldownReduce = baseCooldownReduce + (cooldownReducePerLevel * levelMinus1);
        knockback = baseKnockback + (knockbackIncreasePerLevel * levelMinus1);
        aoe = baseAreaOfEffect + (aoeIncreasePerLevel * levelMinus1);

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

        currentHealth = maxHealth;
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

        if (Time.time - lastStatUpdateTime >= StatUpdateThreshold)
        {
            OnExpChanged?.Invoke(currentExp);
            lastStatUpdateTime = Time.time;
        }
    }

    public void LevelUp()
    {
        if (isLevelingUp) return;

        isLevelingUp = true;

        level++;
        requiredExp = Mathf.RoundToInt(requiredExp * 1.2f);

        UpdateStats();

        OnLevelUp?.Invoke(level);

        if (Time.time - lastStatUpdateTime >= StatUpdateThreshold)
        {
            OnHealthChanged?.Invoke(currentHealth);
            OnExpChanged?.Invoke(currentExp);
            lastStatUpdateTime = Time.time;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
            ShowShopUI();
        }

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

    public void ModifyHealthRegen(float modifier)
    {
        if (isModifyingStats) return;

        isModifyingStats = true;
        try
        {
            healthRegen *= (1f + modifier);
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
        float newSpeed = baseMovementSpeed + (movementSpeedPerLevel * (level - 1));
        if (movementSpeed != newSpeed)
        {
            movementSpeed = newSpeed;
            OnMovementSpeedChanged?.Invoke(movementSpeed);
        }
    }
    #endregion

    public void EnablePeriodicMagnetEffect(bool enable)
    {
        if (enable && !hasMagnetEffect)
        {
            StartCoroutine(PeriodicMagnetEffectCoroutine());
        }
        else if (!enable)
        {
            hasMagnetEffect = false;
            OnMagnetEffectChanged?.Invoke(false);
        }
    }

    private IEnumerator PeriodicMagnetEffectCoroutine()
    {
        while (true)
        {
            hasMagnetEffect = true;
            OnMagnetEffectChanged?.Invoke(true);

            yield return MagnetEffectDuration;  // 캐시된 WaitForSeconds 사용

            hasMagnetEffect = false;
            OnMagnetEffectChanged?.Invoke(false);

            yield return MagnetEffectCooldown;  // 캐시된 WaitForSeconds 사용
        }
    }

    private void OnDestroy()
    {
        // 이벤트 핸들러 정리
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
        OnMagnetEffectChanged = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearSceneReferences();
        }
    }
}