using UnityEngine;
using DG.Tweening;
using System.Collections;
public enum ProjectileState
{
    Preparing,  // 준비 중 (머리 위로 떠오르는 중)
    Launched,   // 발사됨 (플레이어 방향으로 날아가는 중)
    Destroyed   // 파괴됨 (풀로 반환)
}
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class WispProjectile : MonoBehaviour, IPooledObject
{
    [Header("투사체 설정")]
    [SerializeField] private float lifetime = 5f;       // 투사체 수명
    [SerializeField] private float damage = 10f;        // 투사체 데미지
    [SerializeField] private LayerMask targetLayers;    // 타겟 레이어 (플레이어)
    [SerializeField] private Sprite[] projectileSprites;
    [SerializeField] private float blinkInterval = 0.15f;
    private float nextBlinkTime;
    private int currentSpriteIndex = 0;

    //프로퍼티
    private ProjectileState currentState = ProjectileState.Preparing;

    // 상태 체크용 속성 추가
    public bool IsLaunched => currentState == ProjectileState.Launched;
    public bool IsPreparing => currentState == ProjectileState.Preparing;
    // 컴포넌트 캐싱
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Transform ownerTransform;
    // 캐싱된 컴포넌트 접근자 (외부에서 GetComponent 호출 없이 사용)
    public SpriteRenderer GetSpriteRenderer() => spriteRenderer;

    // 시각적 효과
    private Sequence colorSequence;
    private Color redColor = new Color(0.56f, 0f, 0f); // #8f0000 
    private Color whiteColor = Color.white;
    private Vector3 originalScale;

    // 이동 최적화를 위한 변수
    private Vector2 direction;
    private float speed;
    private bool isActive = false;

    // 풀링을 위한 변수
    private string poolTag = "Wisp_Projectile";
    private WaitForSeconds lifetimeWait;
    private Coroutine lifetimeCoroutine;

    private void Awake()
    {
        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        // 리지드바디 설정
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 충돌체 설정
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.3f;

        // 원본 크기 저장
        originalScale = transform.localScale;

        // 캐시된 WaitForSeconds
        lifetimeWait = new WaitForSeconds(lifetime);
    }
    public void SetOwner(Transform owner)
    {
        ownerTransform = owner;
    }
    private void Update()
    {
        // 투사체가 활성화된 상태이고 발사 상태인 경우에만 처리
        if (gameObject.activeInHierarchy && currentState == ProjectileState.Launched)
        {
            // 스프라이트 깜빡임 처리
            if (Time.time > nextBlinkTime)
            {
                // 스프라이트 인덱스 전환만 처리 - 색상은 DOTween에서 관리
                currentSpriteIndex = 1 - currentSpriteIndex;
                if (spriteRenderer != null && projectileSprites != null && projectileSprites.Length > 1)
                {
                    spriteRenderer.sprite = projectileSprites[currentSpriteIndex];
                }

                nextBlinkTime = Time.time + blinkInterval;
            }
        }

        // 준비 상태일 때 소유자 체크는 유지
        if (currentState == ProjectileState.Preparing && ownerTransform != null)
        {
            if (!ownerTransform.gameObject.activeInHierarchy)
            {
                ReturnToPool();
            }
        }
    }

    public void OnObjectSpawn()
    {
        // 상태 초기화
        currentState = ProjectileState.Preparing;

        // 투사체 활성화 시 초기화
        isActive = false; // 발사되기 전까지는 false
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 수명 코루틴 시작
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
    }

    // 투사체 발사 메서드
    public void Launch(Vector2 direction, float speed)
    {
        // 상태 초기화 및 방향 설정
        this.direction = direction.normalized;
        this.speed = speed;

        // 항상 Launched로 상태 설정
        currentState = ProjectileState.Launched;
        isActive = true;

        // 캐싱된 rb 변수 사용
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearVelocity = direction * speed;

            Debug.Log($"투사체 발사 - ID: {GetInstanceID()}, 속도: {rb.linearVelocity}, 방향: {direction}");
        }

        // 중요: Lifetime 코루틴을 명시적으로 재시작
        RestartLifetimeCountdown();

        // 색상 깜빡임 효과 시작
        StartColorBlink(blinkInterval);
    }
    private void RestartLifetimeCountdown()
    {
        // 기존 코루틴 중지
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }

        // 새 코루틴 시작
        lifetimeCoroutine = StartCoroutine(LifetimeCountdown());

        // 디버그 로그 추가
        Debug.Log($"투사체 {GetInstanceID()} 수명 타이머 시작: {lifetime}초");
    }

    public void ProjectileLaunched()
    {
        // 아직 Preparing 상태일 때만 상태 변경
        if (currentState == ProjectileState.Preparing)
        {
            currentState = ProjectileState.Launched;
            isActive = true;
        }
    }

    // 색상 시퀀스 설정 (외부에서 호출)
    public void SetColorSequence(Sequence sequence)
    {
        // 기존 시퀀스 정리
        if (colorSequence != null)
        {
            colorSequence.Kill();
        }

        colorSequence = sequence;
    }

    // 초기 색상 설정 (외부에서 호출)
    public void SetInitialColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;

            // 중요: 색상 설정 후 DOTween 시퀀스 제거 (색상이 덮어씌워지지 않도록)
            if (colorSequence != null)
            {
                colorSequence.Kill();
                colorSequence = null;
            }
        }
    }

    // 색상 깜빡임 시작 (외부에서 호출)
    public void StartColorBlink(float interval)
    {
        // 기존 시퀀스 정리
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        if (spriteRenderer != null)
        {
            // 색상 초기화 (보장)
            spriteRenderer.color = redColor;

            // 새 시퀀스 생성 (명확한 루프 설정)
            colorSequence = DOTween.Sequence();
            colorSequence.Append(spriteRenderer.DOColor(whiteColor, interval / 2))
                         .Append(spriteRenderer.DOColor(redColor, interval / 2))
                         .SetLoops(-1, LoopType.Restart);
        }
    }


    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Sqrt(
            Mathf.Pow(a.r - b.r, 2) +
            Mathf.Pow(a.g - b.g, 2) +
            Mathf.Pow(a.b - b.b, 2)
        );
    }
    // 수명 카운트다운 코루틴
    private IEnumerator LifetimeCountdown()
    {
        float timeElapsed = 0f;
        float checkInterval = 0.5f; // 주기적 확인 간격

        while (timeElapsed < lifetime)
        {
            yield return new WaitForSeconds(checkInterval);
            timeElapsed += checkInterval;

            // 안전 확인: 만약 객체가 비활성화되었다면 코루틴 종료
            if (!gameObject.activeInHierarchy)
            {
                yield break;
            }

            // 디버그 용도로 주기적으로 남은 시간 로깅 (선택적)
            if (timeElapsed % 1f < checkInterval)
            {
                Debug.Log($"투사체 {GetInstanceID()} 남은 수명: {lifetime - timeElapsed}초");
            }
        }

        Debug.Log($"투사체 {GetInstanceID()} 수명 종료, 풀로 반환");
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 타겟 레이어와 충돌 체크
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // 플레이어에게 데미지
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }

            // 충돌 효과 및 풀로 반환
            PlayHitEffect();
            ReturnToPool();
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            // 벽이나 장애물과 충돌 시
            PlayHitEffect();
            ReturnToPool();
        }
    }

    // 충돌 효과 재생
    private void PlayHitEffect()
    {
        // 히트 효과용 파티클 시스템이 있으면 재생
        // 여기서는 간단한 스케일 효과만 적용
        transform.DOScale(originalScale * 0.1f, 0.2f).SetEase(Ease.InBack);

        // 색상 페이드 아웃
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, 0.2f);
        }
    }

    // 풀로 반환
    private void ReturnToPool()
    {
        // 상태 변경
        currentState = ProjectileState.Destroyed;
        isActive = false;

        // 색상 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // 스케일 초기화
        transform.localScale = originalScale;

        // 시퀀스 정리
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        // 속도 초기화
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 오브젝트 풀로 반환
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 정리
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
            Debug.Log($"투사체 {GetInstanceID()} 비활성화로 코루틴 중지");
        }

        // 활성 상태 끄기
        isActive = false;
    }
    private void OnEnable()
    {
        // 활성화될 때마다 상태 초기화 (추가 보장)
        currentState = ProjectileState.Preparing;
        isActive = false;

        // 색상 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // 기존 시퀀스 정리
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }

        // 리지드바디 초기화
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    // 객체 파괴 시 정리
    private void OnDestroy()
    {
        if (colorSequence != null)
        {
            colorSequence.Kill();
        }
    }
}