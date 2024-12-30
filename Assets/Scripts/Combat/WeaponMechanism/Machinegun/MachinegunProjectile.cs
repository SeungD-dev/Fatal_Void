using UnityEngine;

public class MachinegunProjectile : BulletProjectile
{
    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);
        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }
        SpawnDestroyVFX();  // 소멸 효과 추가
        ReturnToPool();
    }

    protected override void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            SpawnDestroyVFX();  // 소멸 효과 추가
            ReturnToPool();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
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
}