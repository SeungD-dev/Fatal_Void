using System.Collections.Generic;
using UnityEngine;

public class BladeProjectile : BaseProjectile
{
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        // 스폰될 때마다 시작 위치 업데이트
        startPosition = transform.position;
        Debug.Log($"OnObjectSpawn - StartPosition set to: {startPosition}, MaxTravelDistance: {maxTravelDistance}");
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        // 이미 타격한 적은 무시
        if (hitEnemies.Contains(enemy)) return;

        // 새로운 적 타격
        hitEnemies.Add(enemy);
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        HandlePenetration();
    }

    protected override void Update()
    {
        float distanceFromStart = Vector2.Distance(startPosition, transform.position);

        // 매 프레임마다 값들을 확인
        Debug.Log($"Update - Current Position: {transform.position}, StartPosition: {startPosition}, " +
                  $"Distance: {distanceFromStart}, MaxDistance: {maxTravelDistance}, " +
                  $"Speed: {speed}, Direction: {direction}");

        // 투사체 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 최대 사거리 도달 시 풀로 반환
        if (distanceFromStart >= maxTravelDistance)
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