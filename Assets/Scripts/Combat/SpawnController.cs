using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float minSpawnDistance = 12f;
    [SerializeField] private EnemySpawnDatabase spawnDatabase;

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
        InitializeEnemyPools();
    }

    private void InitializeEnemyPools()
    {
        if (spawnDatabase == null) return;

        foreach (var spawnWeight in spawnDatabase.enemySpawnWeights)
        {
            if (spawnWeight.enemyData != null && spawnWeight.enemyData.enemyPrefab != null)
            {
                // 각 적 타입별로 풀 생성
                ObjectPool.Instance.CreatePool(
                    spawnWeight.enemyData.enemyName,  // 풀의 태그로 적 이름 사용
                    spawnWeight.enemyData.enemyPrefab,
                    spawnWeight.enemyData.initialPoolSize
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
        // Player와 GameManager가 초기화될 때까지 대기
        while (GameManager.Instance.PlayerStats == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        mainCamera = Camera.main;

        // 초기값 설정
        currentSpawnInterval = SPAWN_INTERVAL_START;
        currentSpawnAmount = INITIAL_SPAWN_AMOUNT;
        nextSpawnTime = Time.time;

        isInitialized = true;
        Debug.Log("SpawnDirector initialized");
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            enabled = true;
            Debug.Log("SpawnDirector enabled");
        }
        else
        {
            enabled = false;
            Debug.Log("SpawnDirector disabled");
        }
    }

    private void Update()
    {
        if (!isInitialized || !enabled || GameManager.Instance.currentGameState != GameState.Playing)
        {
            return;
        }

        gameTime += Time.deltaTime;
        UpdateSpawnInterval();
        UpdateSpawnAmount();
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

        EnemyData enemyData = SelectEnemyType();
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogError("Invalid enemy data or missing prefab!");
            return;
        }

        Vector2 spawnPosition = GetSpawnPosition();

        // 풀에서 적 스폰 (풀 태그로 적 이름 사용)
        GameObject enemyObject = ObjectPool.Instance.SpawnFromPool(
            enemyData.enemyName,
            spawnPosition,
            Quaternion.identity
        );

        if (enemyObject == null) return;

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetEnemyData(enemyData);
        }
        else
        {
            Debug.LogError($"Enemy component not found on prefab: {enemyData.enemyName}");
            ObjectPool.Instance.ReturnToPool(enemyData.enemyName, enemyObject);
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
        if (spawnDatabase == null)
        {
            Debug.LogError("EnemySpawnDatabase is not assigned!");
            return null;
        }

        return spawnDatabase.GetRandomEnemy(gameTime);
    }

    // 디버그 UI는 게임이 Playing 상태일 때만 표시
    private void OnGUI()
    {
        if (!Application.isEditor || !enabled ||
            GameManager.Instance.currentGameState != GameState.Playing)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Game Time: {gameTime:F1}s");
        GUILayout.Label($"Spawn Interval: {currentSpawnInterval:F1}s");
        GUILayout.Label($"Spawn Amount: {currentSpawnAmount}");
        GUILayout.EndArea();
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || playerTransform == null)
            return;

        // 스폰 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, spawnRadius);

        // 최소 스폰 거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);
    }
}

