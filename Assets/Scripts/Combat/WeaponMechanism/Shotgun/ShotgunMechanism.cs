using UnityEngine;

public class ShotgunMechanism : WeaponMechanism
{
    private int currentProjectileCount;
    private float currentSpreadAngle;

    protected override void UpdateWeaponStats()
    {
        base.UpdateWeaponStats();

        // 현재 티어의 샷건 스탯 업데이트
        currentProjectileCount = weaponData.CurrentTierStats.projectileCount;
        currentSpreadAngle = weaponData.CurrentTierStats.spreadAngle;
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        Vector2 direction = (target.position - playerTransform.position).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 부채꼴 형태로 투사체 발사
        float angleStep = currentSpreadAngle / (currentProjectileCount - 1);
        float startAngle = baseAngle - (currentSpreadAngle / 2);

        for (int i = 0; i < currentProjectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            FireShotgunProjectile(currentAngle);
        }
    }

    private void FireShotgunProjectile(float angle)
    {
        Vector2 direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.Euler(0, 0, angle)
        );

        BaseProjectile projectile = projectileObj.GetComponent<BaseProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);

            // 샷건은 관통하지 않으므로 관통 관련 매개변수는 false로 설정
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                false,  // canPenetrate
                0,      // maxPenetrations
                0f      // damageDecay
            );
        }
    }
}