using UnityEngine;

public class GrinderMechanism : WeaponMechanism
{
    private string groundEffectPoolTag;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);

        groundEffectPoolTag = $"{weaponData.weaponType}GroundEffect";

        GrinderProjectile projectilePrefab = data.projectilePrefab.GetComponent<GrinderProjectile>();
        if (projectilePrefab != null && projectilePrefab.groundEffectPrefab != null)
        {
            GameObject prefab = projectilePrefab.groundEffectPrefab;
            prefab.tag = groundEffectPoolTag;  // 프리팹의 태그 설정

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

        Vector2 direction = (target.position - playerTransform.position).normalized;
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        GrinderProjectile projectile = projectileObj.GetComponent<GrinderProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
            float attackRadius = weaponData.CurrentTierStats.attackRadius *
                               (1f + playerStats.AreaOfEffect / 100f);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                target.position,
                attackRadius,
                weaponData.CurrentTierStats.groundEffectDuration,
                weaponData.CurrentTierStats.damageTickInterval,
                groundEffectPoolTag,
                projectileSize
            );
        }
    }
}