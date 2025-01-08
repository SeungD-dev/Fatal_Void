using UnityEngine;

public class CutterMechanism : WeaponMechanism
{
    protected override void Attack(Transform target)
    {
        if (target == null) return;

        SoundManager.Instance.PlaySound("Throw_sfx", 1f, false);

        Vector2 direction = (target.position - playerTransform.position).normalized;

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        CutterProjectile projectile = projectileObj.GetComponent<CutterProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
            var penetrationInfo = weaponData.GetPenetrationInfo();

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                penetrationInfo.canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
    }
}