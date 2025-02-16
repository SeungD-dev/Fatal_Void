using UnityEngine;

public class BeamSaberProjectile : BaseProjectile
{
    private enum AttackState : byte
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
    private AttackState currentState;
    private bool isAttackActive;
    private bool hasInitialized;
    private static readonly int BASE_LAYER_INDEX = 0;
    private Vector3 originalScale;
    private float baseScaleFactor = 1f;

    // 캐싱용 변수들
    private Vector2 currentPosition;
    private Transform cachedTransform;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;
        originalScale = cachedTransform.localScale;
    }

    public override void Initialize(float damage, Vector2 direction, float speed,
         float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
         bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);
        baseScaleFactor = projectileSize;
        ResetState();
    }
    public void SetupCircularAttack(float radius, LayerMask enemyMask, Transform player)
    {
        attackRadius = radius;
        enemyLayer = enemyMask;
        playerTransform = player;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            cachedTransform.localScale = Vector3.one * baseScaleFactor;
        }

        hasInitialized = true;
    }

    private void ResetToDefaultScale()
    {
        transform.localScale = Vector3.one * baseScaleFactor;
    }


    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        ResetToDefaultScale();
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
            animator.Play("Beamsaber_Atk", BASE_LAYER_INDEX, 0f);
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

        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            cachedTransform.position = playerTransform.position;

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

        currentPosition.x = cachedTransform.position.x;
        currentPosition.y = cachedTransform.position.y;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            currentPosition,
            attackRadius,
            enemyLayer
        );

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].TryGetComponent(out Enemy enemy) && enemy.gameObject.activeSelf)
            {
                ApplyDamageAndEffects(enemy);
            }
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            currentPosition.x = cachedTransform.position.x;
            currentPosition.y = cachedTransform.position.y;
            Vector2 enemyPos = enemy.transform.position;
            
            float dx = enemyPos.x - currentPosition.x;
            float dy = enemyPos.y - currentPosition.y;
            float magnitude = Mathf.Sqrt(dx * dx + dy * dy);
            
            if (magnitude > 0)
            {
                dx /= magnitude;
                dy /= magnitude;
                enemy.ApplyKnockback(new Vector2(dx, dy) * knockbackPower);
            }
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
        ResetToDefaultScale();
        hasInitialized = false;
        isAttackActive = false;
        currentState = AttackState.Ready;
    }


    protected override void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(poolTag))
        {
            transform.rotation = Quaternion.identity;
            ResetToDefaultScale();
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Debug.LogWarning("Pool tag is not set. Destroying object instead.");
            Destroy(gameObject);
        }
    }

    //private void OnDrawGizmos()
    //{
    //    if (!hasInitialized) return;

    //    // 실제 공격 범위
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, attackRadius);

    //    // 시각적 크기
    //    Gizmos.color = Color.yellow;
    //    if (spriteRenderer != null && spriteRenderer.sprite != null)
    //    {
    //        float visualRadius = spriteRenderer.sprite.bounds.extents.x * transform.localScale.x;
    //        Gizmos.DrawWireSphere(transform.position, visualRadius);
    //    }
    //}
}