using UnityEngine;

/// <summary>
/// Black Hole (Grinder X-Tier) 전용 Ground Effect
/// 시간에 따라 점진적으로 범위가 확장되는 특수 메커니즘
/// </summary>
public class BlackHoleGroundEffect : MonoBehaviour, IPooledObject
{
    private float damage;
    private float initialRadius;
    private float scaleMultiplier;
    private float duration;
    private float tickInterval;
    private float spawnTime;
    private float lastTickTime;
    private string poolTag;

    private SpriteRenderer spriteRenderer;

    // 확장 관련 캐싱 변수들
    private float currentRadius;
    private float currentScale;
    private float progress;
    private float elapsedTime;

    private readonly Collider2D[] hitResults = new Collider2D[30]; // BlackHole은 더 큰 범위이므로 배열 크기 증가
    private ContactFilter2D contactFilter;
    private Vector2 currentPosition;
    private int enemyLayer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyLayer = LayerMask.NameToLayer("Enemy");

        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Enemy"),
            useTriggers = true
        };
    }

    /// <summary>
    /// BlackHole GroundEffect 초기화
    /// </summary>
    /// <param name="damage">데미지</param>
    /// <param name="radius">초기 반지름</param>
    /// <param name="scaleMultiplier">최대 확장 배율 (1.5배 등)</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="tickInterval">데미지 주기</param>
    public void Initialize(float damage, float radius, float scaleMultiplier, float duration, float tickInterval)
    {
        this.damage = damage;
        this.initialRadius = radius;
        this.scaleMultiplier = scaleMultiplier;
        this.duration = duration;
        this.tickInterval = tickInterval;

        // 초기 스케일 설정
        currentScale = 1.0f;
        currentRadius = initialRadius;
        
        if (spriteRenderer != null)
        {
            transform.localScale = Vector3.one * (currentRadius * 2);
        }
    }

    public void SetPoolTag(string tag)
    {
        // BlackHole은 Grinder보다 더 강력한 사운드 사용
        SoundManager.Instance?.PlaySound("Grinder_sfx", 1.3f, false);
        this.poolTag = tag;
        gameObject.tag = tag;
    }

    public void OnObjectSpawn()
    {
        spawnTime = Time.time;
        lastTickTime = spawnTime;
        currentScale = 1.0f;
        currentRadius = initialRadius;
    }

    private void Update()
    {
        elapsedTime = Time.time - spawnTime;
        
        // 지속시간 체크
        if (elapsedTime >= duration)
        {
            if (!string.IsNullOrEmpty(poolTag))
            {
                ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
            }
            return;
        }

        // 점진적 확장 계산
        progress = elapsedTime / duration; // 0.0 ~ 1.0
        currentScale = 1.0f + (scaleMultiplier - 1.0f) * progress; // 1.0 ~ scaleMultiplier로 선형 확장
        currentRadius = initialRadius * currentScale;

        // 시각적 크기 업데이트
        if (spriteRenderer != null)
        {
            transform.localScale = Vector3.one * (currentRadius * 2);
        }

        // 데미지 틱 처리
        if (Time.time >= lastTickTime + tickInterval)
        {
            ApplyDamageToEnemiesInRange();
            lastTickTime = Time.time;
        }
    }

    private void ApplyDamageToEnemiesInRange()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        // 현재 확장된 범위로 적 탐지
        int hitCount = Physics2D.OverlapCircle(currentPosition, currentRadius, contactFilter, hitResults);

        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i].gameObject.layer == enemyLayer &&
                hitResults[i].TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnDisable()
    {
        spawnTime = 0f;
        lastTickTime = 0f;
        currentScale = 1.0f;
        currentRadius = initialRadius;
        progress = 0f;
        elapsedTime = 0f;
    }

    // 디버그용 - 에디터에서 현재 범위 시각화
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, initialRadius);
        }
    }
}