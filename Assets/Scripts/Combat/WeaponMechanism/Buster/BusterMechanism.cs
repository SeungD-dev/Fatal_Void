using UnityEngine;

public class BusterMechanism : WeaponMechanism
{
    private Vector2 targetDirection;
    private Vector2 spawnPosition;
    private bool penetrationEnabled;
    private TierStats.PenetrationInfo penetrationInfo;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        penetrationEnabled = data.currentTier >= 3;
        if (penetrationEnabled)
        {
            penetrationInfo = data.GetPenetrationInfo();
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        // 위치 및 방향 계산 최적화
        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;
        targetDirection.x = target.position.x - spawnPosition.x;
        targetDirection.y = target.position.y - spawnPosition.y;

        float magnitude = Mathf.Sqrt(targetDirection.x * targetDirection.x + targetDirection.y * targetDirection.y);
        if (magnitude > 0)
        {
            targetDirection.x /= magnitude;
            targetDirection.y /= magnitude;
        }

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out BusterProjectile projectile))
        {
            SoundManager.Instance.PlaySound("Burster_atk", 1f, false);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                penetrationEnabled,
                penetrationEnabled ? penetrationInfo.maxCount : 0,
                penetrationEnabled ? penetrationInfo.damageDecay : 0f
            );
        }
    }
}