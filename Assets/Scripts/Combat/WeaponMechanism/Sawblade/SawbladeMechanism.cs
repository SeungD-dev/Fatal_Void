using UnityEngine;

public class SawbladeMechanism : WeaponMechanism
{
    protected override void Attack(Transform target)
    {
        if (target == null) return;

        Vector2 direction = (target.position - playerTransform.position).normalized;

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        SawbladeProjectile projectile = projectileObj.GetComponent<SawbladeProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                true,    // 항상 관통
                0,       // 무제한 관통
                0f      // 데미지 감소 없음
            );
        }
    }
}