using UnityEngine;

/// <summary>
/// Plasma Sword (Blade X-Tier) 무기 메커니즘
/// 십자(+) 패턴과 대각X 패턴을 번갈아가며 4방향 동시 발사
/// </summary>
public class PlasmaSwordMechanism : WeaponMechanism
{
    // 발사 패턴 enum
    private enum FirePattern
    {
        Cross,    // 십자: 상하좌우
        DiagonalX // 대각X: 대각선 4방향
    }

    // 현재 발사 패턴
    private FirePattern currentPattern = FirePattern.Cross;

    // 방향 벡터들 (캐싱)
    private readonly Vector2[] crossDirections = new Vector2[4]
    {
        Vector2.up,       // ↑
        Vector2.down,     // ↓
        Vector2.left,     // ←
        Vector2.right     // →
    };

    private readonly Vector2[] diagonalDirections = new Vector2[4]
    {
        new Vector2(-1, 1).normalized,  // ↖
        new Vector2(1, 1).normalized,   // ↗
        new Vector2(-1, -1).normalized, // ↙
        new Vector2(1, -1).normalized   // ↘
    };

    // 캐싱된 위치
    private Vector2 spawnPosition;

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"PlasmaSword_Projectile";
        if (prefabToUse != null)
        {
            // 4개씩 발사하므로 더 많은 풀 생성
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 20);
        }
        else
        {
            Debug.LogError($"Plasma Sword projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void Attack(Transform target)
    {
        // Plasma Sword는 타겟에 관계없이 고정 패턴으로 발사
        SoundManager.Instance?.PlaySound("Slash_sfx", 1.2f, false);

        // 스폰 위치 설정
        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;

        // 현재 패턴에 따라 발사
        Vector2[] directions = GetCurrentDirections();
        
        for (int i = 0; i < directions.Length; i++)
        {
            FirePlasmaSwordProjectile(directions[i]);
        }

        // 다음 공격을 위해 패턴 전환
        ToggleFirePattern();
    }

    /// <summary>
    /// 현재 패턴에 맞는 방향 배열 반환
    /// </summary>
    private Vector2[] GetCurrentDirections()
    {
        return currentPattern == FirePattern.Cross ? crossDirections : diagonalDirections;
    }

    /// <summary>
    /// 발사 패턴을 번갈아 전환
    /// </summary>
    private void ToggleFirePattern()
    {
        currentPattern = currentPattern == FirePattern.Cross ? 
                        FirePattern.DiagonalX : 
                        FirePattern.Cross;
    }

    /// <summary>
    /// Plasma Sword 투사체 발사
    /// </summary>
    private void FirePlasmaSwordProjectile(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out PlasmaSwordProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                true, // X-tier는 관통 가능
                0,    // 무제한 관통
                0.1f  // 관통시 10% 데미지 감소
            );
        }
        else if (projectileObj != null && projectileObj.TryGetComponent(out BladeProjectile bladeProjectile))
        {
            // PlasmaSwordProjectile이 없으면 BladeProjectile을 대체 사용
            bladeProjectile.SetPoolTag(poolTag);
            bladeProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                true, // X-tier는 관통 가능
                0,    // 무제한 관통
                0.1f  // 관통시 10% 데미지 감소
            );
        }
    }

    protected override Transform FindNearestTarget()
    {
        // Plasma Sword는 고정 패턴 발사이므로 타겟이 없어도 발사 가능
        // 하지만 기본 WeaponMechanism의 UpdateMechanism에서 타겟이 있을 때만 Attack을 호출하므로
        // 기본 타겟 찾기 로직을 사용
        return base.FindNearestTarget();
    }
}