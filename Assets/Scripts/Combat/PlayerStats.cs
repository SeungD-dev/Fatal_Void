using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Delegate 정의
    public delegate void StatChangeHandler(float value);
    public delegate void LevelChangeHandler(int value);
    public delegate void VoidHandler();

    // Public Delegates
    public StatChangeHandler OnHealthChanged;
    public StatChangeHandler OnExpChanged;
    public LevelChangeHandler OnLevelUp;
    public VoidHandler OnPlayerDeath;

    [Header("Level Settings")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0;
    [SerializeField] private float requiredExp = 100;

    [Header("Stat Settings")]
    [SerializeField] public float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] public float healthRegen;
    [SerializeField] public float power;
    [SerializeField] public float movementSpeed;
    [SerializeField] public float cooldownReduce;
    [SerializeField] public float luck;
    [SerializeField] public float intelligence;

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


    // Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int Level => level;
    public float CurrentExp => currentExp;
    public float RequiredExp => requiredExp;

    private void Start()
    {
        if (GameManager.Instance.currentGameState == GameState.Playing)
        {
            GameManager.Instance.SetPlayerStats(this);
        }
    }

    public void InitializeStats()
    {
        level = 0;  // 시작은 0레벨
        currentExp = 0;
        requiredExp = 100;

        // 기본 스탯 설정
        maxHealth = baseHealth;
        healthRegen = baseHealthRegen;
        power = basePower;
        movementSpeed = baseMovementSpeed;
        cooldownReduce = baseCooldownReduce;
        luck = baseLuck;
        intelligence = baseIntelligence;

        currentHealth = maxHealth;

        // 게임 시작 시 자동으로 레벨 1로 시작
        LevelUp();
    }

    private void UpdateStats()
    {
        maxHealth = baseHealth + (healthPerLevel * (level - 1));
        healthRegen = baseHealthRegen + (healthRegenPerLevel * (level - 1));
        power = basePower + (powerPerLevel * (level - 1));
        movementSpeed = baseMovementSpeed + (movementSpeedPerLevel * (level - 1));
        cooldownReduce = baseCooldownReduce + (cooldownReducePerLevel * (level - 1));
        luck = baseLuck + (luckPerLevel * (level - 1));
        intelligence = baseIntelligence + (intelligencePerLevel * (level - 1));

        // 레벨업 시 체력 전체 회복
        currentHealth = maxHealth;
    }




    private void OnDestroy()
    {
        // Delegate 정리
        OnHealthChanged = null;
        OnExpChanged = null;
        OnLevelUp = null;
        OnPlayerDeath = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearPlayerStats();
        }
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
        Debug.Log("Player Died");
    }

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
        currentExp -= requiredExp;
        requiredExp *= 1.2f;

        UpdateStats();

        OnLevelUp?.Invoke(level);
        OnHealthChanged?.Invoke(currentHealth);
        OnExpChanged?.Invoke(currentExp);

        // 레벨업 시 게임 일시정지 및 상점 UI 표시
        GameManager.Instance.SetGameState(GameState.Paused);
        ShowShopUI();
    }

    private void ShowShopUI()
    {
        ShopController shopController = FindFirstObjectByType<ShopController>();
        if (shopController != null)
        {
            shopController.InitializeShop();
        }
    }

}