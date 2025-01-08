using UnityEngine;

public class BusterMechanism : WeaponMechanism
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

        SoundManager.Instance.PlaySound("Burster_atk", 1f, false);

        BusterProjectile projectile = projectileObj.GetComponent<BusterProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);

            // 3티어 이상일 때 관통 속성 부여
            bool canPenetrate = weaponData.currentTier >= 3;
            var penetrationInfo = canPenetrate ? weaponData.GetPenetrationInfo() : new TierStats.PenetrationInfo();

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
    }
}