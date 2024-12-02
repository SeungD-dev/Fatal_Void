using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Delegate 정의
    public delegate void StatChangeHandler(float value);
    public delegate void IntChangeHandler(int value);
    public delegate void MovementSpeedChangeHandler(float newSpeed);
    public delegate void LevelChangeHandler(int value);
    public delegate void VoidHandler();

    // Public Delegates
    public StatChangeHandler OnHealthChanged;
    public StatChangeHandler OnExpChanged;
    public LevelChangeHandler OnLevelUp;
    public IntChangeHandler OnKillCountChanged;
    public IntChangeHandler OnCoinChanged;
    public VoidHandler OnPlayerDeath;

    public event MovementSpeedChangeHandler OnMovementSpeedChanged;

    [Header("Level Settings")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0;
    [SerializeField] private float requiredExp = 30;

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
    private float luck;
    private float intelligence;

    [Header("Base Stats")]
    [SerializeField] private float baseHealth = 100f;
    [SerializeField] private float baseHealthRegen = 1f;
    [SerializeField] private float basePower = 10f;
    [SerializeField] private float baseMovementSpeed = 5f;
    [SerializeField] private float baseCooldownReduce = 0f;
    [SerializeField] private float baseLuck = 1f;
    [SerializeField] private float baseIntelligence = 1f;

    [Header("Stats Per Level")]
    [SerializeField] private float healthPerLevel = 10f;
    [SerializeField] private float healthRegenPerLevel = 0.2f;
    [SerializeField] private float powerPerLevel = 2f;
    [SerializeField] private float movementSpeedPerLevel = 0.2f;
    [SerializeField] private float cooldownReducePerLevel = 0.05f;
    [SerializeField] private float luckPerLevel = 0.1f;
    [SerializeField] private float intelligencePerLevel = 0.2f;

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
    public float Luck => luck;
    public float Intelligence => intelligence;
    #endregion

    public void InitializeStats()
    {
        if (isInitialized) return;

        level = 0;
        currentExp = 0;
        requiredExp = 100;
        killCount = 0;
        coinCount = 0;

        UpdateStats();
        LevelUp();

        isInitialized = true;
    }

    private void UpdateStats()
    {
        float previousMovementSpeed = movementSpeed;

        maxHealth = baseHealth + (healthPerLevel * (level - 1));
        healthRegen = baseHealthRegen + (healthRegenPerLevel * (level - 1));
        power = basePower + (powerPerLevel * (level - 1));
        movementSpeed = baseMovementSpeed + (movementSpeedPerLevel * (level - 1));
        cooldownReduce = baseCooldownReduce + (cooldownReducePerLevel * (level - 1));
        luck = baseLuck + (luckPerLevel * (level - 1));
        intelligence = baseIntelligence + (intelligencePerLevel * (level - 1));

        // 이동 속도가 변경되었을 때만 이벤트 발생
        if (previousMovementSpeed != movementSpeed)
        {
            OnMovementSpeedChanged?.Invoke(movementSpeed);
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
    public void ModifyMovementSpeed(float modifier, bool isPercentage = false)
    {
        if (isPercentage)
        {
            movementSpeed += baseMovementSpeed * (modifier / 100f);
        }
        else
        {
            movementSpeed += modifier;
        }

        // 최소 이동 속도 보장
        movementSpeed = Mathf.Max(baseMovementSpeed * 0.5f, movementSpeed);
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