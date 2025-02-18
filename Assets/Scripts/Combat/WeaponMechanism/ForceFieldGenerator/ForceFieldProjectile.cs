using UnityEngine;

public class ForceFieldProjectile : BaseProjectile
{
    private float tickInterval;
    private float lastTickTime;
    private Vector3 originalScale;
    private float actualRadius;  // Force Field Radius 값 저장용
    private Transform playerTransform;

    private  Vector2 currentPosition = Vector2.zero;
    private Vector2 targetPosition = Vector2.zero;
    private Vector2 knockbackDirection = Vector2.zero;

    // 충돌 검사용 캐싱
    private readonly Collider2D[] hitResults = new Collider2D[20];
    private ContactFilter2D contactFilter;
    private int enemyLayer;

    protected override void Awake()
    {
        base.Awake();
        enemyLayer = LayerMask.NameToLayer("Enemy");

        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Enemy"),
            useTriggers = true
        };
    }
    public void SetupForceField(float interval, Transform player, float radius)
    {
        tickInterval = interval;
        lastTickTime = Time.time;
        playerTransform = player;
        actualRadius = radius;
        UpdateVisualScale();
    }
    public void SetTickInterval(float interval)
    {
        tickInterval = interval;
        lastTickTime = Time.time;
    }

    public void SetPlayerTransform(Transform player)
    {
        this.playerTransform = player;
    }


    public void SetForceFieldRadius(float radius)
    {
        this.actualRadius = radius;
        UpdateVisualScale();
    }

    public override void Initialize(
        float damage,
        Vector2 direction,
        float speed,
        float knockbackPower = 0f,
        float range = 10f,
        float projectileSize = 1f,
        bool canPenetrate = false,
        int maxPenetrations = 0,
        float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize);
        // base.Initialize는 호출하되, range와 projectileSize는 실제로 사용하지 않음
    }

    private void UpdateVisualScale()
    {
        transform.localScale = Vector3.one * (actualRadius * 4);
    }
    protected override void Update()
    {
        if (playerTransform == null) return;

        transform.position = playerTransform.position;

        if (Time.time >= lastTickTime + tickInterval)
        {
            ApplyDamageInRange();
            lastTickTime = Time.time;
        }
    }

    private void ApplyDamageInRange()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        int hitCount = Physics2D.OverlapCircle(currentPosition, actualRadius, contactFilter, hitResults);

        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i].gameObject.layer == enemyLayer &&
                hitResults[i].TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(damage);

                if (knockbackPower > 0)
                {
                    targetPosition.x = enemy.transform.position.x;
                    targetPosition.y = enemy.transform.position.y;

                    knockbackDirection.x = targetPosition.x - currentPosition.x;
                    knockbackDirection.y = targetPosition.y - currentPosition.y;

                    float magnitude = Mathf.Sqrt(knockbackDirection.x * knockbackDirection.x +
                                               knockbackDirection.y * knockbackDirection.y);

                    if (magnitude > 0)
                    {
                        knockbackDirection.x = (knockbackDirection.x / magnitude) * knockbackPower;
                        knockbackDirection.y = (knockbackDirection.y / magnitude) * knockbackPower;
                        enemy.ApplyKnockback(knockbackDirection);
                    }
                }
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) { }
    protected override void HandlePenetration() { }
    protected override void ReturnToPool() { }

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying) return;

    //    // 실제 공격 범위와 시각적 범위 (이제 동일해야 함)
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, actualRadius);
    //}
}