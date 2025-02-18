using UnityEngine;

public class MachinegunProjectile : BulletProjectile
{
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;
    private Vector2 knockbackForce;
    public override void Initialize(float damage, Vector2 direction, float speed,
       float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
       bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        sqrMaxTravelDistance = range * range;
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            knockbackForce.x = direction.x * knockbackPower;
            knockbackForce.y = direction.y * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        SpawnDestroyVFX();
        ReturnToPool();
    }

    protected override void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;

        if ((dx * dx + dy * dy) >= sqrMaxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            ApplyDamageAndEffects(enemy);
        }
    }
}