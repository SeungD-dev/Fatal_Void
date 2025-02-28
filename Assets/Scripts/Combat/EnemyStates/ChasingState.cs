using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private Transform playerTransform;
    private readonly Rigidbody2D rb;
    private readonly Enemy enemyStats;
    private readonly SpriteRenderer spriteRenderer;

    // 재사용 가능한 벡터 변수들
    private Vector2 directionVector = Vector2.zero;
    private Vector2 targetPosition;
    private Vector2 currentPosition;

    // 성능 최적화를 위한 변수들
    private float moveSpeed;
    private float lastDirectionX;
    private const float DIRECTION_CHANGE_THRESHOLD = 0.05f;

    // 타이머 관련 변수
    private float nextSpriteFlipTime;
    private float spriteFlipInterval = 0.1f;  // 스프라이트 플립 업데이트 주기

    public ChasingState(EnemyAI enemyAI)
    {
        this.enemyAI = enemyAI;
        enemyTransform = enemyAI.transform;
        enemyStats = enemyAI.GetComponent<Enemy>();
        rb = enemyAI.GetComponent<Rigidbody2D>();
        spriteRenderer = enemyAI.spriteRenderer;

        // 플레이어 찾기
        playerTransform = enemyAI.PlayerTransform;
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    public void OnEnter()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // 초기화
        moveSpeed = enemyStats.MoveSpeed;
        nextSpriteFlipTime = Time.time;

        // 방향 초기 계산
        if (playerTransform != null)
        {
            CalculateDirection();
        }
    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();

        // 이동 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // 가벼운 로직과 시각적 업데이트만 Update에서 처리
    public void Update()
    {
        // 상태 유효성 검사
        if (enemyStats.IsKnockBack || playerTransform == null ||
            !IsGamePlaying()) return;

        // 방향 계산 (매 프레임)
        CalculateDirection();

        // 스프라이트 플립은 간격을 두고 업데이트
        if (Time.time >= nextSpriteFlipTime)
        {
            UpdateSpriteDirection();
            nextSpriteFlipTime = Time.time + spriteFlipInterval;
        }
    }

    // 물리 기반 이동은 FixedUpdate에서 처리
    public void FixedUpdate()
    {
        // 상태 유효성 검사
        if (enemyStats.IsKnockBack || playerTransform == null ||
            !IsGamePlaying()) return;

        // FixedUpdate에서는 이미 계산된 방향으로만 이동 수행
        ApplyMovement();
    }

    // 플레이어 방향으로의 벡터 계산
    private void CalculateDirection()
    {
        // 현재 위치와 대상 위치
        currentPosition = enemyTransform.position;
        targetPosition = playerTransform.position;

        // 방향 계산 (벡터 재사용)
        directionVector.x = targetPosition.x - currentPosition.x;
        directionVector.y = targetPosition.y - currentPosition.y;

        // 정규화 (벡터 연산 최적화)
        float sqrMagnitude = directionVector.x * directionVector.x + directionVector.y * directionVector.y;
        if (sqrMagnitude > 0.0001f) // 0으로 나누기 방지 + 최소 이동 임계값
        {
            float inverseMagnitude = 1.0f / Mathf.Sqrt(sqrMagnitude);
            directionVector.x *= inverseMagnitude;
            directionVector.y *= inverseMagnitude;
        }
    }

    // 스프라이트 방향 업데이트 (좌우 플립)
    private void UpdateSpriteDirection()
    {
        // 방향이 충분히 변경되었을 때만 스프라이트 플립 업데이트
        if (Mathf.Abs(directionVector.x - lastDirectionX) > DIRECTION_CHANGE_THRESHOLD)
        {
            lastDirectionX = directionVector.x;
            if (directionVector.x != 0)
            {
                spriteRenderer.flipX = directionVector.x < 0;
            }
        }
    }

    // 물리 이동 적용
    private void ApplyMovement()
    {
        // 커스텀 이동 속도 계산 (필요시 거리에 따른 속도 조절 가능)
        float appliedSpeed = moveSpeed;

        // 물리 기반 이동 수행
        if (rb != null)
        {
            // 리지드바디 이동 (Vector2 재사용으로 가비지 생성 최소화)
            rb.linearVelocity = new Vector2(
                directionVector.x * appliedSpeed,
                directionVector.y * appliedSpeed
            );
        }
        else
        {
            // Transform 기반 이동은 FixedDeltaTime 사용
            enemyTransform.position = new Vector3(
                enemyTransform.position.x + directionVector.x * appliedSpeed * Time.fixedDeltaTime,
                enemyTransform.position.y + directionVector.y * appliedSpeed * Time.fixedDeltaTime,
                enemyTransform.position.z
            );
        }
    }

    // 게임 상태 체크
    private bool IsGamePlaying()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.currentGameState == GameState.Playing;
    }
}