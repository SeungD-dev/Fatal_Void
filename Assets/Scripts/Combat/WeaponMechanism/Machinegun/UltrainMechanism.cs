using System.Collections;
using UnityEngine;

/// <summary>
/// Ultrain (Machinegun X-Tier) 무기 메커니즘
/// MachinegunMechanism과 동일한 패턴을 사용하되 버스트 발사 추가
/// </summary>
public class UltrainMechanism : WeaponMechanism
{
    // 캐싱된 변수들 (MachinegunMechanism과 동일)
    private Vector2 baseDirection;
    private Vector2 spreadDirection;
    private Vector3 spawnPosition;
    private float baseAngle;
    private float finalAngle;
    private float cosAngle;
    private float sinAngle;
    
    // 버스트 발사 관련
    private bool isFiring = false;
    private Coroutine burstCoroutine;

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
            // 버스트 발사를 고려해 더 많은 풀 생성
            int burstCount = Mathf.Max(1, (int)weaponData.CurrentTierStats.burstCount);
            int poolSize = Mathf.Max(30, burstCount * 5); 
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, poolSize);
        }
        else
        {
            Debug.LogError($"Ultrain projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null || isFiring) return;

        // 버스트 발사 시작
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
        }
        
        burstCoroutine = StartCoroutine(BurstFire(target));
    }

    /// <summary>
    /// 버스트 발사 코루틴
    /// </summary>
    private IEnumerator BurstFire(Transform target)
    {
        isFiring = true;
        
        int burstCount = Mathf.Max(1, (int)weaponData.CurrentTierStats.burstCount);
        float burstDelay = Mathf.Max(0.05f, weaponData.CurrentTierStats.burstDelay);

        // 사운드 재생 (버스트 시작 시 한 번만)
        SoundManager.Instance?.PlaySound("Machinegun_atk", 1.2f, false);

        for (int i = 0; i < burstCount; i++)
        {
            if (target == null) break;

            // MachinegunMechanism과 동일한 방식으로 방향과 탄퍼짐 계산
            CalculateBaseDirection(target);
            CalculateSpreadDirection();
            
            // 투사체 발사
            FireUltrainProjectile(spreadDirection);

            // 마지막 발사가 아니면 대기
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        isFiring = false;
        burstCoroutine = null;
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
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                false, 0, 0f, // 기본 관통 설정
                weaponData.CurrentTierStats.knockbackMultiplier // WeaponData에서 넉백 배율 가져오기
            );
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
    /// MonoBehaviour가 파괴될 때 실행 중인 코루틴 정리
    /// </summary>
    private void OnDestroy()
    {
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
    }
}