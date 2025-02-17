using UnityEngine;

public class BusterProjectile : BulletProjectile
{
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;
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
            enemy.ApplyKnockback(direction * knockbackPower);
        }

        if (!canPenetrate)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }


    protected override void Update()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if ((dx * dx + dy * dy) >= sqrMaxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }
}