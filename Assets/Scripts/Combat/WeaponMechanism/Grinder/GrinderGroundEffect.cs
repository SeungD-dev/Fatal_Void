using UnityEngine;

public class GrinderGroundEffect : MonoBehaviour, IPooledObject
{
    private float damage;
    private float radius;
    private float duration;
    private float tickInterval;
    private float spawnTime;
    private float lastTickTime;
    private string poolTag;

    private SpriteRenderer spriteRenderer;

    private readonly Collider2D[] hitResults = new Collider2D[20];
    private ContactFilter2D contactFilter;
    private Vector2 currentPosition;
    private int enemyLayer;


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyLayer = LayerMask.NameToLayer("Enemy");

        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Enemy"),
            useTriggers = true
        };
    }

    public void Initialize(float damage, float radius, float duration, float tickInterval)
    {
        this.damage = damage;
        this.radius = radius;
        this.duration = duration;
        this.tickInterval = tickInterval;

        if (spriteRenderer != null)
        {
            transform.localScale = Vector3.one * (radius * 2);
        }
    }

    public void SetPoolTag(string tag)
    {
        SoundManager.Instance.PlaySound("Grinder_sfx", 1f, false);
        this.poolTag = tag;
        gameObject.tag = tag;
    }

    public void OnObjectSpawn()
    {
        spawnTime = Time.time;
        lastTickTime = spawnTime;
 
    }
    private void Update()
    {
        float elapsedTime = Time.time - spawnTime;
        if (elapsedTime >= duration)
        {
            if (!string.IsNullOrEmpty(poolTag))
            {
                ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
            }
            return;
        }

        if (Time.time >= lastTickTime + tickInterval)
        {
            ApplyDamageToEnemiesInRange();
            lastTickTime = Time.time;
        }
    }
    private void ApplyDamageToEnemiesInRange()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        int hitCount = Physics2D.OverlapCircle(currentPosition, radius, contactFilter, hitResults);

        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i].gameObject.layer == enemyLayer &&
                hitResults[i].TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnDisable()
    {
        spawnTime = 0f;
        lastTickTime = 0f;
    }
}