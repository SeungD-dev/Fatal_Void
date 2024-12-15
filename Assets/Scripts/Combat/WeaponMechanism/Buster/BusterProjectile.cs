using UnityEngine;

public class BusterProjectile : BaseProjectile
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
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }
}