using UnityEngine;

public class AxeMechanism : WeaponMechanism
{
    private LayerMask enemyLayer;
    private float detectionRadius = 10f;

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
                FireAxe(nearestEnemy);
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

    private void FireAxe(Transform target)
    {
        Vector2 direction = (target.position - playerTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject axeObj = Object.Instantiate(weaponData.projectilePrefab,
            playerTransform.position,
            Quaternion.identity); // 회전은 AxeProjectile에서 처리

        AxeProjectile axe = axeObj.GetComponent<AxeProjectile>();
        axe.Initialize(weaponData.weaponDamage, direction, weaponData.projectileSpeed);
    }
}
