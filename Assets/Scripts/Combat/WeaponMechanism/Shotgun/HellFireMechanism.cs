using System.Collections;
using UnityEngine;

/// <summary>
/// HellFire (Shotgun X-Tier) 무기 메커니즘
/// Attack Delay마다 3연속 발사 (7발 → 5발 → 3발) 시스템
/// 각 발사 간에 설정된 간격을 두고 부채꼴 형태로 발사
/// </summary>
public class HellFireMechanism : WeaponMechanism
{
    // 3연발 시스템 관련
    private bool isFiring = false;
    private Coroutine burstCoroutine;
    private MonoBehaviour ownerComponent;

    // 캐싱된 변수들 (성능 최적화)
    private Vector2 baseDirection;
    private Vector3 spawnPosition;
    private float baseAngle;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        ownerComponent = player.GetComponent<MonoBehaviour>();
        
        if (ownerComponent == null)
        {
            Debug.LogError("HellFireMechanism requires MonoBehaviour component for coroutines!");
        }
    }

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"HellFire_Projectile";
        if (prefabToUse != null)
        {
            // 3연발 최대 투사체 수를 고려한 풀 크기 계산
            int maxShotsPerBurst = weaponData.CurrentTierStats.hellFireFirstShotCount + 
                                   weaponData.CurrentTierStats.hellFireSecondShotCount + 
                                   weaponData.CurrentTierStats.hellFireThirdShotCount;
            int poolSize = Mathf.Max(30, maxShotsPerBurst * 3);
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, poolSize);
        }
        else
        {
            Debug.LogError($"HellFire projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null || isFiring || ownerComponent == null) return;

        // 3연발 시스템 시작
        if (burstCoroutine != null)
        {
            ownerComponent.StopCoroutine(burstCoroutine);
        }
        
        burstCoroutine = ownerComponent.StartCoroutine(TripleBurstFire(target));
    }

    /// <summary>
    /// HellFire 3연발 코루틴 (빵빵빵)
    /// </summary>
    private IEnumerator TripleBurstFire(Transform target)
    {
        isFiring = true;

        // 기본 방향 계산
        CalculateBaseDirection(target);

        // 1번째 발사
        FireBurst(weaponData.CurrentTierStats.hellFireFirstShotCount, "1st Shot");
        
        // 1→2번째 간격 대기
        yield return new WaitForSeconds(weaponData.CurrentTierStats.hellFireFirstInterval);
        
        // 타겟이 여전히 유효한지 확인
        if (target != null)
        {
            CalculateBaseDirection(target);
        }

        // 2번째 발사
        FireBurst(weaponData.CurrentTierStats.hellFireSecondShotCount, "2nd Shot");
        
        // 2→3번째 간격 대기
        yield return new WaitForSeconds(weaponData.CurrentTierStats.hellFireSecondInterval);
        
        // 타겟이 여전히 유효한지 확인
        if (target != null)
        {
            CalculateBaseDirection(target);
        }

        // 3번째 발사
        FireBurst(weaponData.CurrentTierStats.hellFireThirdShotCount, "3rd Shot");

        // 발사 완료
        isFiring = false;
        burstCoroutine = null;
    }

    /// <summary>
    /// 기본 방향 계산 (타겟 방향)
    /// </summary>
    private void CalculateBaseDirection(Transform target)
    {
        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;
        spawnPosition.z = 0;

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
    /// 부채꼴 형태로 다수 투사체 발사
    /// </summary>
    private void FireBurst(int projectileCount, string shotName)
    {
        if (projectileCount <= 0) return;

        // 사운드 재생 (각 발사마다)
        SoundManager.Instance?.PlaySound("Shotgun_atk", 1.2f, false);

        float spreadAngle = weaponData.CurrentTierStats.spreadAngle;
        
        // 단일 투사체인 경우 중앙으로 발사
        if (projectileCount == 1)
        {
            FireSingleProjectile(baseDirection);
            return;
        }

        // 다수 투사체인 경우 부채꼴 형태로 발사
        float angleStep = spreadAngle * 2f / (projectileCount - 1);
        float startAngle = baseAngle - spreadAngle;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = (startAngle + (angleStep * i)) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
            
            FireSingleProjectile(direction);
        }

        Debug.Log($"HellFire {shotName}: {projectileCount} projectiles fired");
    }

    /// <summary>
    /// 단일 투사체 발사
    /// </summary>
    private void FireSingleProjectile(Vector2 direction)
    {
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            Quaternion.identity
        );

        if (projectileObj != null && projectileObj.TryGetComponent(out HellFireProjectile projectile))
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
        else if (projectileObj != null && projectileObj.TryGetComponent(out ShotgunProjectile shotgunProjectile))
        {
            // HellFireProjectile이 없으면 ShotgunProjectile을 대체 사용
            shotgunProjectile.SetPoolTag(poolTag);
            shotgunProjectile.Initialize(
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
    /// 무기가 장착 해제될 때 실행 중인 코루틴 정리
    /// </summary>
    public override void OnWeaponUnequipped()
    {
        if (burstCoroutine != null && ownerComponent != null)
        {
            ownerComponent.StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
        isFiring = false;
    }

    /// <summary>
    /// 스탯 변경 시 호출
    /// </summary>
    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        // HellFire는 X-Tier Basic Stats만 사용하므로 추가 처리 불필요
    }
}