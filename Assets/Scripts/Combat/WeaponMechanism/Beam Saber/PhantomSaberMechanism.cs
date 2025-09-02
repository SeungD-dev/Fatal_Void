using System.Collections;
using UnityEngine;

/// <summary>
/// Phantom Saber (Beam Saber X-Tier) 무기 메커니즘
/// BeamSaber와 동일한 연속공격 시스템을 사용하되 향상된 스탯과 플레이어 버프 제공
/// </summary>
public class PhantomSaberMechanism : WeaponMechanism
{
    private bool isSecondAttackReady;
    private const float SECOND_ATTACK_DELAY = 0.25f;
    private LayerMask enemyLayer;
    private MonoBehaviour ownerComponent;
    private float attackCooldown = 0f;
    private bool isComboAttack = false;
    private int comboCount = 0;
    private const int MAX_COMBO = 2;

    private GameObject ownerGameObject;
    private IEnumerator currentComboCoroutine;

    // 플레이어 스탯 버프 관련
    private bool isBuffApplied = false;

    protected override void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        // X-Tier 투사체 프리팹 사용
        GameObject prefabToUse = weaponData.xTierProjectilePrefab != null ? 
                                weaponData.xTierProjectilePrefab : 
                                weaponData.projectilePrefab;

        poolTag = $"PhantomSaber_Projectile";
        if (prefabToUse != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, prefabToUse, 10);
        }
        else
        {
            Debug.LogError($"Phantom Saber projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
        ownerComponent = player.GetComponent<MonoBehaviour>();

        if (ownerComponent != null)
        {
            ownerGameObject = ownerComponent.gameObject;
            StopComboIfActive();
        }
        else
        {
            Debug.LogError("Failed to get MonoBehaviour component from player!");
        }

        // 플레이어 스탯 버프 적용
        ApplyPlayerBuffs();
    }

    /// <summary>
    /// 플레이어 스탯 버프 적용
    /// </summary>
    private void ApplyPlayerBuffs()
    {
        if (playerStats == null || isBuffApplied) return;

        float powerBonus = weaponData.CurrentTierStats.phantomSaberPowerBonus;
        float hasteBonus = weaponData.CurrentTierStats.phantomSaberHasteBonus;
        float speedBonus = weaponData.CurrentTierStats.phantomSaberSpeedBonus;

        // 플레이어 스탯에 버프 적용
        playerStats.AddTemporaryPower(powerBonus);
        playerStats.AddTemporaryHaste(hasteBonus);
        playerStats.AddTemporaryMovementSpeed(speedBonus);

        isBuffApplied = true;
    }

    /// <summary>
    /// 플레이어 스탯 버프 제거
    /// </summary>
    private void RemovePlayerBuffs()
    {
        if (playerStats == null || !isBuffApplied) return;

        float powerBonus = weaponData.CurrentTierStats.phantomSaberPowerBonus;
        float hasteBonus = weaponData.CurrentTierStats.phantomSaberHasteBonus;
        float speedBonus = weaponData.CurrentTierStats.phantomSaberSpeedBonus;

        // 플레이어 스탯에서 버프 제거
        playerStats.RemoveTemporaryPower(powerBonus);
        playerStats.RemoveTemporaryHaste(hasteBonus);
        playerStats.RemoveTemporaryMovementSpeed(speedBonus);

        isBuffApplied = false;
    }

    private void StopComboIfActive()
    {
        if (isComboAttack && currentComboCoroutine != null)
        {
            ownerComponent.StopCoroutine(currentComboCoroutine);
            currentComboCoroutine = null;
            isComboAttack = false;
            comboCount = 0;
        }
    }

    public override void UpdateMechanism()
    {
        if (!ownerGameObject.activeInHierarchy) return;

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Attack(null);
            lastAttackTime = Time.time;

            // X-tier는 항상 연속공격 (BeamSaber의 Tier3+ 로직과 동일)
            if (!isComboAttack)
            {
                isComboAttack = true;
                comboCount = 1;
                currentComboCoroutine = ComboAttackSequence();
                ownerComponent.StartCoroutine(currentComboCoroutine);
            }
        }
    }

    private IEnumerator ComboAttackSequence()
    {
        WaitForSeconds waitDelay = new WaitForSeconds(SECOND_ATTACK_DELAY);

        while (comboCount < MAX_COMBO)
        {
            yield return waitDelay;
            if (!ownerGameObject.activeInHierarchy) break;

            SpawnCircularAttack();
            comboCount++;
        }

        attackCooldown = currentAttackDelay * 0.8f;
        isComboAttack = false;
        comboCount = 0;
        currentComboCoroutine = null;
    }

    protected override void Attack(Transform target)
    {
        if (ownerGameObject.activeInHierarchy)
        {
            SoundManager.Instance?.PlaySound("Slash_sfx", 1.2f, false);
            SpawnCircularAttack();
        }
        else
        {
            isComboAttack = false;
            comboCount = 0;
        }
    }

    private void SpawnCircularAttack()
    {
        if (ObjectPool.Instance == null || !ownerGameObject.activeInHierarchy) return;

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(poolTag, playerTransform.position, Quaternion.identity);
        if (projectileObj == null) return;

        if (projectileObj.TryGetComponent(out PhantomSaberProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);

            // Phantom Saber 전용 강화된 데미지 계산
            float baseDamage = weaponData.CalculateFinalDamage(playerStats);
            float enhancedDamage = baseDamage * weaponData.CurrentTierStats.phantomSaberDamageMultiplier;
            
            // 연속공격 시 추가 데미지 보너스
            if (isComboAttack && comboCount > 1)
            {
                enhancedDamage *= 1.2f;
            }

            // 강화된 공격 범위 계산
            float baseRange = weaponData.CalculateFinalRange(playerStats);
            float enhancedRange = baseRange * weaponData.CurrentTierStats.phantomSaberRangeMultiplier;

            projectile.Initialize(
                enhancedDamage,
                Vector2.zero,
                0f,
                weaponData.CalculateFinalKnockback(playerStats),
                enhancedRange,
                weaponData.CalculateFinalProjectileSize(playerStats)
            );

            projectile.SetupCircularAttack(
                enhancedRange,
                enemyLayer,
                playerTransform
            );
        }
        else if (projectileObj.TryGetComponent(out BeamSaberProjectile beamSaberProjectile))
        {
            // PhantomSaberProjectile이 없으면 BeamSaberProjectile을 대체 사용
            beamSaberProjectile.SetPoolTag(poolTag);

            float baseDamage = weaponData.CalculateFinalDamage(playerStats);
            float enhancedDamage = baseDamage * weaponData.CurrentTierStats.phantomSaberDamageMultiplier;
            
            if (isComboAttack && comboCount > 1)
            {
                enhancedDamage *= 1.2f;
            }

            float baseRange = weaponData.CalculateFinalRange(playerStats);
            float enhancedRange = baseRange * weaponData.CurrentTierStats.phantomSaberRangeMultiplier;

            beamSaberProjectile.Initialize(
                enhancedDamage,
                Vector2.zero,
                0f,
                weaponData.CalculateFinalKnockback(playerStats),
                enhancedRange,
                weaponData.CalculateFinalProjectileSize(playerStats)
            );

            beamSaberProjectile.SetupCircularAttack(
                enhancedRange,
                enemyLayer,
                playerTransform
            );
        }
    }

    /// <summary>
    /// 무기가 제거될 때 호출 (인벤토리에서 장착 해제, 판매 등)
    /// </summary>
    public void OnWeaponRemoved()
    {
        RemovePlayerBuffs();
        StopComboIfActive();
    }

    /// <summary>
    /// WeaponMechanism의 OnWeaponUnequipped 오버라이드
    /// </summary>
    public override void OnWeaponUnequipped()
    {
        OnWeaponRemoved();
    }

    /// <summary>
    /// 플레이어가 죽었을 때 호출
    /// </summary>
    public void OnPlayerDeath()
    {
        RemovePlayerBuffs();
        StopComboIfActive();
    }

    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        // 스탯 변경 시 버프를 다시 적용 (필요시)
    }

    private void OnDestroy()
    {
        // 메커니즘이 파괴될 때 버프 제거
        RemovePlayerBuffs();
        StopComboIfActive();
    }
}