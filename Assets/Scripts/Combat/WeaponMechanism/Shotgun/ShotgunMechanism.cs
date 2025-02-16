using UnityEngine;

public class ShotgunMechanism : WeaponMechanism
{
    private int currentProjectileCount;
    private float currentSpreadAngle;
    private const string DESTROY_VFX_TAG = "Bullet_DestroyVFX";

    // 캐싱용 변수들
    private Vector2 targetDirection;
    private Vector2 projectileDirection;
    private float baseAngle;
    private float angleStep;
    private float startAngle;
    private float currentAngle;
    private Vector3 spawnPosition;
    private Quaternion projectileRotation;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);

        // VFX 풀 초기화
        if (ObjectPool.Instance != null)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>("Prefabs/BulletDestroyVFX");
            if (vfxPrefab != null)
            {
                ObjectPool.Instance.CreatePool(DESTROY_VFX_TAG, vfxPrefab, 10);
            }
        }

        targetDirection = Vector2.zero;
        projectileDirection = Vector2.zero;
        spawnPosition = Vector3.zero;
        UpdateWeaponStats(); // 초기 스탯 설정 호출
    }

    protected override void UpdateWeaponStats()
    {
        base.UpdateWeaponStats();
        currentProjectileCount = weaponData.CurrentTierStats.projectileCount;
        currentSpreadAngle = weaponData.CurrentTierStats.spreadAngle;
        angleStep = currentSpreadAngle / (currentProjectileCount - 1);
    }

    protected override void Attack(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        SoundManager.Instance.PlaySound("Shotgun_sfx", 1f, false);

        // 방향 계산 최적화
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

        // 부채꼴 형태로 투사체 발사
        for (int i = 0; i < currentProjectileCount; i++)
        {
            currentAngle = startAngle + (angleStep * i);
            FireShotgunProjectile(currentAngle);
        }
    }

    private void FireShotgunProjectile(float angle)
    {
        // 방향 계산 최적화
        float angleRad = angle * Mathf.Deg2Rad;
        projectileDirection.x = Mathf.Cos(angleRad);
        projectileDirection.y = Mathf.Sin(angleRad);

        projectileRotation = Quaternion.Euler(0, 0, angle);

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            spawnPosition,
            projectileRotation
        );

        if (projectileObj == null) return;

        if (projectileObj.TryGetComponent(out BaseProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);

            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                projectileDirection,
                weaponData.CurrentTierStats.projectileSpeed,
                weaponData.CalculateFinalKnockback(playerStats),
                currentRange,
                weaponData.CalculateFinalProjectileSize(playerStats),
                false,  // canPenetrate
                0,      // maxPenetrations
                0f      // damageDecay
            );
        }
    }
}