using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class Wisp : EnemyAI
{
    [Header("투사체 공격 설정")]
    [SerializeField] private float projectileDetectionRange = 10f;      // 투사체 발사 감지 범위
    [SerializeField] private float projectilePrepareTime = 2.0f;        // 투사체 준비 시간
    [SerializeField] private float projectileSpeed = 8f;                // 투사체 속도
    [SerializeField] private float projectileCooldown = 4f;             // 투사체 쿨다운 시간
    [SerializeField] private int projectileCount = 3;                   // 발사할 투사체 수
    [SerializeField] private float projectileDelay = 0.3f;              // 투사체 간 발사 딜레이
    [SerializeField] private Color chargeColor = new Color(0.56f, 0f, 0f); // #8f0000 색상
    [SerializeField] private float floatHeight = 1.5f;                  // 투사체가 떠오르는 높이
    [SerializeField] private bool isImmuneToKnockbackWhilePreparing = true; // 준비 중 넉백 면역 여부

    // 상태 추적 변수
    private bool isPreparingProjectile = false;
    private bool isFiring = false;
    private float lastFireTime = -10f;

    // 최적화를 위한 캐싱된 참조
    private Color originalColor;
    private Sequence colorChangeSequence;
    private readonly WaitForSeconds projectilePreparationWait;
    private readonly WaitForSeconds betweenProjectilesWait;
    private float sqrProjectileDetectionRange;
    private WispProjectile[] projectileComponents; // 투사체 컴포넌트 캐시
    private Enemy enemyComponent; // 캐싱된 Enemy 컴포넌트

    // 투사체 풀 태그
    private const string PROJECTILE_POOL_TAG = "Wisp_Projectile";

    // 재사용 가능한 리스트
    private readonly List<GameObject> activeProjectiles = new List<GameObject>();

    public Wisp()
    {
        // 캐시된 WaitForSeconds 초기화 (최적화)
        projectilePreparationWait = new WaitForSeconds(projectilePrepareTime);
        betweenProjectilesWait = new WaitForSeconds(projectileDelay);
    }

    protected override void Awake()
    {
        base.Awake();

        // 초기 색상 저장
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        // 성능을 위해 미리 제곱 계산
        sqrProjectileDetectionRange = projectileDetectionRange * projectileDetectionRange;

        // 최대 동시 투사체 수에 맞게 배열 초기화
        projectileComponents = new WispProjectile[projectileCount];

        // Enemy 컴포넌트 캐싱
        enemyComponent = GetComponent<Enemy>();
    }

    protected override void InitializeStates()
    {
        base.InitializeStates();

        // 상태 생성 및 설정
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);
        var prepareProjectileState = new PrepareProjectileState(this);
        var firingState = new FiringState(this);

        // 상태 전환 설정
        stateMachine.SetState(idleState);

        // 대기 -> 추적: 플레이어가 감지되었을 때
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => playerTransform != null && IsPlayerAlive() && isActive));

        // 추적 -> 투사체 준비: 플레이어가 범위 내에 있고 쿨다운이 준비되었을 때
        stateMachine.AddTransition(chasingState, prepareProjectileState,
            new FuncPredicate(() => CanStartProjectileAttack()));

        // 투사체 준비 -> 발사: 준비가 완료되었을 때
        stateMachine.AddTransition(prepareProjectileState, firingState,
            new FuncPredicate(() => !isPreparingProjectile && !isFiring));

        // 발사 -> 추적: 발사가 완료되었을 때
        stateMachine.AddTransition(firingState, chasingState,
            new FuncPredicate(() => !isFiring));
    }

    protected override void Update()
    {
        base.Update();

        // 성능 최적화 - 활성 상태이고 이미 준비 중이거나 발사 중이 아닐 때만 조건 확인
        if (isActive && !isCulled && !isPreparingProjectile && !isFiring &&
            Time.time > lastFireTime + projectileCooldown && playerTransform != null)
        {
            CheckProjectileAttackConditions();
        }
    }

    private void CheckProjectileAttackConditions()
    {
        // 제곱 거리 사용으로 성능 최적화 (sqrt 연산 회피)
        Vector2 toPlayer = playerTransform.position - transform.position;
        float sqrDistanceToPlayer = toPlayer.sqrMagnitude;

        // 플레이어가 공격 범위 내에 있는지 확인
        if (sqrDistanceToPlayer <= sqrProjectileDetectionRange)
        {
            // 상태 머신을 통해 상태 전환 요청
            // 실제 전환은 InitializeStates에서 설정한 조건문을 통해 이루어짐
        }
    }

    public bool CanStartProjectileAttack()
    {
        return playerTransform != null &&
               !isPreparingProjectile &&
               !isFiring &&
               Time.time > lastFireTime + projectileCooldown &&
               (playerTransform.position - transform.position).sqrMagnitude <= sqrProjectileDetectionRange;
    }

    public IEnumerator PrepareProjectile()
    {
        if (isPreparingProjectile || isFiring) yield break;

        isPreparingProjectile = true;
        GameObject projectile = null;

        try
        {
            // 캐싱된 Enemy 컴포넌트를 사용하여 넉백 면역 설정
            if (enemyComponent != null && isImmuneToKnockbackWhilePreparing)
            {
                enemyComponent.SetKnockbackImmunity(true);
            }

            // 안구(Wisp의 피봇)에서 생성할 투사체 준비
            projectile = ObjectPool.Instance.SpawnFromPool(PROJECTILE_POOL_TAG, transform.position, Quaternion.identity);

            if (projectile == null)
            {
                Debug.LogError($"Wisp_Projectile 풀에서 투사체를 생성할 수 없습니다. 풀이 초기화되었는지 확인하세요.");
                isPreparingProjectile = false;

                // 넉백 면역 해제
                if (enemyComponent != null && isImmuneToKnockbackWhilePreparing)
                {
                    enemyComponent.SetKnockbackImmunity(false);
                }

                yield break;
            }

            // 중요: 태그 설정 - 투사체 정리 시 필요
            projectile.tag = "Projectile";

            // 투사체 컴포넌트 캐싱 (성능 최적화)
            SpriteRenderer projectileSpriteRenderer = null;
            WispProjectile wispProjectile = null;

            // 처음 생성된 투사체라면 컴포넌트 참조 캐싱
            if (projectileComponents[0] == null)
            {
                wispProjectile = projectile.GetComponent<WispProjectile>();
                projectileComponents[0] = wispProjectile;
                projectileSpriteRenderer = projectile.GetComponent<SpriteRenderer>();
            }
            else
            {
                // 이미 캐싱된 참조가 있다면 재사용
                wispProjectile = projectileComponents[0];
                projectileSpriteRenderer = wispProjectile != null ?
                    wispProjectile.GetSpriteRenderer() : projectile.GetComponent<SpriteRenderer>();
            }

            // 투사체에 소유자 설정 (투사체가 Wisp 비활성화를 감지할 수 있도록)
            if (wispProjectile != null)
            {
                wispProjectile.SetOwner(transform);
            }

            // 투사체 초기화 및 비활성화
            projectile.SetActive(false);

            // 투사체 위치 초기화 (Wisp의 피봇 위치)
            projectile.transform.position = transform.position;

            // 투사체 색상 설정 및 활성화
            if (projectileSpriteRenderer != null)
            {
                projectileSpriteRenderer.color = Color.white;
            }
            projectile.SetActive(true);

            // Wisp 색상 변경 애니메이션
            if (spriteRenderer != null)
            {
                if (colorChangeSequence != null)
                {
                    colorChangeSequence.Kill();
                }

                colorChangeSequence = DOTween.Sequence();
                colorChangeSequence.Append(
                    DOTween.To(() => spriteRenderer.color, x => spriteRenderer.color = x, chargeColor, projectilePrepareTime)
                    .SetEase(Ease.InQuad)
                );
            }

            // 투사체를 머리 위로 천천히 띄우는 애니메이션
            Vector3 targetPosition = transform.position + Vector3.up * floatHeight;
            projectile.transform.DOMove(targetPosition, projectilePrepareTime).SetEase(Ease.OutQuad);

            // 투사체 색상 변경 애니메이션
            if (projectileSpriteRenderer != null)
            {
                Sequence projectileColorSequence = DOTween.Sequence();
                projectileColorSequence.Append(
                    DOTween.To(() => projectileSpriteRenderer.color, x => projectileSpriteRenderer.color = x, chargeColor, projectilePrepareTime)
                    .SetEase(Ease.InQuad)
                );

                // 색상 깜빡임 효과 추가
                for (int i = 0; i < 3; i++)
                {
                    float startTime = projectilePrepareTime * 0.6f + (i * 0.3f);
                    float duration = 0.15f;

                    projectileColorSequence.Insert(startTime,
                        projectileSpriteRenderer.DOColor(Color.white, duration).SetLoops(2, LoopType.Yoyo));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during projectile preparation: {e.Message}");
            // 오류 발생 시 투사체 정리
            if (projectile != null && projectile.activeInHierarchy)
            {
                ObjectPool.Instance.ReturnToPool(PROJECTILE_POOL_TAG, projectile);
            }

            isPreparingProjectile = false;
            yield break;
        }

        // try-catch 블록 밖에서 yield return 사용
        yield return projectilePreparationWait;

        // 준비 완료 후 확인
        if (!gameObject.activeInHierarchy)
        {
            // Wisp가 비활성화된 경우 투사체도 풀로 반환
            if (projectile != null && projectile.activeInHierarchy)
            {
                ObjectPool.Instance.ReturnToPool(PROJECTILE_POOL_TAG, projectile);
            }
        }
        else if (projectile != null && projectile.activeInHierarchy)
        {
            // 준비된 투사체를 활성화 리스트에 추가
            activeProjectiles.Add(projectile);
        }

        // 준비 완료
        isPreparingProjectile = false;
    }
    // 투사체 발사 코루틴
    public IEnumerator FireProjectiles()
    {
        if (isFiring) yield break;

        isFiring = true;
        lastFireTime = Time.time;

        // 모든 투사체가 발사되었는지 추적하는 플래그
        bool allProjectilesLaunched = false;

        // 발사된 투사체 목록 추적 (발사 후 정리를 위해)
        List<GameObject> launchedProjectiles = new List<GameObject>();

        try
        {
            // 이미 준비된 투사체가 있는지 확인
            if (activeProjectiles.Count > 0)
            {
                // 첫 번째 투사체를 사용하여 추가 투사체 생성 및 발사
                GameObject firstProjectile = activeProjectiles[0];
                Vector3 startPosition = firstProjectile.transform.position;

                // 첫 번째 투사체 발사
                LaunchProjectile(firstProjectile, 0);
                launchedProjectiles.Add(firstProjectile);

                // 추가 투사체 생성 및 발사
                for (int i = 1; i < projectileCount; i++)
                {
                    yield return betweenProjectilesWait;

                    // 발사 중에 비활성화되었는지 확인
                    if (!gameObject.activeInHierarchy)
                    {
                        Debug.Log("Wisp was deactivated during projectile firing");
                        break;
                    }

                    GameObject projectile = ObjectPool.Instance.SpawnFromPool(PROJECTILE_POOL_TAG, startPosition, Quaternion.identity);
                    if (projectile != null)
                    {
                        // 투사체 컴포넌트 캐싱 및 색상 설정
                        if (i < projectileComponents.Length && projectileComponents[i] == null)
                        {
                            projectileComponents[i] = projectile.GetComponent<WispProjectile>();
                        }

                        // 캐싱된 컴포넌트 사용
                        WispProjectile wispProjectile = i < projectileComponents.Length ?
                            projectileComponents[i] : projectile.GetComponent<WispProjectile>();

                        if (wispProjectile != null)
                        {
                            wispProjectile.SetInitialColor(chargeColor);
                            // 투사체에 소유자 설정
                            wispProjectile.SetOwner(transform);
                        }

                        // 투사체 발사
                        LaunchProjectile(projectile, i);
                        launchedProjectiles.Add(projectile);
                    }
                }

                // 모든 투사체 발사 완료
                allProjectilesLaunched = true;
            }
            else
            {
                // 준비된 투사체가 없으면 바로 투사체 생성 및 발사
                for (int i = 0; i < projectileCount; i++)
                {
                    if (i > 0) yield return betweenProjectilesWait;

                    // 발사 중에 비활성화되었는지 확인
                    if (!gameObject.activeInHierarchy)
                    {
                        Debug.Log("Wisp was deactivated during projectile firing");
                        break;
                    }

                    GameObject projectile = ObjectPool.Instance.SpawnFromPool(PROJECTILE_POOL_TAG, transform.position, Quaternion.identity);
                    if (projectile != null)
                    {
                        // 태그 설정 (정리 목적)
                        projectile.tag = "Projectile";

                        // 투사체 컴포넌트 캐싱
                        if (i < projectileComponents.Length && projectileComponents[i] == null)
                        {
                            projectileComponents[i] = projectile.GetComponent<WispProjectile>();
                        }

                        WispProjectile wispProjectile = i < projectileComponents.Length ?
                            projectileComponents[i] : projectile.GetComponent<WispProjectile>();

                        if (wispProjectile != null)
                        {
                            // 캐싱된 컴포넌트 사용하여 색상 설정 및 깜빡임 효과 추가
                            wispProjectile.SetInitialColor(chargeColor);
                            wispProjectile.StartColorBlink(0.3f);
                            // 투사체에 소유자 설정
                            wispProjectile.SetOwner(transform);
                        }

                        // 투사체 발사
                        LaunchProjectile(projectile, i);
                        launchedProjectiles.Add(projectile);
                    }
                }

                // 모든 투사체 발사 완료
                allProjectilesLaunched = true;
            }

            // 중요: 투사체 발사 후 약간의 지연 추가
            // 이렇게 하면 투사체가 확실히 움직이기 시작했음을 보장함
            yield return new WaitForSeconds(0.1f);
        }
        finally
        {
            // Wisp 색상 원래대로 복원
            if (spriteRenderer != null)
            {
                spriteRenderer.DOColor(originalColor, 1f).SetEase(Ease.OutQuad);
            }

            // 넉백 면역 해제 (단, 모든 투사체가 성공적으로 발사된 경우에만)
            if (allProjectilesLaunched && enemyComponent != null && isImmuneToKnockbackWhilePreparing)
            {
                enemyComponent.SetKnockbackImmunity(false);
            }

            // 발사 상태 종료
            isFiring = false;

            // 활성 투사체 리스트 비우기
            activeProjectiles.Clear();

            // 발사된 투사체에 발사 완료 알림
            foreach (GameObject proj in launchedProjectiles)
            {
                WispProjectile wispProj = proj.GetComponent<WispProjectile>();
                if (wispProj != null)
                {
                    wispProj.ProjectileLaunched();
                }
            }
        }
    }

    // 투사체 발사 로직 - 인덱스 기반 캐싱 활용
    private void LaunchProjectile(GameObject projectile, int index)
    {
        if (projectile == null || playerTransform == null) return;

        // 플레이어 방향으로 벡터 계산
        Vector2 direction = (playerTransform.position - projectile.transform.position).normalized;

        // 투사체 컴포넌트 캐싱 활용
        WispProjectile wispProjectile = null;

        // 캐싱된 컴포넌트가 있는지 확인
        if (index < projectileComponents.Length && projectileComponents[index] != null)
        {
            wispProjectile = projectileComponents[index];
        }
        else if (index < projectileComponents.Length)
        {
            // 아직 캐싱되지 않았다면 캐싱
            wispProjectile = projectile.GetComponent<WispProjectile>();
            projectileComponents[index] = wispProjectile;
        }
        else
        {
            // 배열 범위를 벗어나면 직접 가져옴
            wispProjectile = projectile.GetComponent<WispProjectile>();
        }

        // 투사체에 소유자 설정 (투사체가 Wisp 비활성화를 감지할 수 있도록)
        if (wispProjectile != null)
        {
            wispProjectile.SetOwner(transform);

            // 투사체 발사
            wispProjectile.Launch(direction, projectileSpeed);
        }
        else
        {
            // 최후의 수단으로 Rigidbody2D 직접 접근
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // DOTween 시퀀스 정리
        if (colorChangeSequence != null)
        {
            colorChangeSequence.Kill();
            colorChangeSequence = null;
        }

        // 상태 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // 넉백 면역 상태 초기화 (캐싱된 컴포넌트 사용)
        if (isImmuneToKnockbackWhilePreparing && enemyComponent != null)
        {
            enemyComponent.SetKnockbackImmunity(false);
        }

        // 활성 투사체 모두 풀로 반환
        ClearAllProjectiles();

        // 상태 플래그 초기화
        isPreparingProjectile = false;
        isFiring = false;
    }
    private void ClearAllProjectiles()
    {
        // 활성 투사체 리스트에 있는 모든 투사체를 풀로 반환
        foreach (GameObject projectile in activeProjectiles)
        {
            if (projectile != null && projectile.activeInHierarchy)
            {
                ObjectPool.Instance.ReturnToPool(PROJECTILE_POOL_TAG, projectile);
            }
        }
        activeProjectiles.Clear();

        // Scene에서 준비 중이지만 아직 activeProjectiles에 추가되지 않은 투사체 찾기
        if (isPreparingProjectile)
        {
            // 현재 위치 주변에서 투사체 찾기
            Vector3 searchPosition = transform.position + Vector3.up * floatHeight;
            float searchRadius = 1.5f;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(searchPosition, searchRadius);
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject.CompareTag("Projectile"))
                {
                    WispProjectile projectile = collider.GetComponent<WispProjectile>();
                    if (projectile != null)
                    {
                        ObjectPool.Instance.ReturnToPool(PROJECTILE_POOL_TAG, collider.gameObject);
                    }
                }
            }
        }
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        // DOTween 시퀀스 정리
        if (colorChangeSequence != null)
        {
            colorChangeSequence.Kill();
        }

        // 활성 투사체 정리
        ClearAllProjectiles();
    }

    #region State Implementations

    // Wisp 전용 투사체 준비 상태
    public class PrepareProjectileState : IState
    {
        private readonly Wisp wisp;
        private Coroutine prepareCoroutine;

        public PrepareProjectileState(Wisp wisp)
        {
            this.wisp = wisp;
        }

        public void OnEnter()
        {
            // 투사체 준비 코루틴 시작
            prepareCoroutine = wisp.StartCoroutine(wisp.PrepareProjectile());
        }

        public void OnExit()
        {
            // 코루틴이 여전히 실행 중이라면 중지
            if (prepareCoroutine != null)
            {
                wisp.StopCoroutine(prepareCoroutine);
                prepareCoroutine = null;
            }
        }

        public void Update()
        {
            // 준비 단계에서는 추가 업데이트 필요 없음
        }

        public void FixedUpdate()
        {
            // 준비 단계에서는 이동하지 않음
        }
    }

    // Wisp 전용 투사체 발사 상태
    public class FiringState : IState
    {
        private readonly Wisp wisp;
        private Coroutine firingCoroutine;

        public FiringState(Wisp wisp)
        {
            this.wisp = wisp;
        }

        public void OnEnter()
        {
            // 투사체 발사 코루틴 시작
            firingCoroutine = wisp.StartCoroutine(wisp.FireProjectiles());
        }

        public void OnExit()
        {
            // 코루틴이 여전히 실행 중이라면 중지
            if (firingCoroutine != null)
            {
                wisp.StopCoroutine(firingCoroutine);
                firingCoroutine = null;
            }
        }

        public void Update()
        {
            // 발사 단계에서는 추가 업데이트 필요 없음
        }

        public void FixedUpdate()
        {
            // 발사 단계에서는 이동하지 않음
        }
    }

    #endregion

    #region Debug Visualization

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // 투사체 발사 감지 범위 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, projectileDetectionRange);

        // 피봇(안구) 위치 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);

        // 투사체 경로 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * floatHeight);
    }

    #endregion
}
