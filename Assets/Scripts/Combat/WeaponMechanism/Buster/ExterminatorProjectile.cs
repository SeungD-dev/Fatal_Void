using UnityEngine;

public class ExterminatorProjectile : BulletProjectile
{
    private const float EXECUTE_THRESHOLD = 0.1f; // 10% 체력 기준
    
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;

    public override void Initialize(float damage, Vector2 direction, float speed,
        float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
        bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        sqrMaxTravelDistance = range * range;
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        if (enemy == null) return;

        float enemyMaxHealth = enemy.MaxHealth;
        float enemyCurrentHealth = enemy.CurrentHealth;
        float executeThreshold = enemyMaxHealth * EXECUTE_THRESHOLD;

        // 일반 데미지 적용
        enemy.TakeDamage(damage);

        // 처형 조건 확인: 데미지 후 체력이 10% 이하가 되었거나, 이미 10% 이하인 적을 공격했을 때
        float healthAfterDamage = enemyCurrentHealth - damage;
        if (healthAfterDamage <= executeThreshold || enemyCurrentHealth <= executeThreshold)
        {
            // 즉시 처형 - 적의 현재 체력만큼 추가 데미지를 주어 확실히 사망시킴
            float executeDamage = enemy.CurrentHealth + 1f; // 현재 체력보다 약간 더 많은 데미지
            enemy.TakeDamage(executeDamage);
            
            // 처형 이펙트 (선택사항 - 나중에 VFX 추가 가능)
            SpawnExecuteVFX(enemy.transform.position);
        }

        // 넉백 적용
        if (knockbackPower > 0)
        {
            enemy.ApplyKnockback(direction * knockbackPower);
        }

        // 관통 처리
        if (!canPenetrate)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
        else
        {
            HandlePenetration();
        }
    }

    /// <summary>
    /// 처형 시 특수 VFX 생성 (현재는 기본 파괴 VFX 사용)
    /// 나중에 전용 처형 이펙트로 교체 가능
    /// </summary>
    private void SpawnExecuteVFX(Vector3 position)
    {
        // 현재는 기본 파괴 VFX 사용, 나중에 전용 처형 VFX로 교체
        SpawnDestroyVFX();
    }

    protected override void Update()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if ((dx * dx + dy * dy) >= sqrMaxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }
}