using UnityEngine;

/// <summary>
/// Infinity Disc (Sawblade X-Tier) 무기 메커니즘
/// 한 번만 발사하고 이후 무한히 튕기며 지속적으로 데미지를 주는 무기
/// </summary>
public class InfinityDiscMechanism : WeaponMechanism
{
    // 발사 상태
    private bool hasBeenFired = false;
    
    // 현재 활성화된 투사체 참조
    private InfinityDiscProjectile activeProjectile = null;
    
    // 캐싱된 변수들
    private Vector2 targetDirection;
    private Vector3 spawnPosition;

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"InfinityDisc_Projectile";
        if (prefabToUse != null)
        {
            // 1개만 사용하므로 작은 풀 생성
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 2);
        }
        else
        {
            Debug.LogError($"Infinity Disc projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        
        // 새로 장착될 때마다 발사 상태 초기화
        hasBeenFired = false;
        activeProjectile = null;
    }

    protected override void Attack(Transform target)
    {
        // 이미 발사했다면 더 이상 발사하지 않음
        if (hasBeenFired) return;
        
        if (target == null) return;

        SoundManager.Instance?.PlaySound("Throw_sfx", 1.2f, false);

        // 방향 계산
        CalculateDirection(target);

        // 투사체 발사
        FireInfinityDisc();
        
        // 발사 완료 플래그 설정
        hasBeenFired = true;
    }

    /// <summary>
    /// 타겟을 향한 방향 계산
    /// </summary>
    private void CalculateDirection(Transform target)
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
    }

    /// <summary>
    /// Infinity Disc 투사체 발사
    /// </summary>
    private void FireInfinityDisc()
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out InfinityDiscProjectile projectile))
        {
            activeProjectile = projectile;
            projectile.SetPoolTag(poolTag);
            projectile.SetParentMechanism(this); // 자신의 참조 전달
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                true, // 관통 가능
                0,    // 무제한 관통
                0f    // 데미지 감소 없음
            );
        }
        else if (projectileObj != null && projectileObj.TryGetComponent(out SawbladeProjectile sawbladeProjectile))
        {
            // InfinityDiscProjectile이 없으면 SawbladeProjectile을 대체 사용 (무한 튕기기는 불가능)
            sawbladeProjectile.SetPoolTag(poolTag);
            sawbladeProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                targetDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                true, // 관통 가능
                0,    // 무제한 관통
                0f    // 데미지 감소 없음
            );
        }
    }

    /// <summary>
    /// 무기가 제거될 때 호출 (인벤토리에서 장착 해제, 판매 등)
    /// </summary>
    public void OnWeaponRemoved()
    {
        DestroyActiveProjectile();
        hasBeenFired = false;
    }

    /// <summary>
    /// 플레이어가 죽었을 때 호출
    /// </summary>
    public void OnPlayerDeath()
    {
        DestroyActiveProjectile();
        hasBeenFired = false;
    }

    /// <summary>
    /// 활성화된 투사체 제거
    /// </summary>
    private void DestroyActiveProjectile()
    {
        if (activeProjectile != null)
        {
            activeProjectile.ForceReturn();
            activeProjectile = null;
        }
    }

    /// <summary>
    /// 투사체가 풀에 반환될 때 호출되는 콜백
    /// </summary>
    public void OnProjectileReturned()
    {
        activeProjectile = null;
        // 필요시 재발사 로직 추가 가능 (현재는 한 번만 발사)
    }

    protected override Transform FindNearestTarget()
    {
        // 이미 발사했다면 타겟을 찾을 필요 없음
        if (hasBeenFired) return null;
        
        return base.FindNearestTarget();
    }

    private void OnDestroy()
    {
        // 메커니즘이 파괴될 때 투사체도 제거
        DestroyActiveProjectile();
    }
}