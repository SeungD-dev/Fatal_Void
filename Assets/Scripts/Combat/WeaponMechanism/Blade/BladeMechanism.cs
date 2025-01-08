using UnityEngine;

public class BladeMechanism : WeaponMechanism
{
    protected override void InitializeProjectilePool()
    {
        // 풀 태그 생성 및 설정
        poolTag = $"{weaponData.weaponType}Projectile";
        

        if (weaponData.projectilePrefab != null)
        {
            ObjectPool.Instance.CreatePool(poolTag, weaponData.projectilePrefab, 10);
        }      
    }

    protected override void Attack(Transform target)
    {
        if (target == null) return;

        SoundManager.Instance.PlaySound("Slash_sfx", 1f, false);

        // 풀 태그가 설정되어 있는지 확인
        if (string.IsNullOrEmpty(poolTag))
        {
            InitializeProjectilePool();
        }

        Vector2 direction = (target.position - playerTransform.position).normalized;
        //Debug.Log($"Attacking with pool tag: {poolTag}");  // 디버그 로그 추가

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        BladeProjectile projectile = projectileObj.GetComponent<BladeProjectile>();
        if (projectile != null)
        {
            float damage = weaponData.CalculateFinalDamage(playerStats);
            float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
            float projectileSpeed = weaponData.CurrentTierStats.projectileSpeed;
            float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
            float range = weaponData.CalculateFinalRange(playerStats);

            projectile.SetPoolTag(poolTag);
            projectile.Initialize(
                damage,
                direction,
                projectileSpeed,
                knockbackPower,
                range,
                projectileSize,
                true,
                0,
                0f
            );
        }
    }
}