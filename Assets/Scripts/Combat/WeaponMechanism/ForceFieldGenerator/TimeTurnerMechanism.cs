using UnityEngine;

/// <summary>
/// Time Turner (ForceFieldGenerator X-Tier) 무기 메커니즘
/// ForceFieldMechanism을 상속하여 X-Tier 전용 기능 제공
/// </summary>
public class TimeTurnerMechanism : ForceFieldMechanism
{
    private TimeTurnerProjectile currentTimeTurner;

    public override void Initialize(WeaponData data, Transform player)
    {
        // WeaponMechanism의 기본 초기화만 수행
        weaponData = data;
        playerTransform = player;
        playerStats = player.GetComponent<PlayerStats>();
        lastAttackTime = 0f;
        UpdateWeaponStats();
        InitializeProjectilePool();
        
        // Time Turner 생성 (ForceField 대신)
        CreateTimeTurner();
    }

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 우선 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        if (prefabToUse != null)
        {
            ObjectPool.Instance.CreatePool("TimeTurner_Projectile", prefabToUse, 1); // Time Turner는 1개만 필요
        }
        else
        {
            Debug.LogError($"Time Turner projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    /// <summary>
    /// Time Turner Force Field 생성
    /// </summary>
    private void CreateTimeTurner()
    {
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        if (prefabToUse == null)
        {
            Debug.LogError($"Time Turner prefab is missing for weapon: {weaponData.weaponName}");
            return;
        }

        Vector3 spawnPosition = new Vector3(playerTransform.position.x, playerTransform.position.y, 0);

        GameObject timeTurnerObj = ObjectPool.Instance.SpawnFromPool(
            "TimeTurner_Projectile",
            spawnPosition,
            Quaternion.identity
        );

        if (timeTurnerObj != null && timeTurnerObj.TryGetComponent(out TimeTurnerProjectile timeTurner))
        {
            currentTimeTurner = timeTurner;
            UpdateTimeTurnerStats();
        }
        else if (timeTurnerObj != null && timeTurnerObj.TryGetComponent(out ForceFieldProjectile forceField))
        {
            // TimeTurnerProjectile이 없으면 ForceFieldProjectile을 대체 사용 (속도 감소 기능은 없음)
            currentForceField = forceField;
            UpdateForceFieldStats();
        }
    }

    /// <summary>
    /// Time Turner 스탯 업데이트
    /// </summary>
    private void UpdateTimeTurnerStats()
    {
        if (currentTimeTurner == null) return;

        currentTimeTurner.Initialize(
            weaponData.CalculateFinalDamage(playerStats),
            Vector2.zero,
            0f,
            0f, // Time Turner는 넉백 사용 안함
            1f,
            1f
        );

        currentTimeTurner.SetupTimeTurner(
            weaponData.CurrentTierStats.timeTurnerTickInterval,
            playerTransform,
            weaponData.CurrentTierStats.timeTurnerRadius,
            weaponData.CurrentTierStats.timeTurnerSpeedDebuff
        );
    }

    /// <summary>
    /// ForceField 대체 사용시 스탯 업데이트 (기존 로직 유지)
    /// </summary>
    private void UpdateForceFieldStats()
    {
        if (currentForceField == null) return;

        currentForceField.Initialize(
            weaponData.CalculateFinalDamage(playerStats),
            Vector2.zero,
            0f,
            weaponData.CalculateFinalKnockback(playerStats),
            1f,
            1f
        );

        // Time Turner 설정을 ForceField에 적용 (가능한 부분만)
        currentForceField.SetupForceField(
            weaponData.CurrentTierStats.timeTurnerTickInterval,
            playerTransform,
            weaponData.CurrentTierStats.timeTurnerRadius
        );
    }

    public override void UpdateMechanism()
    {
        // Time Turner는 Update에서 자체적으로 관리
    }

    protected override void Attack(Transform target) 
    { 
        // Time Turner는 지속적인 공격이므로 별도 Attack 불필요
    }

    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        
        if (currentTimeTurner != null)
        {
            UpdateTimeTurnerStats();
        }
        else if (currentForceField != null)
        {
            UpdateForceFieldStats();
        }
    }

    /// <summary>
    /// Time Turner 정리 (WeaponManager의 CleanupWeaponMechanism에서 호출됨)
    /// </summary>
    public new void Cleanup()
    {
        if (currentTimeTurner != null && currentTimeTurner.gameObject != null)
        {
            ObjectPool.Instance?.ReturnToPool("TimeTurner_Projectile", currentTimeTurner.gameObject);
            currentTimeTurner = null;
        }
        
        // ForceField 대체 사용 중인 경우
        if (currentForceField != null && currentForceField.gameObject != null)
        {
            ObjectPool.Instance?.ReturnToPool("ForceField_Projectile", currentForceField.gameObject);
            currentForceField = null;
        }
    }

    public override void OnWeaponUnequipped()
    {
        Cleanup();
    }
}