using UnityEngine;

/// <summary>
/// Cyclone Edge (Cutter X-Tier) 무기 메커니즘
/// 기존 Cutter와 동일하되 샷건처럼 3개 투사체를 부채꼴로 발사
/// 퍼짐 각도는 WeaponData에서 설정 가능
/// </summary>
public class CycloneEdgeMechanism : WeaponMechanism
{
    // 캐싱된 변수들
    private Vector2 targetDirection;
    private Vector2 spawnPosition;
    private float baseAngle;
    
    // 3개 투사체의 각도 오프셋 (중앙, 왼쪽, 오른쪽) - 초기화 시 설정됨
    private float[] angleOffsets = new float[3];

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // WeaponData에서 퍼짐 각도 가져와서 각도 오프셋 설정
        float spreadAngle = weaponData.CurrentTierStats.cutterSpreadAngle;
        angleOffsets[0] = 0f;           // 중앙
        angleOffsets[1] = -spreadAngle;  // 왼쪽
        angleOffsets[2] = spreadAngle;   // 오른쪽

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"CycloneEdge_Projectile";
        if (prefabToUse != null)
        {
            // 3개씩 발사하므로 더 많은 풀 생성
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 15);
        }
        else
        {
            Debug.LogError($"Cyclone Edge projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void UpdateWeaponStats()
    {
        base.UpdateWeaponStats();
        
        // WeaponData에서 퍼짐 각도 업데이트
        if (weaponData != null)
        {
            float spreadAngle = weaponData.CurrentTierStats.cutterSpreadAngle;
            angleOffsets[0] = 0f;           // 중앙
            angleOffsets[1] = -spreadAngle;  // 왼쪽
            angleOffsets[2] = spreadAngle;   // 오른쪽
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        SoundManager.Instance?.PlaySound("Throw_sfx", 1.2f, false);

        // 스폰 위치와 기본 방향 계산
        CalculateBaseDirection(target);

        // 3개 투사체를 부채꼴로 발사
        for (int i = 0; i < angleOffsets.Length; i++)
        {
            Vector2 spreadDirection = CalculateSpreadDirection(angleOffsets[i]);
            FireCycloneEdgeProjectile(spreadDirection);
        }
    }

    /// <summary>
    /// 타겟을 향한 기본 방향 계산
    /// </summary>
    private void CalculateBaseDirection(Transform target)
    {
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

        baseAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 퍼짐 각도를 적용한 방향 계산
    /// </summary>
    private Vector2 CalculateSpreadDirection(float angleOffset)
    {
        float finalAngle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
        
        return new Vector2(
            Mathf.Cos(finalAngle),
            Mathf.Sin(finalAngle)
        );
    }

    /// <summary>
    /// Cyclone Edge 투사체 발사
    /// </summary>
    private void FireCycloneEdgeProjectile(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out CycloneEdgeProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);
            var penetrationInfo = weaponData.GetPenetrationInfo();
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                penetrationInfo.canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
        else if (projectileObj != null && projectileObj.TryGetComponent(out CutterProjectile cutterProjectile))
        {
            // CycloneEdgeProjectile이 없으면 CutterProjectile을 대체 사용
            cutterProjectile.SetPoolTag(poolTag);
            var penetrationInfo = weaponData.GetPenetrationInfo();
            cutterProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                penetrationInfo.canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
    }
}