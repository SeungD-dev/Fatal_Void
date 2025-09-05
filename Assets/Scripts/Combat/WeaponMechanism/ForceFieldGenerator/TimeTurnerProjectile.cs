using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Time Turner (ForceFieldGenerator X-Tier) 전용 투사체
/// ForceFieldProjectile과 동일한 메커니즘이지만 넉백 대신 이동속도 감소 효과 적용
/// </summary>
public class TimeTurnerProjectile : BaseProjectile
{
    private float tickInterval;
    private float lastTickTime;
    private Vector3 originalScale;
    private float actualRadius;
    private Transform playerTransform;
    private float speedDebuffAmount;

    private Vector2 currentPosition = Vector2.zero;
    private Vector2 targetPosition = Vector2.zero;

    // 충돌 감지용 캐싱
    private readonly Collider2D[] hitResults = new Collider2D[20];
    private ContactFilter2D contactFilter;
    private int enemyLayer;

    // 속도 디버프를 받고 있는 적들을 추적 (범위에서 나갈 때 디버프 제거용)
    private readonly HashSet<Enemy> slowedEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> enemiesToRemove = new List<Enemy>(); // 범위 밖으로 나간 적들

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

    /// <summary>
    /// Time Turner Force Field 설정
    /// </summary>
    /// <param name="interval">공격 주기</param>
    /// <param name="player">플레이어 Transform</param>
    /// <param name="radius">공격 범위</param>
    /// <param name="speedDebuff">이동속도 감소율 (0.0~1.0)</param>
    public void SetupTimeTurner(float interval, Transform player, float radius, float speedDebuff)
    {
        tickInterval = interval;
        lastTickTime = Time.time;
        playerTransform = player;
        actualRadius = radius;
        speedDebuffAmount = speedDebuff;
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

    public void SetTimeTurnerRadius(float radius)
    {
        this.actualRadius = radius;
        UpdateVisualScale();
    }

    public void SetSpeedDebuff(float debuff)
    {
        speedDebuffAmount = debuff;
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
        // knockbackPower는 Time Turner에서 사용하지 않음 (속도 감소로 대체)
    }

    private void UpdateVisualScale()
    {
        // Time Turner는 ForceField보다 약간 다른 시각적 스케일 사용
        transform.localScale = Vector3.one * (actualRadius * 4.2f);
    }

    protected override void Update()
    {
        if (playerTransform == null) return;

        transform.position = playerTransform.position;

        if (Time.time >= lastTickTime + tickInterval)
        {
            ApplyDamageAndSpeedDebuff();
            lastTickTime = Time.time;
        }

        // 매 프레임 범위 밖으로 나간 적들의 디버프 제거
        CheckAndRemoveDebuffsOutOfRange();
    }

    /// <summary>
    /// 범위 내 적들에게 데미지와 속도 감소 효과 적용
    /// </summary>
    private void ApplyDamageAndSpeedDebuff()
    {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        int hitCount = Physics2D.OverlapCircle(currentPosition, actualRadius, contactFilter, hitResults);

        // 현재 범위 내 적들 처리
        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i].gameObject.layer == enemyLayer &&
                hitResults[i].TryGetComponent(out Enemy enemy))
            {
                // 데미지 적용
                enemy.TakeDamage(damage);

                // 속도 디버프 적용 (중첩되지 않음)
                if (!slowedEnemies.Contains(enemy))
                {
                    slowedEnemies.Add(enemy);
                }
                enemy.ApplySpeedDebuff(speedDebuffAmount);
            }
        }
    }

    /// <summary>
    /// 범위 밖으로 나간 적들의 속도 디버프 제거
    /// </summary>
    private void CheckAndRemoveDebuffsOutOfRange()
    {
        if (slowedEnemies.Count == 0) return;

        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.y;

        enemiesToRemove.Clear();

        foreach (Enemy enemy in slowedEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            // 적과의 거리 체크
            float sqrDistance = Vector2.SqrMagnitude((Vector2)enemy.transform.position - currentPosition);
            if (sqrDistance > actualRadius * actualRadius)
            {
                // 범위 밖으로 나간 경우 디버프 제거
                enemy.RemoveSpeedDebuff();
                enemiesToRemove.Add(enemy);
            }
        }

        // 범위 밖으로 나간 적들을 추적 목록에서 제거
        foreach (Enemy enemy in enemiesToRemove)
        {
            slowedEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// 투사체가 비활성화될 때 모든 디버프 제거
    /// </summary>
    private void OnDisable()
    {
        // 모든 디버프 제거
        foreach (Enemy enemy in slowedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.RemoveSpeedDebuff();
            }
        }
        slowedEnemies.Clear();
        enemiesToRemove.Clear();
    }

    protected override void OnTriggerEnter2D(Collider2D other) { }
    protected override void HandlePenetration() { }
    protected override void ReturnToPool() { }

    // 디버그용 - 에디터에서 현재 범위 시각화
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Time Turner는 파란색으로 표시 (ForceField와 구분)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
        
        // 디버프 받은 적들을 노란색으로 표시
        Gizmos.color = Color.yellow;
        foreach (Enemy enemy in slowedEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireCube(enemy.transform.position, Vector3.one * 0.3f);
            }
        }
    }
}