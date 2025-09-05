using System.Collections;
using UnityEngine;

/// <summary>
/// Ultrain (Machinegun X-Tier) 무기 메커니즘
/// 매우 빠른 Attack Delay와 넓은 Spread Angle을 사용한 단순한 연속 공격
/// X-Tier Basic Stats만 사용하여 복잡한 버스트 로직 대신 기본 발사 패턴 활용
/// </summary>
public class UltrainMechanism : WeaponMechanism
{
    // 캐싱된 변수들
    private Vector2 baseDirection;
    private Vector2 spreadDirection;
    private Vector3 spawnPosition;
    private float baseAngle;
    private float finalAngle;
    private float cosAngle;
    private float sinAngle;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
    }

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"Ultrain_Projectile";
        if (prefabToUse != null)
        {
            // 빠른 연사를 고려해 충분한 풀 생성
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 30);
        }
        else
        {
            Debug.LogError($"Ultrain projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        // 사운드 재생
        SoundManager.Instance?.PlaySound("Machinegun_atk", 1.2f, false);

        // 방향과 탄퍼짐 계산
        CalculateBaseDirection(target);
        CalculateSpreadDirection();
        
        // 투사체 발사
        FireUltrainProjectile(spreadDirection);
    }

    /// <summary>
    /// 타겟 방향 기본 방향 계산
    /// </summary>
    private void CalculateBaseDirection(Transform target)
    {
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
    }

    /// <summary>
    /// 탄퍼짐 방향 계산 (MachinegunMechanism과 동일한 방식)
    /// </summary>
    private void CalculateSpreadDirection()
    {
        float spreadAngle = weaponData.CurrentTierStats.spreadAngle;
        finalAngle = (baseAngle + Random.Range(-spreadAngle, spreadAngle)) * Mathf.Deg2Rad;

        cosAngle = Mathf.Cos(finalAngle);
        sinAngle = Mathf.Sin(finalAngle);

        spreadDirection.x = cosAngle;
        spreadDirection.y = sinAngle;
    }

    /// <summary>
    /// Ultrain 투사체 발사 (MachinegunMechanism 패턴 따름)
    /// </summary>
    private void FireUltrainProjectile(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out UltrainProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats), // Basic Stats의 knockback 사용
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                false, 0, 0f // 기본 관통 설정
            );
            // knockbackMultiplier 제거: Basic Stats의 knockback만 사용
        }
        else if (projectileObj != null && projectileObj.TryGetComponent(out MachinegunProjectile machinegunProjectile))
        {
            // UltrainProjectile이 없으면 MachinegunProjectile을 대체 사용
            machinegunProjectile.SetPoolTag(poolTag);
            machinegunProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats)
            );
        }
    }

    /// <summary>
    /// 무기가 장착 해제될 때 호출
    /// </summary>
    public override void OnWeaponUnequipped()
    {
        // Ultrain은 단순 발사 패턴이므로 별도 정리 불필요
    }
}