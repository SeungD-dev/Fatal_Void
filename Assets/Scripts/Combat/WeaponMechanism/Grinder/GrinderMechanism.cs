using UnityEngine;

public class GrinderMechanism : WeaponMechanism
{
    private string groundEffectPoolTag;
    private Vector2 targetDirection;
    private Vector3 spawnPosition;
    private float calculatedAttackRadius;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        groundEffectPoolTag = $"{weaponData.weaponType}GroundEffect";

        if (data.projectilePrefab != null &&
            data.projectilePrefab.TryGetComponent(out GrinderProjectile projectilePrefab) &&
            projectilePrefab.groundEffectPrefab != null)
        {
            GameObject prefab = projectilePrefab.groundEffectPrefab;
            prefab.tag = groundEffectPoolTag;
            ObjectPool.Instance.CreatePool(groundEffectPoolTag, prefab, 5);
        }
        else
        {
            Debug.LogError($"Ground effect prefab is missing for weapon: {weaponData.weaponName}");
        }
    }
    protected override void Attack(Transform target)
    {
        if (target == null) return;

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

        if (projectileObj != null && projectileObj.TryGetComponent(out GrinderProjectile projectile))
        {
            calculatedAttackRadius = weaponData.CurrentTierStats.attackRadius *
                                   (1f + playerStats.AreaOfEffect / 100f);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                target.position,
                calculatedAttackRadius,
                weaponData.CurrentTierStats.groundEffectDuration,
                weaponData.CurrentTierStats.damageTickInterval,
                groundEffectPoolTag,
                weaponData.CalculateFinalProjectileSize(playerStats)
            );
        }
    }
}