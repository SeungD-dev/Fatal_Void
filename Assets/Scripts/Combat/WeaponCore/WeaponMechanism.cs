using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class WeaponMechanism
{
    protected WeaponData weaponData;
    protected Transform playerTransform;
    protected PlayerStats playerStats;
    protected string poolTag;
    protected float lastAttackTime;
    protected float currentAttackDelay;
    protected float currentRange;
    protected float detectionRange;

    // 캐시용 변수들
    private static readonly List<Transform> tempEnemyList = new List<Transform>(20);
    protected Vector2 tempDirection;
    protected Vector2 playerPosition;
    protected Vector2 targetPosition;

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = data;
        playerTransform = player;
        playerStats = player.GetComponent<PlayerStats>();
        lastAttackTime = 0f;
        tempDirection = Vector2.zero;
        UpdateWeaponStats();
        InitializeProjectilePool();
    }

    protected virtual void UpdateWeaponStats()
    {
        if (weaponData == null || playerStats == null) return;
        currentAttackDelay = weaponData.CalculateFinalAttackDelay(playerStats);
        currentRange = weaponData.CalculateFinalRange(playerStats);
        detectionRange = currentRange + 1f;
    }

    protected virtual void InitializeProjectilePool()
    {
        if (weaponData == null) return;

        poolTag = $"{weaponData.weaponType}Projectile";
        if (weaponData.projectilePrefab != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 10);
        }
        else
        {
            Debug.LogError($"Projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    public virtual void UpdateMechanism()
    {
        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Transform target = FindNearestTarget();
            if (target != null)
            {
                Attack(target);
                lastAttackTime = Time.time;
            }
        }
    }

    protected virtual Transform FindNearestTarget()
    {
        // 임시 리스트 초기화
        tempEnemyList.Clear();

        // 이 부분은 Enemy Manager로 대체되어야 합니다
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        playerPosition.x = playerTransform.position.x;
        playerPosition.y = playerTransform.position.y;

        Transform nearestTarget = null;
        float nearestDistance = detectionRange * detectionRange;

        for (int i = 0; i < enemies.Length; i++)
        {
            Transform enemyTransform = enemies[i].transform;
            targetPosition.x = enemyTransform.position.x;
            targetPosition.y = enemyTransform.position.y;

            float sqrDistance = (targetPosition - playerPosition).sqrMagnitude;
            if (sqrDistance <= nearestDistance)
            {
                nearestDistance = sqrDistance;
                nearestTarget = enemyTransform;
            }
        }

        return nearestTarget;
    }

    protected abstract void Attack(Transform target);

    protected virtual void FireProjectile(Transform target)
    {
        if (target == null) return;

        playerPosition.x = playerTransform.position.x;
        playerPosition.y = playerTransform.position.y;
        targetPosition.x = target.position.x;
        targetPosition.y = target.position.y;

        tempDirection = targetPosition - playerPosition;
        float sqrMagnitude = tempDirection.sqrMagnitude;
        if (sqrMagnitude > 0)
        {
            float magnitude = Mathf.Sqrt(sqrMagnitude);
            tempDirection.x /= magnitude;
            tempDirection.y /= magnitude;
        }

        float angle = Mathf.Atan2(tempDirection.y, tempDirection.x) * Mathf.Rad2Deg;
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.Euler(0, 0, angle)
        );

        if (projectileObj.TryGetComponent(out BaseProjectile projectile))
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
            var penetrationInfo = weaponData.GetPenetrationInfo();

            projectile.Initialize(
                damage,
                tempDirection,
                projectileSpeed,
                knockbackPower,
                currentRange,
                projectileSize,
                penetrationInfo.canPenetrate,
                penetrationInfo.maxCount,
                penetrationInfo.damageDecay
            );
        }
    }

    public WeaponData GetWeaponData() => weaponData;

    public virtual void OnPlayerStatsChanged()
    {
        UpdateWeaponStats();
    }
}