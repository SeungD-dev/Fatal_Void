using UnityEngine;
using System.Collections.Generic;

public class EnemyCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float cullingDistance = 30f;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float screenBuffer = 2f;

    private Camera mainCamera;
    private Transform playerTransform;
    private float nextUpdateTime;
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private List<Enemy> enemiesCache = new List<Enemy>();
    private Vector2 screenBounds;
    private float aspectRatio;

    private void Start()
    {
        mainCamera = Camera.main;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        CalculateScreenBounds();
    }

    private void CalculateScreenBounds()
    {
        float cameraHeight = mainCamera.orthographicSize * 2;
        aspectRatio = (float)Screen.width / Screen.height;
        float cameraWidth = cameraHeight * aspectRatio;
        screenBounds = new Vector2(cameraWidth / 2, cameraHeight / 2);
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateEnemyCulling();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            UpdateSingleEnemyCulling(enemy);
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    private void UpdateEnemyCulling()
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

    private void UpdateSingleEnemyCulling(Enemy enemy)
    {
        if (enemy == null || playerTransform == null) return;

        Vector2 enemyPos = enemy.transform.position;
        Vector2 playerPos = playerTransform.position;

        float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);
        if (distanceSqr > cullingDistance * cullingDistance)
        {
            enemy.SetCullingState(false);
            return;
        }

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
    }
#endif
}