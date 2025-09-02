using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plasma Sword (Blade X-Tier) 전용 투사체
/// BladeProjectile을 기반으로 하되 X-tier에 맞는 향상된 특성 제공
/// </summary>
public class PlasmaSwordProjectile : BaseProjectile
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
        // 동일한 적에게 중복 피해 방지
        if (!hitEnemies.Add(enemy)) return;

        // 기본 데미지 적용
        enemy.TakeDamage(damage);

        // 넉백 적용
        if (knockbackPower > 0)
        {
            enemy.ApplyKnockback(direction * knockbackPower);
        }

        // X-tier 특성: 관통 처리
        HandlePenetration();
    }

    protected override void Update()
    {
        // 현재 위치 업데이트
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        // 거리 계산 최적화
        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;
        float sqrDistance = dx * dx + dy * dy;

        // 투사체 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 최대 거리 체크
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

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            ApplyDamageAndEffects(enemy);
        }
    }
}