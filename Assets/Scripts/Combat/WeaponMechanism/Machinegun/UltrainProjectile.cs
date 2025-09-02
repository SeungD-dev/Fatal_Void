using UnityEngine;

/// <summary>
/// Ultrain (Machinegun X-Tier) 전용 투사체
/// 일반 Machinegun 투사체보다 향상된 시각적 효과와 성능을 제공
/// </summary>
public class UltrainProjectile : BulletProjectile
{
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;
    private Vector2 knockbackForce;
    private float knockbackMultiplier = 1.0f;
    
    public override void Initialize(float damage, Vector2 direction, float speed,
        float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
        bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        sqrMaxTravelDistance = range * range;
    }

    /// <summary>
    /// 넉백 배율 설정 (UltrainMechanism에서 호출)
    /// </summary>
    public void SetKnockbackMultiplier(float multiplier)
    {
        knockbackMultiplier = multiplier;
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        if (enemy == null) return;

        // 기본 데미지 적용
        enemy.TakeDamage(damage);

        // WeaponData에서 설정한 넉백 배율 적용
        if (knockbackPower > 0)
        {
            float finalKnockback = knockbackPower * knockbackMultiplier;
            knockbackForce.x = direction.x * finalKnockback;
            knockbackForce.y = direction.y * finalKnockback;
            enemy.ApplyKnockback(knockbackForce);
        }

        // 기본 파괴 효과 사용
        SpawnDestroyVFX();
        ReturnToPool();
    }

    protected override void Update()
    {
        // 투사체 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 현재 위치 업데이트
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        // 최대 거리 체크 (최적화된 거리 계산)
        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;

        if ((dx * dx + dy * dy) >= sqrMaxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            ApplyDamageAndEffects(enemy);
        }
    }
}