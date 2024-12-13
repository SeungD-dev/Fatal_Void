using UnityEngine;

public abstract class BaseProjectile : MonoBehaviour, IPooledObject
{
    protected float damage;
    protected Vector2 direction;
    protected float speed;
    protected string poolTag;
    protected Vector2 startPosition;

    protected float knockbackPower;
    protected float maxTravelDistance;
    protected bool canPenetrate;
    protected int remainingPenetrations;
    protected float damageDecayRate;

    // AOE 관련 필드 추가
    protected float baseProjectileSize = 1f;
    protected float currentProjectileSize;

    protected Rigidbody2D rb;

    [SerializeField] private float rotationOffset;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Initialize(
     float damage,
     Vector2 direction,
     float speed,
     float knockbackPower = 0f,
     float range = 10f,
     float projectileSize = 1f,
     bool canPenetrate = false,
     int maxPenetrations = 0,
     float damageDecay = 0.1f)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        this.knockbackPower = knockbackPower;
        this.maxTravelDistance = range;
        this.canPenetrate = canPenetrate;
        this.remainingPenetrations = maxPenetrations;
        this.damageDecayRate = damageDecay;

        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        

        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        
        this.baseProjectileSize = projectileSize;
        UpdateProjectileSize();
    }

    // AOE 크기 업데이트
    protected virtual void UpdateProjectileSize()
    {
        currentProjectileSize = baseProjectileSize;
        transform.localScale = Vector3.one * currentProjectileSize;
    }

    // 투사체 크기 변경 메서드 (외부에서 AOE 변경 시 호출)
    public virtual void UpdateSize(float newSize)
    {
        baseProjectileSize = newSize;
        UpdateProjectileSize();
    }

    public void SetPoolTag(string tag)
    {
        poolTag = tag;
    }

    public virtual void OnObjectSpawn()
    {
        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    protected virtual void Update()
    {
        // 투사체 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 최대 사거리 체크
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            ReturnToPool();
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageAndEffects(enemy);
            }
        }
    }

    protected virtual void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            Debug.Log($"Projectile knockback power: {knockbackPower}, Direction: {direction}, Final force: {knockbackForce}"); // 디버그용
            enemy.ApplyKnockback(knockbackForce);
        }

        HandlePenetration();
    }

    protected virtual void HandlePenetration()
    {
        if (canPenetrate && (remainingPenetrations > 0 || remainingPenetrations == 0))
        {
            if (remainingPenetrations > 0)
            {
                remainingPenetrations--;
            }
            damage *= (1f - damageDecayRate);
        }
        else
        {
            ReturnToPool();
        }
    }

    protected virtual void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(poolTag))
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one; // 크기 초기화

            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Debug.LogWarning("Pool tag is not set. Destroying object instead.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        transform.localScale = Vector3.one; // 크기 초기화
    }
}