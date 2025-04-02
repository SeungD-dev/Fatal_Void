using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Brute : EnemyAI
{
    [Header("Charge Attack Settings")]
    [SerializeField] private float chargeDetectionRange = 8f; // 돌진 감지 범위
    [SerializeField] private float chargePrepareTime = 1.2f;  // 돌진 준비 시간
    [SerializeField] private float chargeSpeed = 15f;         // 돌진 속도
    [SerializeField] private float chargeDuration = 0.8f;     // 돌진 지속 시간
    [SerializeField] private float chargeCooldown = 5f;       // 돌진 쿨다운 시간
    [SerializeField] private Color chargeColor = new Color(0.56f, 0f, 0f); // #8f0000 색상
    [SerializeField] private bool isImmuneToKnockbackWhileCharging = true; // 돌진 중 넉백 면역 여부

    // 상태 추적 변수
    private bool isCharging = false;        // 돌진 중인지 여부
    private bool isPreparingCharge = false; // 돌진 준비 중인지 여부
    private float lastChargeTime = -10f;    // 마지막 돌진 시간

    // 캐시된 참조
    private Color originalColor;            // 원래 색상
    private Vector3 originalScale;          // 원래 크기
    private Vector2 chargeDirection;        // 돌진 방향
    private Sequence pulseSequence;         // DOTween 시퀀스
    private Rigidbody2D rb;                 // 캐시된 리지드바디
    private Animator animator;              // 애니메이터 컴포넌트

    // 애니메이션 파라미터 이름 (상수로 캐싱)
    private const string ANIM_CHARGE = "Brute_Charge";

    // 최적화된 변수
    private float sqrChargeDetectionRange;  // 제곱된 돌진 감지 범위 (최적화용)
    private readonly WaitForSeconds prepareWait; // 캐시된 대기 시간

    public Brute()
    {
        // 캐시된 WaitForSeconds 초기화 (최적화)
        prepareWait = new WaitForSeconds(chargePrepareTime);
    }

    protected override void Awake()
    {
        base.Awake();

        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // 색상과 크기 캐싱
        originalColor = spriteRenderer ? spriteRenderer.color : Color.white;
        originalScale = transform.localScale;

        // 성능을 위해 감지 범위 제곱값 미리 계산
        sqrChargeDetectionRange = chargeDetectionRange * chargeDetectionRange;
    }

    protected override void InitializeStates()
    {
        base.InitializeStates();

        // 상태 생성
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);
        var chargeState = new ChargeState(this);
        var prepareChargeState = new PrepareChargeState(this);

        // 상태 전환 설정
        stateMachine.SetState(idleState);

        // 대기 -> 추적: 플레이어가 감지되었을 때
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => playerTransform != null && IsPlayerAlive() && isActive));

        // 추적 -> 돌진 준비: 플레이어가 범위 내에 있고 쿨다운이 준비되었을 때
        stateMachine.AddTransition(chasingState, prepareChargeState,
            new FuncPredicate(() => CanStartCharge()));

        // 돌진 준비 -> 돌진: 준비가 완료되었을 때
        stateMachine.AddTransition(prepareChargeState, chargeState,
            new FuncPredicate(() => isPreparingCharge == false && !isCharging));

        // 돌진 -> 추적: 돌진이 완료되었을 때
        stateMachine.AddTransition(chargeState, chasingState,
            new FuncPredicate(() => isCharging == false));
    }

    protected override void Update()
    {
        base.Update();

        // 성능 최적화 - 활성 상태이고 이미 돌진/준비 중이 아닐 때만 돌진 조건 확인
        if (isActive && !isCulled && !isCharging && !isPreparingCharge &&
            Time.time > lastChargeTime + chargeCooldown && playerTransform != null)
        {
            CheckChargeConditions();
        }
    }

    private void CheckChargeConditions()
    {
        // 성능을 위해 제곱 거리 사용 (sqrt 연산 회피)
        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqrDistanceToPlayer = toPlayer.sqrMagnitude;

        // 플레이어가 돌진 범위 내에 있는지 확인
        if (sqrDistanceToPlayer <= sqrChargeDetectionRange)
        {
            // 돌진을 위한 정규화된 방향 저장
            chargeDirection = toPlayer.normalized;

            // 상태 머신을 통해 상태 전환 요청
            // 실제 전환은 InitializeStates에서 설정한 조건문을 통해 이루어짐
        }
    }

    public bool CanStartCharge()
    {
        return playerTransform != null &&
               !isCharging &&
               !isPreparingCharge &&
               Time.time > lastChargeTime + chargeCooldown &&
               (playerTransform.position - transform.position).sqrMagnitude <= sqrChargeDetectionRange;
    }

    public IEnumerator PrepareCharge()
    {
        if (isPreparingCharge || isCharging) yield break;

        isPreparingCharge = true;

        // 준비 시작 시 현재 플레이어 위치로의 방향 캐싱
        chargeDirection = (playerTransform.position - transform.position).normalized;

        // 시각적 피드백 - 색상 변경
        if (spriteRenderer != null)
        {
            spriteRenderer.color = chargeColor;
        }

        // 애니메이션 변경 - 돌진 준비/돌진 애니메이션으로 전환
        if (animator != null)
        {
            animator.Play(ANIM_CHARGE);
        }

        // 시각적 피드백 - DOTween을 사용한 크기 맥동 효과
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }

        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(transform.DOScale(originalScale * 1.2f, chargePrepareTime * 0.4f).SetEase(Ease.OutQuad));
        pulseSequence.Append(transform.DOScale(originalScale, chargePrepareTime * 0.6f).SetEase(Ease.InOutQuad));

        // 준비 시간 대기
        yield return prepareWait;

        // 준비 완료
        isPreparingCharge = false;
    }

    public IEnumerator PerformCharge()
    {
        if (isCharging) yield break;

        isCharging = true;
        lastChargeTime = Time.time;

        // 적 컴포넌트 접근
        Enemy enemyComponent = GetComponent<Enemy>();

        // 돌진 중에 넉백 면역 설정
        if (enemyComponent != null && isImmuneToKnockbackWhileCharging)
        {
            // 넉백 면역 상태 설정
            enemyComponent.SetKnockbackImmunity(true);
        }

        // 애니메이션은 이미 PrepareCharge에서 Brute_Charge로 변경됨

        if (rb != null)
        {
            // 물리 기반 이동 사용 (최적화됨)
            rb.linearVelocity = chargeDirection * chargeSpeed;

            // 돌진 지속 시간 동안 대기
            float endTime = Time.time + chargeDuration;
            while (Time.time < endTime && isActive)
            {
                yield return null;
            }

            // 이동 중지
            rb.linearVelocity = Vector2.zero;
        }

        // 원래 외관으로 복귀
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // 원래 크기로 확실히 복귀
        transform.localScale = originalScale;

        // 기본 애니메이션으로 복귀
        if (animator != null)
        {
            animator.Play("Brute");
        }

        // 넉백 면역 해제
        if (enemyComponent != null && isImmuneToKnockbackWhileCharging)
        {
            enemyComponent.SetKnockbackImmunity(false);
        }

        isCharging = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // 활성화된 트윈 종료
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
            pulseSequence = null;
        }

        // 시각적 상태 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        transform.localScale = originalScale;

        // 애니메이션 초기화
        if (animator != null)
        {
            animator.Play("Brute");
        }

        // 넉백 면역 상태 초기화
        if (isImmuneToKnockbackWhileCharging)
        {
            Enemy enemyComponent = GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.SetKnockbackImmunity(false);
            }
        }

        // 상태 플래그 초기화
        isCharging = false;
        isPreparingCharge = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // DOTween 시퀀스 정리
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }
    }

    #region State Implementations

    // Brute 전용 돌진 준비 상태
    public class PrepareChargeState : IState
    {
        private readonly Brute brute;
        private Coroutine prepareCoroutine;

        public PrepareChargeState(Brute brute)
        {
            this.brute = brute;
        }

        public void OnEnter()
        {
            // 돌진 준비 코루틴 시작
            prepareCoroutine = brute.StartCoroutine(brute.PrepareCharge());
        }

        public void OnExit()
        {
            // 코루틴이 여전히 실행 중이라면 중지
            if (prepareCoroutine != null)
            {
                brute.StopCoroutine(prepareCoroutine);
                prepareCoroutine = null;
            }
        }

        public void Update()
        {
            // 준비 중에는 업데이트가 필요 없음, 시간 기반으로 동작
        }

        public void FixedUpdate()
        {
            // 의도적으로 비워둠 - 준비 중에는 이동하지 않음
        }
    }

    // Brute 전용 돌진 상태
    public class ChargeState : IState
    {
        private readonly Brute brute;
        private Coroutine chargeCoroutine;

        public ChargeState(Brute brute)
        {
            this.brute = brute;
        }

        public void OnEnter()
        {
            // 돌진 코루틴 시작
            chargeCoroutine = brute.StartCoroutine(brute.PerformCharge());
        }

        public void OnExit()
        {
            // 코루틴이 여전히 실행 중이라면 중지
            if (chargeCoroutine != null)
            {
                brute.StopCoroutine(chargeCoroutine);
                chargeCoroutine = null;
            }
        }

        public void Update()
        {
            // 돌진 중에는 업데이트가 필요 없음, 코루틴에서 처리됨
        }

        public void FixedUpdate()
        {
            // Rigidbody2D.linearVelocity를 사용하므로 추가 물리 업데이트 필요 없음
        }
    }

    #endregion

    #region Debug Visualization

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // 돌진 감지 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeDetectionRange);
    }

    #endregion
}