using UnityEngine;

/// <summary>
/// Black Hole (Grinder X-Tier) 무기 메커니즘
/// GrinderMechanism을 상속하여 X-Tier 전용 기능 제공
/// </summary>
public class BlackHoleMechanism : GrinderMechanism
{
    private string blackHoleGroundEffectPoolTag;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        
        // BlackHole 전용 Ground Effect 풀 태그
        blackHoleGroundEffectPoolTag = $"BlackHole_GroundEffect";

        // X-Tier 투사체 프리팹에서 BlackHole Ground Effect 프리팹 설정
        GameObject prefabToCheck = data.xTierProjectilePrefab != null ? 
                                  data.xTierProjectilePrefab : 
                                  data.projectilePrefab;

        if (prefabToCheck != null &&
            prefabToCheck.TryGetComponent(out BlackHoleProjectile blackHoleProjectilePrefab) &&
            blackHoleProjectilePrefab.groundEffectPrefab != null)
        {
            GameObject prefab = blackHoleProjectilePrefab.groundEffectPrefab;
            prefab.tag = blackHoleGroundEffectPoolTag;
            ObjectPool.Instance.CreatePool(blackHoleGroundEffectPoolTag, prefab, 8); // BlackHole은 더 강력하므로 풀 크기 증가
        }
        else
        {
            Debug.LogWarning($"BlackHole Ground Effect prefab is missing for weapon: {weaponData.weaponName}, falling back to regular Grinder effect");
        }
    }

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 우선 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"BlackHole_Projectile";
        if (prefabToUse != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 15); // BlackHole 전용 풀
        }
        else
        {
            Debug.LogError($"BlackHole projectile prefab is missing for weapon: {weaponData.weaponName}");
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

        // BlackHole 전용 사운드 재생
        SoundManager.Instance?.PlaySound("Grinder_sfx", 1.4f, false);

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out BlackHoleProjectile blackHoleProjectile))
        {
            calculatedAttackRadius = weaponData.CurrentTierStats.attackRadius *
                                   (1f + playerStats.AreaOfEffect / 100f);

            blackHoleProjectile.SetPoolTag(poolTag);
            blackHoleProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                target.position,
                calculatedAttackRadius,
                weaponData.CurrentTierStats.groundEffectDuration,
                weaponData.CurrentTierStats.damageTickInterval,
                blackHoleGroundEffectPoolTag, // BlackHole 전용 Ground Effect 풀 사용
                weaponData.CalculateFinalProjectileSize(playerStats),
                weaponData.CurrentTierStats.blackHoleScaleMultiplier,
                weaponData.CurrentTierStats.blackHoleDuration,
                weaponData.CurrentTierStats.blackHoleDamageInterval
            );
        }
        else if (projectileObj != null && projectileObj.TryGetComponent(out GrinderProjectile grinderProjectile))
        {
            // BlackHoleProjectile이 없으면 일반 GrinderProjectile을 대체 사용
            calculatedAttackRadius = weaponData.CurrentTierStats.attackRadius *
                                   (1f + playerStats.AreaOfEffect / 100f);

            grinderProjectile.SetPoolTag(poolTag);
            grinderProjectile.Initialize(
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