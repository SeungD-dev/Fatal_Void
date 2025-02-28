using UnityEngine;
using System.Collections.Generic;

public class ShotgunMechanism : WeaponMechanism
{
    private int currentProjectileCount;
    private float currentSpreadAngle;
    private const string DESTROY_VFX_TAG = "Bullet_DestroyVFX";

    // 순환 풀 시스템 - 2개의 풀만 사용하여 메모리 오버헤드 감소
    private const int POOL_COUNT = 2; // 3개에서 2개로 감소
    private string[] poolTags;
    private int currentPoolIndex = 0;

    // 캐싱용 변수들
    private Vector2 targetDirection = Vector2.zero;
    private Vector2 projectileDirection = Vector2.zero;
    private float baseAngle;
    private float angleStep;
    private float startAngle;
    private Vector3 spawnPosition = Vector3.zero;
    private Quaternion projectileRotation;

    // 성능 최적화를 위한 재사용 배열
    private GameObject[] projectileArray;

    // 디버그 플래그 (출시 빌드에서는 false로 설정)
    private const bool ENABLE_DEBUG_LOGS = false;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);

        // 순환 풀 태그 초기화
        poolTags = new string[POOL_COUNT];
        for (int i = 0; i < POOL_COUNT; i++)
        {
            poolTags[i] = $"{data.weaponType}_Projectile_{i}";
        }

        // 최대 필요 크기의 배열 한 번만 할당
        int maxProjectiles = GetMinimumProjectileCountForTier(4) + 1;
        projectileArray = new GameObject[maxProjectiles];

        // VFX 풀 초기화
        if (ObjectPool.Instance != null)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>("Prefabs/VFX/BulletDestroyVFX");
            if (vfxPrefab != null && !ObjectPool.Instance.DoesPoolExist(DESTROY_VFX_TAG))
            {
                ObjectPool.Instance.CreatePool(DESTROY_VFX_TAG, vfxPrefab, 30);
            }
        }

        UpdateWeaponStats();
        InitializeProjectilePools();
    }

    private void InitializeProjectilePools()
    {
        if (weaponData == null || weaponData.projectilePrefab == null) return;

        // 각 풀의 크기 계산 - 필요한 최소 양보다 약간 더 크게
        int maxTierCount = GetMinimumProjectileCountForTier(4);
        int poolSize = maxTierCount * 2; // 충분한 여유 확보

        for (int i = 0; i < POOL_COUNT; i++)
        {
            if (ObjectPool.Instance != null && !ObjectPool.Instance.DoesPoolExist(poolTags[i]))
            {
                ObjectPool.Instance.CreatePool(poolTags[i], weaponData.projectilePrefab, poolSize);             
            }
        }
    }

    protected override void InitializeProjectilePool()
    {
        // 별도의 초기화 메서드로 대체
    }

    protected override void UpdateWeaponStats()
    {
        base.UpdateWeaponStats();

        if (weaponData != null)
        {
            // 티어별 최소 투사체 수 적용
            int tier = weaponData.currentTier;
            currentProjectileCount = GetMinimumProjectileCountForTier(tier);
            currentSpreadAngle = weaponData.CurrentTierStats.spreadAngle;

            // 각도 계산
            if (currentProjectileCount > 1)
            {
                angleStep = currentSpreadAngle / (currentProjectileCount - 1);
            }
            else
            {
                angleStep = 0;
            }
        }
    }

    // 티어별 최소 투사체 수
    private int GetMinimumProjectileCountForTier(int tier)
    {
        switch (tier)
        {
            case 1: return 4;
            case 2: return 5;
            case 3: return 6;
            case 4: return 7;
            default: return 4;
        }
    }

    protected override void Attack(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        // 사운드 재생
        SoundManager.Instance?.PlaySound("Shotgun_sfx", 1f, false);

        // 다음 풀 인덱스로 순환
        currentPoolIndex = (currentPoolIndex + 1) % POOL_COUNT;
        string currentTag = poolTags[currentPoolIndex];

        // 방향 계산
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
        startAngle = baseAngle - (currentSpreadAngle * 0.5f);

        // 모든 투사체를 가져옴
        bool success = TryGetAllProjectiles(currentTag);

        if (!success)
        {
            // 실패 시 다른 풀 시도
            int alternateIndex = (currentPoolIndex + 1) % POOL_COUNT;
            currentTag = poolTags[alternateIndex];
            success = TryGetAllProjectiles(currentTag);

            if (!success)
            {
                // 두 번째 시도도 실패하면 풀 확장 후 재시도
                EnsurePoolCapacity(currentTag);
                success = TryGetAllProjectiles(currentTag);

                if (!success)
                {
                    return;
                }
            }
        }

        // 모든 투사체 초기화 및 발사
        for (int i = 0; i < currentProjectileCount; i++)
        {
            GameObject projectileObj = projectileArray[i];
            if (projectileObj == null) continue;

            // 각도 및 방향 계산
            float currentAngle = startAngle + (angleStep * i);
            float angleRad = currentAngle * Mathf.Deg2Rad;
            projectileDirection.x = Mathf.Cos(angleRad);
            projectileDirection.y = Mathf.Sin(angleRad);
            projectileRotation = Quaternion.Euler(0, 0, currentAngle);

            // 위치 및 회전 설정
            projectileObj.transform.position = spawnPosition;
            projectileObj.transform.rotation = projectileRotation;

            // 투사체 초기화
            if (projectileObj.TryGetComponent(out BaseProjectile projectile))
            {
                projectile.SetPoolTag(currentTag);
                projectile.Initialize(
                    weaponData.CalculateFinalDamage(playerStats),
                    projectileDirection,
                    weaponData.CurrentTierStats.projectileSpeed,
                    weaponData.CalculateFinalKnockback(playerStats),
                    currentRange,
                    weaponData.CalculateFinalProjectileSize(playerStats),
                    false, 0, 0f
                );
            }

            // 초기화 완료 후 활성화
            projectileObj.SetActive(true);
        }
    }

    // 모든 투사체를 가져오는 시도
    private bool TryGetAllProjectiles(string tag)
    {
        // 초기화
        for (int i = 0; i < currentProjectileCount; i++)
        {
            projectileArray[i] = null;
        }

        bool allSuccess = true;

        // 모든 투사체를 한 번에 비활성화 상태로 가져오기
        for (int i = 0; i < currentProjectileCount; i++)
        {
            GameObject proj = ObjectPool.Instance.SpawnFromPool(tag, spawnPosition, Quaternion.identity);

            if (proj != null)
            {
                proj.SetActive(false); // 초기화 전에 비활성화
                projectileArray[i] = proj;
            }
            else
            {
                allSuccess = false;
                break;
            }
        }

        // 실패 시 이미 가져온 것들을 풀에 반환
        if (!allSuccess)
        {
            for (int i = 0; i < currentProjectileCount; i++)
            {
                if (projectileArray[i] != null)
                {
                    ObjectPool.Instance.ReturnToPool(tag, projectileArray[i]);
                    projectileArray[i] = null;
                }
            }
        }

        return allSuccess;
    }

    // 풀 용량 확보
    private void EnsurePoolCapacity(string tag)
    {
        // 성능에 영향을 주지 않도록 최소한으로 확장
        int existingSize = 0;

        if (ObjectPool.Instance != null)
        {
            existingSize = ObjectPool.Instance.GetAvailableCount(tag);

            // 필요한 만큼만 추가 (필요량의 2배 + 여유분)
            int requiredSize = currentProjectileCount * 2 + 5;
            int additionalNeeded = requiredSize - existingSize;

            if (additionalNeeded > 0 && weaponData.projectilePrefab != null)
            {
                // 풀이 이미 있으면 확장
                if (ObjectPool.Instance.DoesPoolExist(tag))
                {
                    for (int i = 0; i < additionalNeeded; i++)
                    {
                        GameObject newObj = Object.Instantiate(weaponData.projectilePrefab);
                        newObj.SetActive(false);
                        ObjectPool.Instance.ReturnToPool(tag, newObj);
                    }
                }
                // 풀이 없으면 새로 생성
                else
                {
                    ObjectPool.Instance.CreatePool(tag, weaponData.projectilePrefab, requiredSize);
                }
            }
        }
    }

    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        UpdateWeaponStats();
    }
}