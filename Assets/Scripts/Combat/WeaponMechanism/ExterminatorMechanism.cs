using UnityEngine;

/// <summary>
/// Exterminator (Buster X-Tier) 무기 메커니즘
/// 플레이어 이동 방향과 정반대 방향으로 2개의 투사체를 발사하며,
/// 무제한 관통과 처형 기능을 가진다.
/// </summary>
public class ExterminatorMechanism : WeaponMechanism
{
    private PlayerController playerController;
    private Vector2 forwardDirection;
    private Vector2 backwardDirection;
    private Vector2 spawnPosition;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        
        // PlayerController 참조 획득
        playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("Exterminator requires PlayerController component!");
        }
    }

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"Exterminator_Projectile";
        if (prefabToUse != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 15); // 2개씩 발사하므로 더 많은 풀 생성
        }
        else
        {
            Debug.LogError($"Exterminator projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null || playerController == null) return;

        // 플레이어의 현재 이동 방향 가져오기
        Vector2 movementDirection = GetPlayerMovementDirection();
        
        // 이동하지 않는 경우 타겟 방향으로 설정
        if (movementDirection.magnitude < 0.1f)
        {
            movementDirection = GetDirectionToTarget(target);
        }

        // 정방향과 역방향 계산
        forwardDirection = movementDirection.normalized;
        backwardDirection = -forwardDirection;

        // 스폰 위치 설정
        spawnPosition = playerTransform.position;

        // 정방향 투사체 발사
        FireExterminatorProjectile(forwardDirection);
        
        // 역방향 투사체 발사
        FireExterminatorProjectile(backwardDirection);

        // 사운드 재생
        SoundManager.Instance?.PlaySound("Burster_atk", 1.2f, false); // Exterminator는 약간 더 강한 사운드
    }

    /// <summary>
    /// PlayerController에서 이동 방향을 가져온다
    /// </summary>
    private Vector2 GetPlayerMovementDirection()
    {
        if (playerController == null) return Vector2.zero;

        // PlayerController의 movementVector 필드에 접근 (리플렉션 사용)
        var field = typeof(PlayerController).GetField("movementVector", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (Vector2)field.GetValue(playerController);
        }

        // 리플렉션 실패시 Rigidbody2D의 velocity 사용
        var rb = playerController.GetComponent<Rigidbody2D>();
        return rb != null ? rb.linearVelocity.normalized : Vector2.zero;
    }

    /// <summary>
    /// 플레이어에서 타겟으로의 방향을 계산한다
    /// </summary>
    private Vector2 GetDirectionToTarget(Transform target)
    {
        Vector2 direction = (Vector2)(target.position - playerTransform.position);
        return direction.normalized;
    }

    /// <summary>
    /// Exterminator 투사체를 지정된 방향으로 발사한다
    /// </summary>
    private void FireExterminatorProjectile(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out ExterminatorProjectile projectile))
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
        else if (projectileObj != null && projectileObj.TryGetComponent(out BusterProjectile busterProjectile))
        {
            // ExterminatorProjectile이 없으면 BusterProjectile을 대체 사용 (무제한 관통 설정)
            busterProjectile.SetPoolTag(poolTag);
            busterProjectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                direction,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                true, // 관통 활성화
                0,    // 무제한 관통
                0.1f  // 관통시 데미지 감소 10%
            );
        }
    }

    protected override Transform FindNearestTarget()
    {
        // 기본 타겟 찾기 로직 사용
        return base.FindNearestTarget();
    }
}