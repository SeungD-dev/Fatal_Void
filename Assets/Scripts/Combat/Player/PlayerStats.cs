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
    [SerializeField] private float currentExp = 0;
    [SerializeField] private float requiredExp = 1;
    [SerializeField] private float initialRequiredExp = 100;
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

   


    private bool isInitialized = false;

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
        LevelUp();

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
    public void AddExperience(float exp)
    {
        currentExp += exp;
        OnExpChanged?.Invoke(currentExp);

        if (currentExp >= requiredExp)
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        level++;
        if (level > 1)
        {
            currentExp -= requiredExp;
        }
        requiredExp *= 1.2f;

        UpdateStats();

        OnLevelUp?.Invoke(level);
        OnHealthChanged?.Invoke(currentHealth);
        OnExpChanged?.Invoke(currentExp);

        GameManager.Instance.SetGameState(GameState.Paused);
        ShowShopUI();
    }

    private void ShowShopUI()
    {
        var shopController = GameManager.Instance.ShopController;
        if (shopController != null)
        {
            shopController.InitializeShop();
        }
    }
    #endregion

    #region Stat Modification


    public void ModifyPower(float amount)
    {
        // 직접적인 증가값으로 변경
        power += amount;
        power = Mathf.Max(basePower, power);
        OnPowerChanged?.Invoke();
    }


    public void ModifyMaxHealth(float amount)
    {
        float oldMaxHealth = maxHealth;
        // 직접적인 증가값으로 변경
        maxHealth += amount;
        maxHealth = Mathf.Max(baseHealth, maxHealth);

        if (oldMaxHealth > 0)
        {
            float healthRatio = currentHealth / oldMaxHealth;
            currentHealth = maxHealth * healthRatio;
            OnHealthChanged?.Invoke(currentHealth);
        }
    }
    public void ModifyCooldownReduce(float amount)
    {
        // 직접적인 증가값으로 변경
        cooldownReduce += amount;
        cooldownReduce = Mathf.Max(baseCooldownReduce, cooldownReduce);
        OnCooldownReduceChanged?.Invoke();
    }

    public void ModifyKnockback(float amount)
    {
        // 직접적인 증가값으로 변경
        knockback += amount;
        knockback = Mathf.Max(baseKnockback, knockback);
        OnKnockbackChanged?.Invoke();
    }

    public void ModifyAreaOfEffect(float amount)
    {
        // 직접적인 증가값으로 변경
        aoe += amount;
        aoe = Mathf.Max(baseAreaOfEffect, aoe);
        OnAreaOfEffectChanged?.Invoke();
    }

    public void ModifyPickupRange(float amount)
    {
        pickupRange += amount;
        pickupRange = Mathf.Max(basePickupRange, pickupRange);
    }

    public void ModifyMovementSpeed(float amount, bool isPercentage)
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

    public void ModifyHealthRegen(float modifier)
    {
        healthRegen *= (1f + modifier);
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