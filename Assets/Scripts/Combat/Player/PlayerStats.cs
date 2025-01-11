using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public delegate void StatChangeHandler(float value);
    public delegate void IntChangeHandler(int value);
    public delegate void MovementSpeedChangeHandler(float newSpeed);
    public delegate void LevelChangeHandler(int value);
    public delegate void VoidHandler();
    public delegate void StatChangeDelegate();

    // Public Delegates
    public StatChangeHandler OnHealthChanged;
    public StatChangeHandler OnExpChanged;
    public LevelChangeHandler OnLevelUp;
    public IntChangeHandler OnKillCountChanged;
    public IntChangeHandler OnCoinChanged;
    public VoidHandler OnPlayerDeath;

    public delegate void MagnetEffectHandler(bool isActive);
    public event MagnetEffectHandler OnMagnetEffectChanged;

    public event StatChangeDelegate OnPowerChanged;
    public event StatChangeDelegate OnCooldownReduceChanged;
    public event StatChangeDelegate OnKnockbackChanged;
    public event StatChangeDelegate OnAreaOfEffectChanged;

    public event MovementSpeedChangeHandler OnMovementSpeedChanged;

    [Header("Level Settings")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int requiredExp = 1;
    [SerializeField] private int initialRequiredExp = 100;
    [Header("Resource Stats")]
    private float currentHealth;
    private int killCount = 0;
    private int coinCount = 0;

    [Header("Current Stats")]
    private float maxHealth;
    private float healthRegen;
    private float power;
    private float movementSpeed;
    private float cooldownReduce;
    private float knockback;
    private float aoe;

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
    private float pickupRange;

   [Header("Magnet Effect")]
    private bool hasMagnetEffect = false;
    private float magnetEffectCooldown = 30f;
    private float lastMagnetEffectTime = -30f;  // 처음에 바로 사용할 수 있도록

    private bool isLevelingUp = false;
    private ShopController cachedShopController;


    private bool isInitialized = false;
    private bool isModifyingStats = false;

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
        float previousMovementSpeed = movementSpeed;
        float previousPower = power;
        float previousCooldownReduce = cooldownReduce;
        float previousKnockback = knockback;
        float previousAoe = aoe;

        maxHealth = baseHealth + (healthPerLevel * (level - 1));
        healthRegen = baseHealthRegen + (healthRegenPerLevel * (level - 1));
        power = basePower + (powerPerLevel * (level - 1));
        movementSpeed = baseMovementSpeed + (movementSpeedPerLevel * (level - 1));
        cooldownReduce = baseCooldownReduce + (cooldownReducePerLevel * (level - 1));
        knockback = baseKnockback + (knockbackIncreasePerLevel * (level - 1));
        aoe = baseAreaOfEffect + (aoeIncreasePerLevel * (level - 1));

        // 변경된 스탯에 대해서만 이벤트 호출
        if (previousMovementSpeed != movementSpeed)
        {
            OnMovementSpeedChanged?.Invoke(movementSpeed);
        }
        if (previousPower != power)
        {
            OnPowerChanged?.Invoke();
        }
        if (previousCooldownReduce != cooldownReduce)
        {
            OnCooldownReduceChanged?.Invoke();
        }
        if (previousKnockback != knockback)
        {
            OnKnockbackChanged?.Invoke();
        }
        if (previousAoe != aoe)
        {
            OnAreaOfEffectChanged?.Invoke();
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
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
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

        if (currentHealth != oldHealth)
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    private void Die()
    {
        OnPlayerDeath?.Invoke();
        GameManager.Instance.SetGameState(GameState.GameOver);
        Debug.Log("Player Died");
    }
    #endregion

    #region Level and Experience
    public void AddExperience(float expAmount)
    {
        if (expAmount <= 0) return;

        int expToAdd = Mathf.RoundToInt(expAmount);  // 소수점 경험치를 정수로 변환
        currentExp += expToAdd;

        while (currentExp >= requiredExp)
        {
            if (!isLevelingUp)  // 레벨업 중복 방지
            {
                int overflow = currentExp - requiredExp;
                LevelUp();
                currentExp = overflow;  // 남은 경험치 적용
            }
            else
            {
                break;  // 레벨업 중이면 추가 레벨업 방지
            }
        }

        OnExpChanged?.Invoke(currentExp);
    }

    public void LevelUp()
    {
        if (isLevelingUp) return;

        isLevelingUp = true;

        level++;

        // 다음 레벨 경험치 요구량 계산 (20% 증가)
        requiredExp = Mathf.RoundToInt(requiredExp * 1.2f);

        UpdateStats();

        OnLevelUp?.Invoke(level);
        OnHealthChanged?.Invoke(currentHealth);
        OnExpChanged?.Invoke(currentExp);

        // GameState 변경과 상점 열기를 분리
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
            ShowShopUI();
        }

        isLevelingUp = false;
    }
    private void ShowShopUI()
    {
        if (cachedShopController != null)
        {
            // 첫 상점이 아닌 경우 (레벨 2 이상)
            if (level >= 2)
            {
                cachedShopController.isFirstShop = false;  // 첫 상점 플래그를 false로 설정
            }
            cachedShopController.InitializeShop();
        }
        else if (GameManager.Instance?.ShopController != null)
        {
            cachedShopController = GameManager.Instance.ShopController;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
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
        try
        {
            isModifyingStats = true;
            healthRegen *= (1f + modifier);
        }
        finally
        {
            isModifyingStats = false;
        }
    }

    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }

    public void ResetMovementSpeed()
    {
        movementSpeed = baseMovementSpeed + (movementSpeedPerLevel * (level - 1));
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

            // 3초 동안 자석 효과 지속
            yield return new WaitForSeconds(3f);

            hasMagnetEffect = false;
            OnMagnetEffectChanged?.Invoke(false);

            // 27초 대기 (총 30초 주기)
            yield return new WaitForSeconds(27f);
        }
    }

    private void OnDestroy()
    {
        OnHealthChanged = null;
        OnExpChanged = null;
        OnLevelUp = null;
        OnKillCountChanged = null;
        OnCoinChanged = null;
        OnPlayerDeath = null;
        OnMovementSpeedChanged = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearSceneReferences();
        }
    }
}