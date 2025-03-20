using UnityEngine;
using System.Collections.Generic;

public class EnemyCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float cullingDistance = 30f;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float screenBuffer = 2f;

    [Header("Optimization")]
    [SerializeField] private bool useDistanceBasedInterval = true;
    [SerializeField] private float nearUpdateInterval = 0.1f;   // 가까운 적 업데이트 간격
    [SerializeField] private float farUpdateInterval = 0.3f;    // 먼 적 업데이트 간격
    [SerializeField] private float distanceThreshold = 15f;     // 가까운/먼 거리 기준

    // 캐싱된 변수들
    private Camera mainCamera;
    private Transform playerTransform;
    private float nextUpdateTime;
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private List<Enemy> enemiesCache = new List<Enemy>();
    private Vector2 screenBounds;
    private float aspectRatio;

    // 거리 기반 업데이트를 위한 변수들
    private HashSet<Enemy> nearEnemies = new HashSet<Enemy>();
    private HashSet<Enemy> farEnemies = new HashSet<Enemy>();
    private float nextNearUpdateTime;
    private float nextFarUpdateTime;
    private float sqrDistanceThreshold;

    private void Awake()
    {
        // 거리 임계값 제곱 (매번 제곱근 계산 회피)
        sqrDistanceThreshold = distanceThreshold * distanceThreshold;
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // GameManager에서 플레이어 참조 가져오기
        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            playerTransform = GameManager.Instance.PlayerTransform;
        }
        else
        {
            // 폴백으로 Find 사용 (초기화 시 한 번만)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not found!");
        }

        CalculateScreenBounds();

        // 초기 업데이트 시간 설정
        nextUpdateTime = Time.time + updateInterval;
        nextNearUpdateTime = Time.time + nearUpdateInterval;
        nextFarUpdateTime = Time.time + farUpdateInterval;
    }
    private void CalculateScreenBounds()
    {
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize * 2;
        aspectRatio = (float)Screen.width / Screen.height;
        float cameraWidth = cameraHeight * aspectRatio;
        screenBounds = new Vector2(cameraWidth / 2, cameraHeight / 2);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float currentTime = Time.time;

        if (useDistanceBasedInterval)
        {
            // 가까운 적들 더 자주 업데이트
            if (currentTime >= nextNearUpdateTime)
            {
                UpdateNearEnemyCulling();
                nextNearUpdateTime = currentTime + nearUpdateInterval;
            }

            // 먼 적들 덜 자주 업데이트
            if (currentTime >= nextFarUpdateTime)
            {
                UpdateFarEnemyCulling();
                nextFarUpdateTime = currentTime + farUpdateInterval;
            }
        }
        else
        {
            // 기존 방식 - 모든 적을 동일한 간격으로 업데이트
            if (currentTime >= nextUpdateTime)
            {
                UpdateAllEnemyCulling();
                nextUpdateTime = currentTime + updateInterval;
            }
        }
    }

    // 가까운 적들 업데이트
    private void UpdateNearEnemyCulling()
    {
        enemiesCache.Clear();
        enemiesCache.AddRange(nearEnemies);

        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                nearEnemies.Remove(enemy);
                activeEnemies.Remove(enemy);
                continue;
            }

            // 거리 확인 및 필요시 집합 재분류
            Vector2 enemyPos = enemy.transform.position;
            Vector2 playerPos = playerTransform.position;
            float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

            if (distanceSqr > sqrDistanceThreshold)
            {
                // 멀어졌으므로 far로 이동
                nearEnemies.Remove(enemy);
                farEnemies.Add(enemy);
            }
            else
            {
                // 여전히 가까우므로 컬링 상태 업데이트
                UpdateSingleEnemyCulling(enemy);
            }
        }
    }

    // 먼 적들 업데이트
    private void UpdateFarEnemyCulling()
    {
        enemiesCache.Clear();
        enemiesCache.AddRange(farEnemies);

        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                farEnemies.Remove(enemy);
                activeEnemies.Remove(enemy);
                continue;
            }

            // 거리 확인 및 필요시 집합 재분류
            Vector2 enemyPos = enemy.transform.position;
            Vector2 playerPos = playerTransform.position;
            float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

            if (distanceSqr <= sqrDistanceThreshold)
            {
                // 가까워졌으므로 near로 이동
                farEnemies.Remove(enemy);
                nearEnemies.Add(enemy);
            }
            else
            {
                // 여전히 멀리 있으므로 컬링 상태 업데이트
                UpdateSingleEnemyCulling(enemy);
            }
        }
    }

    // 모든 적들 업데이트 (기존 방식)
    private void UpdateAllEnemyCulling()
    {
        if (playerTransform == null) return;

        // 현재 활성화된 적들의 목록을 캐시에 복사
        enemiesCache.Clear();
        enemiesCache.AddRange(activeEnemies);

        // 캐시된 목록을 사용하여 컬링 업데이트
        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                activeEnemies.Remove(enemy);
                continue;
            }

            UpdateSingleEnemyCulling(enemy);
        }

        // 파괴된 적들 정리
        activeEnemies.RemoveWhere(e => e == null);
    }

    // 적 등록
    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);

            // 컬링 매니저 참조 설정
            enemy.SetCullingManager(this);

            // 플레이어 참조 전달
            if (playerTransform != null)
            {
                enemy.Initialize(playerTransform);
            }

            // 거리 기반 분류
            if (useDistanceBasedInterval && playerTransform != null)
            {
                float distanceSqr = Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position - (Vector2)playerTransform.position);

                if (distanceSqr <= sqrDistanceThreshold)
                {
                    nearEnemies.Add(enemy);
                }
                else
                {
                    farEnemies.Add(enemy);
                }
            }

            // 초기 컬링 상태 설정
            UpdateSingleEnemyCulling(enemy);
        }
    }

    // 적 등록 해제
    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        nearEnemies.Remove(enemy);
        farEnemies.Remove(enemy);
    }

    // 개별 적 컬링 상태 업데이트
    private void UpdateSingleEnemyCulling(Enemy enemy)
    {
        if (enemy == null || playerTransform == null) return;

        Vector2 enemyPos = enemy.transform.position;
        Vector2 playerPos = playerTransform.position;
        float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

        // 거리 기반 컬링
        if (distanceSqr > cullingDistance * cullingDistance)
        {
            enemy.SetCullingState(false);
            return;
        }

        // 화면 기반 컬링 (가시성 확인)
        Vector2 viewportPoint = mainCamera.WorldToViewportPoint(enemyPos);
        bool isVisible = IsInScreenBounds(viewportPoint);
        enemy.SetCullingState(isVisible);
    }

    private bool IsInScreenBounds(Vector2 viewportPoint)
    {
        float buffer = screenBuffer;
        return viewportPoint.x >= -buffer && viewportPoint.x <= (1 + buffer) &&
               viewportPoint.y >= -buffer && viewportPoint.y <= (1 + buffer);
    }

    // 화면 크기 변경 시 경계값 재계산
    private void OnRectTransformDimensionsChange()
    {
        CalculateScreenBounds();
    }

    // 최적화 설정 실시간 변경 메서드
    public void SetDistanceBasedInterval(bool enable)
    {
        useDistanceBasedInterval = enable;

        if (!enable)
        {
            // 비활성화 시 모든 적을 한 번 업데이트
            UpdateAllEnemyCulling();
        }
        else
        {
            // 활성화 시 적들을 거리에 따라 분류
            ReclassifyEnemiesByDistance();
        }
    }

    // 거리에 따라 적 재분류
    private void ReclassifyEnemiesByDistance()
    {
        if (playerTransform == null) return;

        nearEnemies.Clear();
        farEnemies.Clear();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            float distanceSqr = Vector2.SqrMagnitude(
                (Vector2)enemy.transform.position - (Vector2)playerTransform.position);

            if (distanceSqr <= sqrDistanceThreshold)
            {
                nearEnemies.Add(enemy);
            }
            else
            {
                farEnemies.Add(enemy);
            }
        }
    }
    public void SetPlayerReference(Transform player)
    {
        if (player != null)
        {
            playerTransform = player;

            // 이미 등록된 모든 적에게 플레이어 참조 전달
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.Initialize(playerTransform);
                }
            }
        }
    }


#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (!Application.isPlaying || playerTransform == null) return;

    // 컬링 범위 시각화
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(playerTransform.position, cullingDistance);

    // 화면 범위 시각화
    Gizmos.color = Color.yellow;
    Vector3 center = playerTransform.position;
    Vector3 size = new Vector3(screenBounds.x * 2, screenBounds.y * 2, 0);
    Gizmos.DrawWireCube(center, size);

    // 버퍼 영역 시각화
    Gizmos.color = Color.green;
    float bufferSize = screenBuffer * 2;
    Vector3 bufferSizeVec = new Vector3(
        size.x * (1 + bufferSize),
        size.y * (1 + bufferSize),
        0
    );
    Gizmos.DrawWireCube(center, bufferSizeVec);

    // 거리 기반 업데이트 임계값 시각화
    if (useDistanceBasedInterval)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, distanceThreshold);
    }
}
}
#endif