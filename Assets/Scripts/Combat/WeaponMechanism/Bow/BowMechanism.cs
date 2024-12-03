using UnityEngine;

public class BowMechanism : WeaponMechanism
{
    private LayerMask enemyLayer;
    private float detectionRadius = 12f; // 활은 검보다 더 먼 거리에서 발사 가능

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    protected override void Attack()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(playerTransform.position, detectionRadius, enemyLayer);
        if (enemies.Length > 0)
        {
            Transform nearestEnemy = GetNearestEnemy(enemies);
            if (nearestEnemy != null)
            {
                FireArrow(nearestEnemy);
            }
        }
    }

    private Transform GetNearestEnemy(Collider2D[] enemies)
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (Collider2D enemy in enemies)
        {
            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void FireArrow(Transform target)
    {
        Vector2 direction = (target.position - playerTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject arrowObj = Object.Instantiate(weaponData.projectilePrefab,
            playerTransform.position,
            Quaternion.Euler(0, 0, angle));

        BowProjectile arrow = arrowObj.GetComponent<BowProjectile>();
        arrow.Initialize(weaponData.weaponDamage, direction, weaponData.projectileSpeed);
    }
}