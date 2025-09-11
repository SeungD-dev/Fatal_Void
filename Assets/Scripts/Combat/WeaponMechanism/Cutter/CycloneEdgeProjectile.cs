using UnityEngine;

/// <summary>
/// Cyclone Edge (Cutter X-Tier) 전용 투사체
/// CutterProjectile과 동일한 부메랑 특성을 가지되 X-tier 성능 향상
/// </summary>
public class CycloneEdgeProjectile : BaseProjectile
{
    private bool isReturning = false;
    [SerializeField] private float rotationSpeed = 720f;
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;
    private float sqrMinReturnDistance;
    private float angleZ;

    public override void Initialize(float damage, Vector2 direction, float speed,
        float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
        bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        sqrMaxTravelDistance = range * range;
        sqrMinReturnDistance = 0.25f; // 0.5f * 0.5f
        isReturning = false;
    }

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        isReturning = false;
        angleZ = 0f;
    }

    protected override void Update()
    {
        // 회전 효과 (부메랑 특성)
        angleZ = (angleZ + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        // 현재 위치 계산
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;
        float sqrDistance = dx * dx + dy * dy;

        // 이동 단계에 따른 동작
        if (!isReturning)
        {
            // 나가는 단계: 최대 거리에 도달하면 돌아오기 시작
            if (sqrDistance >= sqrMaxTravelDistance)
            {
                isReturning = true;
            }
            else
            {
                // 거리에 따른 속도 감소 (멀어질수록 느려짐)
                float speedMultiplier = 1f - (sqrDistance / sqrMaxTravelDistance) * 0.8f; // 0.2f까지 감소
                transform.Translate(direction * speed * speedMultiplier * Time.deltaTime, Space.World);
            }
        }
        else
        {
            // 돌아오는 단계: 시작점 근처에 도달하면 풀에 반환
            if (sqrDistance <= sqrMinReturnDistance)
            {
                ReturnToPool();
            }
            else
            {
                // 돌아올 때 속도 (가까워질수록 빨라짐)
                float returnRatio = sqrDistance / sqrMaxTravelDistance;
                float speedMultiplier = 0.5f + (1f - returnRatio) * 1.5f; // 0.5f부터 2f까지 증가
                transform.Translate(-direction * speed * speedMultiplier * Time.deltaTime, Space.World);
            }
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        // 기본 데미지 적용
        enemy.TakeDamage(damage);

        // 넉백 적용
        if (knockbackPower > 0)
        {
            enemy.ApplyKnockback(direction * knockbackPower);
        }

        // 부메랑 특성: 관통 처리하지 않음 (계속 날아가야 함)
        // HandlePenetration()을 호출하지 않아서 풀로 돌아가지 않음
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            ApplyDamageAndEffects(enemy);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isReturning = false;
        angleZ = 0f;
    }
}