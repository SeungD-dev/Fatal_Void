using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float minSpawnDistance = 12f;
    [SerializeField] private SpawnSettingsData spawnSettings;
    [SerializeField] private EnemySpawnDatabase enemyDatabase;
    [SerializeField] private int spawnPositionCacheSize = 100;

    [Header("Spawn Cache")]
    private Vector2[] cachedSpawnPositions;
    private int currentCacheIndex;

    // 사분면별 스폰 포인트
    private readonly List<Vector2>[] quadrantSpawnPoints = new List<Vector2>[4];
    private int[] quadrantSpawnIndices = new int[4];

    [Header("Time Settings")]
    private float gameTime = 0f;
    private float currentSpawnInterval;
    private float nextSpawnTime;
    private int currentSpawnAmount;

    private Transform playerTransform;
    private Camera mainCamera;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(InitializeAfterGameStart());
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        InitializeSpawnSystem();
    }

    private void InitializeSpawnSystem()
    {
        mainCamera = Camera.main;
        InitializeSpawnCache();
        InitializeQuadrants();
    }

    private void InitializeSpawnCache()
    {
        cachedSpawnPositions = new Vector2[spawnPositionCacheSize];
        for (int i = 0; i < spawnPositionCacheSize; i++)
        {
            cachedSpawnPositions[i] = GenerateSpawnPosition();
        }
    }

    private void InitializeQuadrants()
    {
        for (int i = 0; i < 4; i++)
        {
            quadrantSpawnPoints[i] = new List<Vector2>();
        }

        // 각 사분면별 스폰 포인트 미리 계산
        for (int i = 0; i < spawnPositionCacheSize / 4; i++)
        {
            for (int q = 0; q < 4; q++)
            {
                quadrantSpawnPoints[q].Add(GenerateQuadrantSpawnPosition(q));
            }
        }
    }

    private IEnumerator InitializeAfterGameStart()
    {
        while (GameManager.Instance.PlayerStats == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // 초기 스폰 설정
        var initialSettings = spawnSettings.GetSettingsAtTime(0);
        currentSpawnInterval = initialSettings.spawnInterval;
        currentSpawnAmount = initialSettings.spawnAmount;
        nextSpawnTime = Time.time;

        // 적 풀 초기화
        InitializeEnemyPools();

        // 스폰 카운트 초기화
        enemyDatabase.ResetSpawnCounts();

        isInitialized = true;
    }

    private void InitializeEnemyPools()
    {
        foreach (var settings in enemyDatabase.enemySettings)
        {
            if (settings.enemyData != null && settings.enemyData.enemyPrefab != null)
            {
                ObjectPool.Instance.CreatePool(
                    settings.enemyData.enemyName,
                    settings.enemyData.enemyPrefab,
                    settings.enemyData.initialPoolSize
                );
            }
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        enabled = (newState == GameState.Playing);
    }

    private void Update()
    {
        if (!isInitialized || !enabled ||
            GameManager.Instance.currentGameState != GameState.Playing)
        {
            return;
        }

        gameTime += Time.deltaTime;

        // 스폰 설정 업데이트
        var currentSettings = spawnSettings.GetSettingsAtTime(gameTime);
        currentSpawnAmount = currentSettings.spawnAmount;
        currentSpawnInterval = currentSettings.spawnInterval;

        TrySpawnEnemies();
    }

    private void TrySpawnEnemies()
    {
        if (Time.time >= nextSpawnTime)
        {
            StartCoroutine(SpawnEnemyBatch());
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
    }

    private IEnumerator SpawnEnemyBatch()
    {
        for (int i = 0; i < currentSpawnAmount; i++)
        {
            SpawnEnemy();

            // 프레임 드랍 방지를 위해 스폰을 분산
            if (i % 3 == 0) // 3개씩 스폰 후 다음 프레임으로
            {
                yield return null;
            }
        }
    }

    private void SpawnEnemy()
    {
        if (!enabled || GameManager.Instance.currentGameState != GameState.Playing)
        {
            return;
        }

        EnemyData enemyData = enemyDatabase.GetRandomEnemy(gameTime);
        if (enemyData == null)
        {
            Debug.LogWarning("Failed to get enemy data for spawning");
            return;
        }

        Vector2 spawnPosition = GetOptimizedSpawnPosition();
        GameObject enemyObject = ObjectPool.Instance.SpawnFromPool(
            enemyData.enemyName,
            spawnPosition,
            Quaternion.identity
        );

        if (enemyObject != null)
        {
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            EnemyAI enemyAI = enemyObject.GetComponent<EnemyAI>();

            if (enemy != null && enemyAI != null)
            {
                enemy.SetEnemyData(enemyData);
                enemy.Initialize(playerTransform);
                enemyAI.Initialize(playerTransform);
            }
            else
            {
                Debug.LogError($"Required components not found on prefab: {enemyData.enemyName}");
                ObjectPool.Instance.ReturnToPool(enemyData.enemyName, enemyObject);
            }
        }
    }

    private Vector2 GetOptimizedSpawnPosition()
    {
        // 플레이어의 현재 위치
        Vector2 playerPos = playerTransform.position;

        // 최적의 스폰 사분면 선택
        int quadrant = GetOptimalSpawnQuadrant();
        Vector2 basePosition = quadrantSpawnPoints[quadrant][quadrantSpawnIndices[quadrant]];

        // 인덱스 순환
        quadrantSpawnIndices[quadrant] = (quadrantSpawnIndices[quadrant] + 1) % quadrantSpawnPoints[quadrant].Count;

        Vector2 spawnPosition = playerPos + basePosition;

        // 화면에 보이는지 확인
        Vector2 viewportPoint = mainCamera.WorldToViewportPoint(spawnPosition);
        if (IsPositionVisible(viewportPoint))
        {
            // 보이는 경우 다른 사분면에서 재시도
            int attempts = 4;
            while (attempts-- > 0 && IsPositionVisible(viewportPoint))
            {
                quadrant = (quadrant + 1) % 4;
                basePosition = quadrantSpawnPoints[quadrant][quadrantSpawnIndices[quadrant]];
                spawnPosition = playerPos + basePosition;
                viewportPoint = mainCamera.WorldToViewportPoint(spawnPosition);
            }
        }

        return spawnPosition;
    }

    private int GetOptimalSpawnQuadrant()
    {
        if (playerTransform == null) return Random.Range(0, 4);

        // 플레이어의 이동 방향 고려
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        Vector2 playerVelocity = playerRb != null ? playerRb.linearVelocity : Vector2.zero;

        if (Mathf.Abs(playerVelocity.x) > Mathf.Abs(playerVelocity.y))
        {
            return playerVelocity.x > 0 ? 3 : 1; // 오른쪽 또는 왼쪽
        }
        else
        {
            return playerVelocity.y > 0 ? 0 : 2; // 위 또는 아래
        }
    }

    private Vector2 GenerateQuadrantSpawnPosition(int quadrant)
    {
        float angle = Random.Range(quadrant * 90f, (quadrant + 1) * 90f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, spawnRadius);

        return new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
    }

    private Vector2 GenerateSpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, spawnRadius);

        return new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
    }

    private bool IsPositionVisible(Vector2 viewportPoint)
    {
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 스폰 영역 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform ? playerTransform.position : transform.position, spawnRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform ? playerTransform.position : transform.position, minSpawnDistance);

        // 캐시된 스폰 포인트 시각화
        if (playerTransform)
        {
            Gizmos.color = Color.yellow;
            Vector3 playerPos = playerTransform.position;
            for (int i = 0; i < 4; i++)
            {
                if (quadrantSpawnPoints[i] != null)
                {
                    foreach (var point in quadrantSpawnPoints[i])
                    {
                        Gizmos.DrawWireSphere(point + (Vector2)playerPos, 0.3f);
                    }
                }
            }
        }
    }
#endif
}