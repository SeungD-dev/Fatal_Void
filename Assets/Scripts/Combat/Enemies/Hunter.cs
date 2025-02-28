using UnityEngine;

public class Hunter : EnemyAI
{
    [Header("Aura Settings")]
    [SerializeField] private Transform auraTransform;  // 오라 오브젝트의 Transform
    [SerializeField] private float rotationSpeed = 100f;  // 회전 속도 (도/초)

    // 오라 회전 최적화를 위한 변수
    [SerializeField] private float auraRotationInterval = 0.033f; // 약 30Hz로 회전 업데이트
    private float nextAuraRotationTime;
    private float accumulatedRotation; // 누적된 회전량

    // 거리 기반 최적화를 위한 추가 변수
    [SerializeField] private float auraOptimizationDistance = 20f; // 최적화 시작 거리
    private float sqrAuraOptimizationDistance;
    private bool isAuraOptimized = false;

    protected override void Awake()
    {
        base.Awake();

        // 초기화
        nextAuraRotationTime = Time.time;
        sqrAuraOptimizationDistance = auraOptimizationDistance * auraOptimizationDistance;

        // 초기 회전 값을 랜덤하게 설정하여 모든 오라가 동일한 위치에서 시작하지 않도록 함
        if (auraTransform != null)
        {
            auraTransform.Rotate(Vector3.forward * Random.Range(0f, 360f));
        }
    }

    protected override void InitializeStates()
    {
        base.InitializeStates();
        // Hunter 전용 상태 추가 가능
    }

    protected override void Update()
    {
        // 기본 AI 로직 업데이트 (이동 제외)
        base.Update();

        // 컬링되었거나 비활성 상태면 오라 업데이트 건너뛰기
        if (isCulled || !isActive) return;

        // 오라 회전 - 시각적 업데이트이므로 Update에서 수행
        UpdateAura();
    }

    protected override void FixedUpdate()
    {
        // 기본 AI 물리 로직 업데이트
        base.FixedUpdate();
    }

    // 거리에 따른 오라 효과 최적화 업데이트
    protected override void UpdateVisualEffects()
    {
        base.UpdateVisualEffects();

        // 플레이어와의 거리에 따라 오라 최적화 상태 결정
        if (playerTransform != null)
        {
            float sqrDistance = (transform.position - playerTransform.position).sqrMagnitude;

            // 거리에 따라 오라 최적화 상태 설정
            if (sqrDistance > sqrAuraOptimizationDistance)
            {
                // 멀리 있을 때 오라 최적화 (낮은 프레임 레이트로 업데이트)
                isAuraOptimized = true;
                auraRotationInterval = 0.1f; // 10Hz로 감소
            }
            else
            {
                // 가까이 있을 때 정상 업데이트
                isAuraOptimized = false;
                auraRotationInterval = 0.033f; // 30Hz로 복원
            }
        }
    }

    // 오라 업데이트 메서드
    private void UpdateAura()
    {
        if (auraTransform == null) return;

        // 시간 기반 업데이트
        if (Time.time >= nextAuraRotationTime)
        {
            // 마지막 업데이트 이후 경과한 시간에 따른 회전량 계산
            float timeSinceLastUpdate = Time.time - (nextAuraRotationTime - auraRotationInterval);
            float rotationAmount = rotationSpeed * timeSinceLastUpdate;

            // 최적화 모드일 때 회전량 조정
            if (isAuraOptimized)
            {
                // 플레이어에게서 멀리 있을 때는 낮은 프레임 레이트로 업데이트하되,
                // 부드러운 회전을 위해 누적된 회전량을 더 크게 적용
                rotationAmount *= 1.5f;
            }

            // 오라 회전 적용
            auraTransform.Rotate(Vector3.forward * rotationAmount);

            // 다음 회전 시간 설정
            nextAuraRotationTime = Time.time + auraRotationInterval;
        }
    }

    // 컬링 상태 설정 시 추가 작업
    public override void SetCullingState(bool isVisible)
    {
        base.SetCullingState(isVisible);

        // 컬링 상태에 따라 오라 활성화/비활성화
        if (auraTransform != null)
        {
            auraTransform.gameObject.SetActive(isVisible);
        }
    }

    // 디버그용 기즈모
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!Application.isPlaying) return;

        // 오라 최적화 거리 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, auraOptimizationDistance);
    }
}