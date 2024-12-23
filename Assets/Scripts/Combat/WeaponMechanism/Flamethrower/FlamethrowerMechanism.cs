using UnityEngine;

public class FlamethrowerMechanism : WeaponMechanism
{
    private bool isFiring = false;
    private float firingDuration = 3f;
    private float currentFiringTime = 0f;
    private FlamethrowerProjectile currentProjectile;
    private Vector2 lastMoveDirection = Vector2.right;
    private Rigidbody2D playerRb;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            Debug.LogError("Rigidbody2D not found on player!");
        }
    }

    public override void UpdateMechanism()
    {
        if (isFiring)
        {
            currentFiringTime += Time.deltaTime;
            if (currentFiringTime >= firingDuration && weaponData.currentTier < 4)
            {
                StopFiring();
            }

            // �߻� ���� ���� ���� ������Ʈ
            if (currentProjectile != null)
            {
                UpdateFiringDirection();
            }
        }
        else if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            StartFiring();
        }
    }

    private void UpdateFiringDirection()
    {
        
        Vector2 currentVelocity = playerRb.linearVelocity;

        
        if (currentVelocity.sqrMagnitude > 0.1f)
        {
            lastMoveDirection = currentVelocity.normalized;
            Attack(null);
        }
    }

    private void StartFiring()
    {
        isFiring = true;
        currentFiringTime = 0f;
        lastAttackTime = Time.time;
        Attack(null);
    }

    private void StopFiring()
    {
        isFiring = false;
        if (currentProjectile != null)
        {
            currentProjectile.DeactivateProjectile();
            ObjectPool.Instance.ReturnToPool(poolTag, currentProjectile.gameObject);
            currentProjectile = null;
        }
    }

    protected override void Attack(Transform target)
    {
        float damage = weaponData.CalculateFinalDamage(playerStats);
        float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);

        if (currentProjectile != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, currentProjectile.gameObject);
        }

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        currentProjectile = projectileObj.GetComponent<FlamethrowerProjectile>();
        if (currentProjectile != null)
        {
            currentProjectile.Initialize(
                damage,
                lastMoveDirection,
                currentRange,
                knockbackPower
            );
        }
    }

    protected override void InitializeProjectilePool()
    {
        poolTag = $"{weaponData.weaponType}Projectile";
        if (weaponData.projectilePrefab != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 2);
        }
        else
        {
            Debug.LogError($"Projectile prefab is missing for weapon: {weaponData.weaponName}");
        }
    }

    private void OnDisable()
    {
        StopFiring();
    }
}