using UnityEngine;

public class BeamSaberProjectile : BaseProjectile
{
    private float attackRadius;
    private LayerMask enemyLayer;
    private Animator animator;
    private static readonly int BaseLayerIndex = 0;
    private SpriteRenderer spriteRenderer;
    private static int instanceCounter = 0;
    private int instanceId;
    private Transform playerTransform;

    private float elapsedTime = 0f;
    private bool attackExecuted = false;
    private const float FRAME_TIME = 1f / 60f;
    private const int ATTACK_START_FRAME = 10;
    private const int ATTACK_END_FRAME = 15;
    private const int TOTAL_FRAMES = 25;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        instanceId = ++instanceCounter;
    }

    public void SetupCircularAttack(float radius, LayerMask enemyMask, Transform player)
    {
        // 오브젝트가 비활성화 상태라면 활성화
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"BeamSaber #{instanceId} was inactive during SetupCircularAttack!");
            gameObject.SetActive(true);
        }

        attackRadius = radius;
        enemyLayer = enemyMask;
        playerTransform = player;

        // 공격 범위와 sprite 크기 동기화
        UpdateVisualSize();
    }

    private void UpdateVisualSize()
    {
        if (spriteRenderer == null || !spriteRenderer.sprite) return;

        // 스프라이트의 실제 크기 계산
        float spriteSize = spriteRenderer.sprite.bounds.size.x;

        if (spriteSize > 0)
        {
            // attackRadius를 직접 사용하지 않고 range 값을 그대로 사용
            transform.localScale = Vector3.one;  // 먼저 기본 스케일로 리셋

            // Range와 ProjectileSize가 1:1로 매칭되도록 스케일 설정
            float targetScale = (attackRadius * 2);  // Range가 실제 반지름이므로 지름으로 변환
            transform.localScale = Vector3.one * targetScale;
        }
    }
    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();

        if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"BeamSaber #{instanceId} was inactive during OnObjectSpawn!");
            gameObject.SetActive(true);
        }
        ResetState();
    }
    private void ResetState()
    {
        elapsedTime = 0f;
        attackExecuted = false;

        if (animator != null && gameObject.activeSelf)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Play("Beamsaber_Atk", BaseLayerIndex, 0f);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Debug.Log("BeamSaber OnEnable called");
        if (animator != null)
        {
            animator.enabled = true;
        }
    }
  

    protected override void Update()
    {
        if (playerTransform != null)
        {
            // 플레이어와 적의 충돌 여부 체크
            bool isPlayerCollidingWithEnemy = Physics2D.OverlapCircle(playerTransform.position, 0.1f, enemyLayer);
            if (isPlayerCollidingWithEnemy)
            {
                Debug.Log($"BeamSaber #{instanceId} - Player is colliding with enemy");
            }

            transform.position = playerTransform.position;
        }

        // 플레이어 위치 추적
        if (playerTransform != null)
        {
            transform.position = playerTransform.position;
        }

        elapsedTime += Time.deltaTime;
        int currentFrame = Mathf.FloorToInt(elapsedTime / FRAME_TIME);

        // 공격 프레임 체크
        if (!attackExecuted &&
            currentFrame >= ATTACK_START_FRAME &&
            currentFrame <= ATTACK_END_FRAME)
        {
            PerformCircularAttack();
            attackExecuted = true;
        }

        // 애니메이션 완료 체크
        if (currentFrame >= TOTAL_FRAMES)
        {
            ReturnToPool();
        }
    }
    private void PerformCircularAttack()
    {
        if (!gameObject.activeSelf) return;  // 비활성화 상태면 공격하지 않음

        // 범위 내 적 탐색
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            attackRadius,
            enemyLayer
        );

        // 적이 감지되었을 경우에만 데미지와 넉백 처리
        if (enemies.Length > 0)
        {
            foreach (Collider2D enemyCollider in enemies)
            {
                if (!gameObject.activeSelf) break;  // 도중에 비활성화되면 중단

                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && enemy.gameObject.activeSelf)  // 적이 활성화 상태일 때만
                {
                    enemy.TakeDamage(damage);

                    if (knockbackPower > 0)
                    {
                        Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                        enemy.ApplyKnockback(knockbackDirection * knockbackPower);
                    }
                }
            }
        }
    }
    protected override void OnDisable()
    {
        Debug.Log($"BeamSaber #{instanceId} OnDisable - Player position: {(playerTransform != null ? playerTransform.position.ToString() : "null")}");
        base.OnDisable();
        attackExecuted = false;
        elapsedTime = 0f;

        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    private void OnDrawGizmos()
    {// 공격 범위 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // 실제 스프라이트 크기 표시 (노란색)
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Gizmos.color = Color.yellow;
            float size = transform.localScale.x / 2f;  // 실제 표시되는 크기의 반지름
            Gizmos.DrawWireSphere(transform.position, size);
        }
    }

}