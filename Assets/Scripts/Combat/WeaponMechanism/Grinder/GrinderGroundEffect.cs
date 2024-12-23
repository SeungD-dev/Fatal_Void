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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            if (string.IsNullOrEmpty(poolTag))
            {
                Debug.LogError($"Pool tag is empty or null! Current gameObject tag: {gameObject.tag}");
                gameObject.SetActive(false);
                return;
            }

            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }

    private void OnDisable()
    {
        spawnTime = 0f;
        lastTickTime = 0f;
    }
}