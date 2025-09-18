using UnityEngine;

public class CutterProjectile : BaseProjectile
{
    private bool isReturning = false;
    [SerializeField] private float rotationSpeed = 720f;
    private Vector2 currentPosition;
    private float sqrMaxTravelDistance;
    private float sqrMinReturnDistance;
    private readonly Quaternion rotationDelta = Quaternion.identity;
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
        // ȸ�� ����ȭ
        angleZ = (angleZ + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        // ���� ��ġ ���
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        float dx = currentPosition.x - startPosition.x;
        float dy = currentPosition.y - startPosition.y;
        float sqrDistance = dx * dx + dy * dy;

        // ���� ���⿡ ���� �ӵ� ���
        if (!isReturning)
        {
            if (sqrDistance >= sqrMaxTravelDistance)
            {
                isReturning = true;
            }
            else
            {
                float speedMultiplier = 1f - (sqrDistance / sqrMaxTravelDistance) * 0.8f; // 0.2f���� ����
                transform.Translate(direction * speed * speedMultiplier * Time.deltaTime, Space.World);
            }
        }
        else
        {
            if (sqrDistance <= sqrMinReturnDistance)
            {
                ReturnToPool();
            }
            else
            {
                float returnRatio = sqrDistance / sqrMaxTravelDistance;
                float speedMultiplier = 0.5f + (1f - returnRatio) * 1.5f; // 0.5f���� 2f�� ����
                transform.Translate(-direction * speed * speedMultiplier * Time.deltaTime, Space.World);
            }
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        // Cutter는 부메랑 무기이므로 적과 충돌해도 사라지지 않음
        // HandlePenetration() 호출하지 않아서 ReturnToPool()이 호출되지 않음
        // 오직 거리 기반 로직(Update)에서만 returnToPool 됨
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isReturning = false;
        angleZ = 0f;
    }
}