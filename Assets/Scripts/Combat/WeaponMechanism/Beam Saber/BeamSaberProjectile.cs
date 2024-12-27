using UnityEngine;

public class BeamSaberProjectile : BaseProjectile
{
    private enum AttackState
    {
        Ready,
        Attacking,
        Finished
    }

    private float attackRadius;
    private LayerMask enemyLayer;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private AttackState currentState = AttackState.Ready;
    private bool isAttackActive = false;
    private bool hasInitialized = false;
    private static readonly int BaseLayerIndex = 0;
    private static int instanceCounter = 0;
    private int instanceId;
    private Vector3 originalScale;
    private float baseScaleFactor = 1f;  // 기본 크기 비율 저장

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        instanceId = ++instanceCounter;
        originalScale = transform.localScale;
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
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);

        baseScaleFactor = projectileSize;  // WeaponData에서 설정한 크기 저장
        ResetState();
    }
    public void SetupCircularAttack(float radius, LayerMask enemyMask, Transform player)
    {
        attackRadius = radius;
        enemyLayer = enemyMask;
        playerTransform = player;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float spriteRadius = spriteRenderer.sprite.bounds.extents.x;
            if (spriteRadius > 0)
            {
                // Vector3.one을 기준으로 새로 계산
                float radiusScale = attackRadius / spriteRadius;
                transform.localScale = Vector3.one * baseScaleFactor * radiusScale;

                // 디버그 로그 추가
                Debug.Log($"BeamSaber #{instanceId} Scale - Base: {baseScaleFactor}, Radius: {radiusScale}, Final: {transform.localScale.x}");
            }
        }

        hasInitialized = true;
    }


    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        transform.localScale = Vector3.one;  // 스폰 시 크기 초기화
        ResetState();
    }


    private void ResetState()
    {
        hasInitialized = false;
        currentState = AttackState.Ready;
        isAttackActive = false;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Play("Beamsaber_Atk", BaseLayerIndex, 0f);
        }
    }

    // BaseProjectile의 OnTriggerEnter2D 비활성화
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // BeamSaber는 OverlapCircle로만 데미지를 처리
    }

    protected override void Update()
    {
        if (!hasInitialized || !gameObject.activeSelf) return;

        // 플레이어 위치 추적
        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            transform.position = playerTransform.position;

            // 공격 상태일 때만 충돌 체크
            if (currentState == AttackState.Attacking && isAttackActive)
            {
                PerformCircularAttack();
            }
        }
        else
        {
            ReturnToPool();
        }
    }

    private void PerformCircularAttack()
    {
        if (!isAttackActive || !gameObject.activeSelf) return;

        try
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(
                transform.position,
                attackRadius,
                enemyLayer
            );

            foreach (Collider2D enemyCollider in enemies)
            {
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    ApplyDamageAndEffects(enemy);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in PerformCircularAttack: {e.Message}");
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - transform.position)).normalized;
            enemy.ApplyKnockback(knockbackDirection * knockbackPower);
        }
    }

    public void OnAttackStart()
    {
        if (!hasInitialized || !gameObject.activeSelf) return;

        currentState = AttackState.Attacking;
        isAttackActive = true;
    }

    public void OnAttackEnd()
    {
        if (!hasInitialized) return;

        isAttackActive = false;
        currentState = AttackState.Finished;
    }

    public void OnAnimationComplete()
    {
        if (gameObject.activeSelf)
        {
            ReturnToPool();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (animator != null)
        {
            animator.enabled = true;
        }
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        transform.localScale = Vector3.one;  // 비활성화 시 크기 초기화
        hasInitialized = false;
        isAttackActive = false;
        currentState = AttackState.Ready;
    }

    protected override void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(poolTag))
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;  // 풀 반환 시 크기 초기화
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Debug.LogWarning("Pool tag is not set. Destroying object instead.");
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!hasInitialized) return;

        // 실제 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // 시각적 크기
        Gizmos.color = Color.yellow;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float visualRadius = spriteRenderer.sprite.bounds.extents.x * transform.localScale.x;
            Gizmos.DrawWireSphere(transform.position, visualRadius);
        }
    }
}