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

        // 처형 조건 확인: 현재 체력이 10% 이하이거나, 일반 데미지 후 10% 이하가 될 경우
        if (enemyCurrentHealth <= executeThreshold)
        {
            // 즉시 처형 - 현재 체력만큼 추가 데미지로 확실히 사망
            float executeDamage = enemyCurrentHealth + 1f;
            enemy.TakeDamage(executeDamage);
            SpawnExecuteVFX(enemy.transform.position);
        }
        else
        {
            float healthAfterDamage = enemyCurrentHealth - damage;
            if (healthAfterDamage <= executeThreshold)
            {
                // 일반 데미지 후 처형 범위에 들어가는 경우
                enemy.TakeDamage(damage);
                float remainingHealth = enemy.CurrentHealth;
                if (remainingHealth > 0 && remainingHealth <= executeThreshold)
                {
                    enemy.TakeDamage(remainingHealth + 1f);
                    SpawnExecuteVFX(enemy.transform.position);
                }
            }
            else
            {
                // 일반 데미지만 적용
                enemy.TakeDamage(damage);
            }
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