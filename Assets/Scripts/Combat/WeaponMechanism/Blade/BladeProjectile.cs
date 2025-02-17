using System.Collections.Generic;
using UnityEngine;

public class BladeProjectile : BaseProjectile
{
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>(8);
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;


    public override void Initialize(float damage, Vector2 direction, float speed,
        float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
        bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        sqrMaxTravelDistance = maxTravelDistance * maxTravelDistance;
    }


    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        startPosition = transform.position;
        hitEnemies.Clear();
    }


    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        if (!hitEnemies.Add(enemy)) return;

        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            enemy.ApplyKnockback(direction * knockbackPower);
        }

        HandlePenetration();
    }

    protected override void Update()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;
        float sqrDistance = dx * dx + dy * dy;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (sqrDistance >= sqrMaxTravelDistance)
        {
            ReturnToPool();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hitEnemies.Clear();
    }
}