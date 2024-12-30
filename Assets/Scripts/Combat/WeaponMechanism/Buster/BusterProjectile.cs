using UnityEngine;

public class BusterProjectile : BulletProjectile
{
    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);
        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        // 3티어 이상일 경우 관통, 아닐 경우 즉시 제거
        if (!canPenetrate)
        {
            SpawnDestroyVFX();  // 소멸 효과 추가
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }

    protected override void Update()
    {
        base.Update();

        // 최대 사거리 도달 시 소멸 효과 추가
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }
}