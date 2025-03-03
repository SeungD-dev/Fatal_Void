using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    public StateMachine stateMachine;
    protected Enemy enemyStats;
    protected Transform playerTransform;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    public Transform PlayerTransform => playerTransform;

    // 성능 최적화를 위한 변수들
    protected bool isActive;
    protected bool isCulled;  // 컬링 상태 추적
    protected Vector3 lastKnownPlayerPosition;

    // 거리 기반 업데이트 최적화
    [SerializeField] protected float distanceUpdateThreshold = 15f; // 플레이어와의 거리가 이 값보다 크면 업데이트 주기 늘림
    protected float sqrDistanceToPlayer;
    protected float sqrDistanceThreshold;

    // 움직임 제어 변수
    protected Vector2 moveDirection;
    protected float currentMoveSpeed;

    // 시각적 효과 관련 변수
    protected float effectUpdateInterval = 0.1f;  // 시각적 효과 업데이트 주기
    protected float nextEffectUpdateTime;

    protected virtual void Awake()
    {
        enemyStats = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new StateMachine();

        // 거리 임계값 제곱 (매번 제곱근 계산 회피)
        sqrDistanceThreshold = distanceUpdateThreshold * distanceUpdateThreshold;

        // 첫 효과 업데이트 시간 설정
        nextEffectUpdateTime = Time.time + Random.Range(0f, effectUpdateInterval);
    }

    protected virtual void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
        isActive = GameManager.Instance != null &&
                  GameManager.Instance.currentGameState == GameState.Playing;
        isCulled = false;

        // 상태 초기화
        InitializeStates();
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        isActive = (newState == GameState.Playing);
    }

    protected virtual void InitializeStates()
    {
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);

        stateMachine.SetState(idleState);
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => playerTransform != null && IsPlayerAlive() && isActive));
    }

    public virtual void Initialize(Transform target)
    {
        if (target == null) return;

        playerTransform = target;
        lastKnownPlayerPosition = playerTransform.position;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // 바로 추적 상태로 전환
        var chasingState = new ChasingState(this);
        stateMachine.SetState(chasingState);

        isActive = IsGamePlaying();
    }

    // 가벼운 로직과 시각적 효과만 Update에서 처리
    protected virtual void Update()
    {
        // 컬링되었거나 게임이 일시정지되었으면 처리하지 않음
        if (isCulled || !isActive || playerTransform == null) return;

        // 상태 머신 업데이트 (이동 관련 로직 제외)
        stateMachine.Update();

        // 시각적 효과 업데이트 (제한된 주기로)
        if (Time.time >= nextEffectUpdateTime)
        {
            UpdateVisualEffects();
            nextEffectUpdateTime = Time.time + effectUpdateInterval;
        }
    }

    // 물리 및 이동 로직은 FixedUpdate에서 처리
    protected virtual void FixedUpdate()
    {
        // 컬링되었거나 게임이 일시정지되었으면 처리하지 않음
        if (isCulled || !isActive || playerTransform == null) return;

        // 상태 머신 FixedUpdate 호출로 이동 로직 수행
        stateMachine.FixedUpdate();
    }

    // 컬링 상태 설정 (EnemyCullingManager에서 호출됨)
    public virtual void SetCullingState(bool isVisible)
    {
        isCulled = !isVisible;

        // 컴포넌트 활성화/비활성화
        enabled = isVisible;

        // Enemy 컴포넌트에 컬링 상태 전달
        if (enemyStats != null)
        {
            enemyStats.SetCullingState(isVisible);
        }
    }

    // 시각적 효과 업데이트 (바운스, 파티클 등)
    protected virtual void UpdateVisualEffects()
    {
        // 바운스 효과 업데이트
        if (enemyStats != null && !enemyStats.IsKnockBack)
        {
            enemyStats.UpdateBounceEffect();
        }
    }

    // 플레이어 상태 체크
    protected virtual bool IsPlayerAlive()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.PlayerStats != null &&
               GameManager.Instance.PlayerStats.CurrentHealth > 0;
    }

    // 게임 상태 체크
    protected virtual bool IsGamePlaying()
    {
        return GameManager.Instance != null &&
               GameManager.Instance.currentGameState == GameState.Playing;
    }

    // 디버그용 기즈모
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceUpdateThreshold);
    }
}