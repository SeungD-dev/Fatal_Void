using UnityEngine;

public class MachinegunMechanism : WeaponMechanism
{
    private const float SPREAD_ANGLE = 10f;

    // 캐싱용 변수들
    private Vector2 baseDirection;
    private Vector2 spreadDirection;
    private Vector3 spawnPosition;
    private float baseAngle;
    private float finalAngle;
    private float cosAngle;
    private float sinAngle;
    protected override void InitializeProjectilePool()
    {
        poolTag = $"{weaponData.weaponType}Projectile";
        if (weaponData.projectilePrefab != null)
        {
            int poolSize = Mathf.Max(20, (int)(1f / currentAttackDelay * 3f)); // 3초 분량
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, poolSize);
        }
        else
        {
            Debug.LogError($"Projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }
    protected override void Attack(Transform target)
    {
        if (target == null) return;

        SoundManager.Instance.PlaySound("Machinegun_atk", 1f, false);

        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;

        baseDirection.x = target.position.x - spawnPosition.x;
        baseDirection.y = target.position.y - spawnPosition.y;

        float magnitude = Mathf.Sqrt(baseDirection.x * baseDirection.x + baseDirection.y * baseDirection.y);
        if (magnitude > 0)
        {
            baseDirection.x /= magnitude;
            baseDirection.y /= magnitude;
        }

        baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        finalAngle = (baseAngle + Random.Range(-SPREAD_ANGLE, SPREAD_ANGLE)) * Mathf.Deg2Rad;

        cosAngle = Mathf.Cos(finalAngle);
        sinAngle = Mathf.Sin(finalAngle);

        spreadDirection.x = cosAngle;
        spreadDirection.y = sinAngle;

        FireProjectileWithSpread(spreadDirection);
    }


    private void FireProjectileWithSpread(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out MachinegunProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats)
            );
        }
    }  
}