using UnityEngine;

public class Enemy : MonoBehaviour, IPooledObject
{
    [SerializeField] private EnemyData enemyData;
    private float currentHealth;
    private float calculatedMaxHealth;
    private float lastDamageTime;
    private const float damageDelay = 1f;

    // 컴포넌트 캐싱
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetEnemyData(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError("Attempted to set null EnemyData!");
            return;
        }

        enemyData = data;

        // 스프라이트 설정
        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }

        // 스탯 초기화
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
    }

    private void InitializeStats()
    {
        if (enemyData == null) return;

        // 플레이어 레벨에 따른 체력 계산
        int playerLevel = GameManager.Instance.PlayerStats.Level;
        calculatedMaxHealth = Mathf.Min(
            enemyData.baseHealth * playerLevel,
            enemyData.maxPossibleHealth
        );
        currentHealth = calculatedMaxHealth;
        lastDamageTime = 0f;

        Debug.Log($"Enemy {enemyData.enemyName} initialized with health: {currentHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (!gameObject.activeSelf) return;

        currentHealth -= damage;

        // TODO: 피격 효과 추가

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!gameObject.activeSelf) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageDelay)
            {
                PlayerStats playerStats = collision.gameObject.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(enemyData.baseDamage);
                    lastDamageTime = Time.time;
                }
            }
        }
    }

    private void Die()
    {
        if (GameManager.Instance?.PlayerStats != null)
        {
            GameManager.Instance.PlayerStats.AddExperience(20f);
        }
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        // 컴포넌트 상태 초기화
        currentHealth = 0;
        lastDamageTime = 0;

        ObjectPool.Instance.ReturnToPool("Enemy", gameObject);
    }

    // 프로퍼티
    public float CurrentHealth => currentHealth;
    public float MaxHealth => calculatedMaxHealth;
    public float Damage => enemyData?.baseDamage ?? 0f;
    public float MoveSpeed => enemyData?.moveSpeed ?? 0f;
    public string EnemyName => enemyData?.enemyName ?? "Unknown Enemy";
}


