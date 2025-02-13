using System.Collections;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float minSpawnDistance = 12f;
    [SerializeField] private SpawnSettingsData spawnSettings;

    [SerializeField] private EnemySpawnDatabase enemyDatabase;


    [Header("Time Settings")]
    private float gameTime = 0f;
    private const float SPAWN_INTERVAL_START = 3f;
    private const float SPAWN_INTERVAL_MIN = 1f;
    private const float INTERVAL_UPDATE_TIME = 30f;
    private const float INTERVAL_DECREASE = 0.1f;

    [Header("Spawn Amount Settings")]
    private const int INITIAL_SPAWN_AMOUNT = 3;
    private const float AMOUNT_UPDATE_TIME = 30f;

    private float currentSpawnInterval;
    private float nextSpawnTime;
    private int currentSpawnAmount;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isInitialized = false;

    private float lastIntervalUpdateTime = 0f;
    private float lastAmountUpdateTime = 0f;

    private void Start()
    {
        StartCoroutine(InitializeAfterGameStart());
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
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

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
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


    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            enabled = true;
        }
        else
        {
            enabled = false;
        }
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


    private void UpdateSpawnInterval()
    {
        if (gameTime - lastIntervalUpdateTime >= INTERVAL_UPDATE_TIME)
        {
            lastIntervalUpdateTime = gameTime;

            if (gameTime <= 600f && currentSpawnInterval > SPAWN_INTERVAL_MIN)
            {
                currentSpawnInterval = Mathf.Max(
                    SPAWN_INTERVAL_MIN,
                    currentSpawnInterval - INTERVAL_DECREASE
                );
                Debug.Log($"[{gameTime:F1}s] Spawn interval updated to: {currentSpawnInterval:F1}s");
            }
        }
    }

    private void UpdateSpawnAmount()
    {
        if (gameTime - lastAmountUpdateTime >= AMOUNT_UPDATE_TIME)
        {
            lastAmountUpdateTime = gameTime;
            currentSpawnAmount++;
            Debug.Log($"[{gameTime:F1}s] Spawn amount updated to: {currentSpawnAmount}");
        }
    }

    private void TrySpawnEnemies()
    {
        if (Time.time >= nextSpawnTime)
        {
            for (int i = 0; i < currentSpawnAmount; i++)
            {
                SpawnEnemy();
            }

            nextSpawnTime = Time.time + currentSpawnInterval;
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

        Vector2 spawnPosition = GetSpawnPosition();
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

                // EnemyAI 초기화
                enemyAI.Initialize(playerTransform);
            }
            else
            {
                Debug.LogError($"Required components not found on prefab: {enemyData.enemyName}");
                ObjectPool.Instance.ReturnToPool(enemyData.enemyName, enemyObject);
            }
        }
    }

    private Vector2 GetSpawnPosition()
    {
        Vector2 viewportPoint = Random.insideUnitCircle.normalized;
        Vector2 playerPos = playerTransform.position;
        float distance = Random.Range(minSpawnDistance, spawnRadius);

        return playerPos + (viewportPoint * distance);
    }

    private EnemyData SelectEnemyType()
    {
        if (enemyDatabase == null)
        {
            Debug.LogError("EnemySpawnDatabase is not assigned!");
            return null;
        }

        return enemyDatabase.GetRandomEnemy(gameTime);
    }

    // 디버그 UI는 게임이 Playing 상태일 때만 표시
    //private void OnGUI()
    //{
    //    if (!Application.isEditor || !enabled ||
    //        GameManager.Instance.currentGameState != GameState.Playing)
    //        return;

    //    GUILayout.BeginArea(new Rect(10, 10, 300, 150));
    //    GUILayout.Label($"Game Time: {(int)(gameTime / 60):D2}:{(gameTime % 60):00.0}");
    //    GUILayout.Label($"Spawn Interval: {currentSpawnInterval:F1}s");
    //    GUILayout.Label($"Spawn Amount: {currentSpawnAmount}");
    //    GUILayout.Label($"Next Spawn: {(nextSpawnTime - Time.time):F1}s");

    //    // 현재 스폰된 적들의 비율 표시
    //    foreach (var settings in enemyDatabase.enemySettings)
    //    {
    //        float ratio = gameTime == 0 ? 0 :
    //                     (settings.spawnCount * 100f / enemyDatabase.ratioCheckInterval);
    //        GUILayout.Label($"{settings.enemyData.enemyName}: {ratio:F1}%");
    //    }
    //    GUILayout.EndArea();
    //}

    // 디버그용 기즈모
    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying || playerTransform == null)
    //        return;

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
    //}
}

