using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static WaveData;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveData waveData;
    [SerializeField] private SpawnWarningController warningController;
    [SerializeField] private ShopController shopController;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject warningPrefab; // 경고 프리팹

    [Header("Wave UI")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private GameObject waveCompleteBanner;
    [SerializeField] private TextMeshProUGUI waveCompleteText;

    [Header("Spawn Settings")]
    [SerializeField] private float minDistanceFromPlayer = 8f; // 플레이어로부터 최소 스폰 거리

    // 웨이브 상태
    private int currentWaveNumber = 0;
    private bool isWaveActive = false;
    private bool isInSurvivalPhase = false;
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private WaveData.Wave currentWave;
    private Coroutine spawnCoroutine;

    // 캐싱
    private PlayerStats playerStats;
    private PlayerUIController playerUIController;
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private Camera mainCamera;
    private GameMap gameMap;

    // 문자열 캐시
    private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
    private const string WAVE_TIME_FORMAT = "Wave: {0:00}";
    private const string SURVIVAL_TIME_FORMAT = "Survive: {0:00}";

    private void Awake()
    {
        if (waveData == null)
        {
            Debug.LogError("WaveData is not assigned!");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
    }

    private void ValidateReferences()
    {
        if (waveData == null)
        {
            Debug.LogError("WaveData is not assigned!");
            enabled = false;
            return;
        }
        playerUIController = FindAnyObjectByType<PlayerUIController>();
    }

    private void Start()
    {
        // 필수 의존성이 모두 준비될 때까지 기다리는 코루틴 실행
        StartCoroutine(WaitForDependencies());

        // 독립적으로 초기화할 수 있는 작업 먼저 실행
        InitializeWarningPool();

        // 인벤토리 컨트롤러의 진행 버튼 이벤트 연결
        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked += StartNextWave;
        }
    }
    private IEnumerator WaitForDependencies()
    {
        float timeOut = 5f;
        float elapsed = 0f;

        // GameManager 의존성 확인
        while (GameManager.Instance == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found after timeout!");
            yield break;
        }

        // PlayerStats 의존성 초기화
        while (GameManager.Instance.PlayerStats == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance.PlayerStats == null)
        {
            Debug.LogError("PlayerStats not found after timeout!");
            yield break;
        }

        playerStats = GameManager.Instance.PlayerStats;
        playerStats.OnPlayerDeath += HandlePlayerDeath;

        // MapManager 의존성 확인
        while (MapManager.Instance == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager not found after timeout!");
            yield break;
        }

        // 맵 로드 대기
        yield return StartCoroutine(WaitForMapLoad());

        // 나머지 이벤트 구독
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        playerUIController = FindAnyObjectByType<PlayerUIController>();
    }
    private IEnumerator WaitForMapLoad()
    {
        float timeOut = 2f; // 더 짧은 타임아웃 (5초→2초)
        float elapsed = 0f;

        // MapManager에 현재 맵이 로드될 때까지 기다림
        while (MapManager.Instance.CurrentMap == null && elapsed < timeOut)
        {
            elapsed += 0.05f; // 더 짧은 간격으로 체크 (0.1초→0.05초)
            yield return new WaitForSeconds(0.05f);
        }

        // 맵 참조 가져오기
        gameMap = MapManager.Instance.CurrentMap;

        if (gameMap == null)
        {
            Debug.LogError("GameMap not found after timeout!");
            yield break;
        }

        // 맵이 로드된 후에 실행되어야 하는 초기화 로직
        InitializeEnemyPools();
        SetupFirstWave();

        Debug.Log("WaveManager fully initialized with map reference");
    }

    private void InitializeSystem()
    {
        // 맵이 로드된 후에 실행되어야 하는 초기화 로직
        InitializeEnemyPools();
        SetupFirstWave();
    }
    private void InitializeWarningPool()
    {
        if (warningPrefab != null && ObjectPool.Instance != null)
        {
            if (!ObjectPool.Instance.DoesPoolExist("SpawnWarning"))
            {
                ObjectPool.Instance.CreatePool("SpawnWarning", warningPrefab, 10);
            }
        }
    }
    private void InitializeEnemyPools()
    {
        // 모든 웨이브에서 사용되는 적 유형 수집
        HashSet<EnemyData> allEnemyTypes = new HashSet<EnemyData>();

        foreach (var wave in waveData.waves)
        {
            foreach (var enemy in wave.enemies)
            {
                if (enemy.enemyData != null)
                {
                    allEnemyTypes.Add(enemy.enemyData);
                }
            }
        }

        // 각 적 유형에 대한 풀 생성
        foreach (var enemyData in allEnemyTypes)
        {
            if (enemyData.enemyPrefab != null)
            {
                // 이미 풀이 있는지 확인
                if (!ObjectPool.Instance.DoesPoolExist(enemyData.enemyName))
                {
                    // 컬링 매니저 참조 얻기
                    EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();

                    // Enemy 컴포넌트 초기화
                    if (cullingManager != null)
                    {
                        GameObject prefabInstance = enemyData.enemyPrefab;
                        Enemy enemyComponent = prefabInstance.GetComponent<Enemy>();
                        if (enemyComponent != null)
                        {
                            enemyComponent.SetCullingManager(cullingManager);
                        }
                    }

                    // 풀 생성
                    ObjectPool.Instance.CreatePool(
                        enemyData.enemyName,
                        enemyData.enemyPrefab,
                        enemyData.initialPoolSize
                    );
                }
            }
        }
    }

    private void SetupFirstWave()
    {
        currentWaveNumber = 1;
        currentWave = waveData.GetWave(currentWaveNumber);
        if (currentWave != null)
        {
            UpdateWaveUI();
        }
        else
        {
            Debug.LogError("Failed to get first wave data!");
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // 게임 플레이 상태일 때만 웨이브 진행
        if (newState == GameState.Playing)
        {
            // 게임이 시작되면 첫 웨이브 시작
            if (!isWaveActive && currentWaveNumber == 1 && waveTimer == 0f)
            {
                StartWave(currentWaveNumber);
            }
            else if (isWaveActive && spawnCoroutine == null)
            {
                // 일시정지 후 재개 시 스폰 코루틴 다시 시작
                spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());
            }
        }
        else if (newState == GameState.Paused || newState == GameState.GameOver)
        {
            // 일시정지나 게임오버 시 스폰 코루틴 중지
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    private void Update()
    {
        if (!isWaveActive || GameManager.Instance.currentGameState != GameState.Playing)
            return;

        // 타이머 업데이트
        waveTimer += Time.deltaTime;

        // 타이머 UI 업데이트
        UpdateTimerUI();

        // 웨이브 단계 관리
        if (!isInSurvivalPhase && waveTimer >= currentWave.waveDuration)
        {
            // 웨이브 시간 종료 - 생존 단계 시작
            isInSurvivalPhase = true;
            waveTimer = 0f; // 타이머 리셋

            // 스폰 코루틴 중지
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
        else if (isInSurvivalPhase && waveTimer >= currentWave.survivalDuration)
        {
            // 생존 단계 완료 - 웨이브 클리어
            CompleteWave();
        }

        // 주기적으로 파괴된 적 정리
        if (Time.frameCount % 60 == 0) // 약 1초마다
        {
            CleanupDestroyedEnemies();
        }
    }

    public void StartNextWave()
    {
        // First time this is called, it should start wave 1
        // Next times, it will get the next wave number

        int nextWaveNumber;
        if (currentWaveNumber == 0) // First time
        {
            nextWaveNumber = 1;
        }
        else
        {
            nextWaveNumber = waveData.GetNextWaveNumber(currentWaveNumber);
        }

        // Start the appropriate wave
        if (nextWaveNumber > 0)
        {
            // Hide wave complete banner
            if (waveCompleteBanner != null)
            {
                waveCompleteBanner.SetActive(false);
            }

            StartWave(nextWaveNumber);
        }
        else
        {
            // All waves completed - game victory
            Debug.Log("All waves completed!");
            GameManager.Instance.SetGameState(GameState.GameOver);
        }
    }

    public void StartWave(int waveNumber)
    {
        currentWave = waveData.GetWave(waveNumber);
        if (currentWave == null)
        {
            Debug.LogError($"Wave {waveNumber} not found!");
            return;
        }

        // 웨이브 정보 설정
        currentWaveNumber = waveNumber;
        waveTimer = 0f;
        isWaveActive = true;
        isInSurvivalPhase = false;

        // UI 업데이트
        UpdateWaveUI();

        // 스폰 코루틴 시작
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());

        // 게임 상태 플레이로 설정
        GameManager.Instance.SetGameState(GameState.Playing);
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        // 스폰 타이머 초기화
        spawnTimer = 0f;

        // 웨이브 활성화 상태 및 생존 단계가 아닐 때만 스폰
        while (isWaveActive && !isInSurvivalPhase)
        {
            // 게임이 플레이 상태일 때만 실행
            if (GameManager.Instance.currentGameState == GameState.Playing)
            {
                spawnTimer += Time.deltaTime;

                // 스폰 시간이 되었을 때
                if (spawnTimer >= currentWave.spawnInterval)
                {
                    // 적 스폰
                    SpawnEnemyBatch(currentWave.spawnAmount);
                    spawnTimer = 0f;
                }

                // 웨이브 시간이 종료되었는지 확인
                if (waveTimer >= currentWave.waveDuration)
                {
                    break; // 스폰 중단
                }
            }

            yield return null;
        }

        spawnCoroutine = null;
    }

    private void SpawnEnemyBatch(int count)
    {
        if (currentWave == null) return;

        List<Vector2> spawnPositions = new List<Vector2>(count);

        // 현재 웨이브의 스폰 설정 가져오기
        SpawnSettings settings = currentWave.spawnSettings;

        // 적 스폰 위치 생성
        switch (settings.formation)
        {
            case SpawnFormation.Surround:
                spawnPositions = GenerateSurroundPositions(count, settings.surroundDistance, settings.angleOffset);
                break;
            case SpawnFormation.Rectangle:
                spawnPositions = GenerateRectanglePositions(count, settings.surroundDistance);
                break;
            case SpawnFormation.Line:
                spawnPositions = GenerateLinePositions(count, settings.lineStart, settings.lineEnd);
                break;
            case SpawnFormation.Fixed:
                spawnPositions = GetFixedSpawnPositions(count, settings.fixedSpawnPoints);
                break;
            case SpawnFormation.Random:
                spawnPositions = GenerateRandomPositions(count);
                break;
            case SpawnFormation.EdgeRandom:
            default:
                // 기존 방식 - 가장자리 랜덤
                for (int i = 0; i < count; i++)
                {
                    spawnPositions.Add(GetOptimizedSpawnPosition());
                }
                break;
        }

        // 경고 및 스폰 코루틴 시작
        StartCoroutine(ShowWarningsAndSpawnBatch(spawnPositions));
    }
    private IEnumerator ShowWarningsAndSpawnBatch(List<Vector2> positions)
    {
        // 스폰 설정에서 스폰 포인트당 적 수 가져오기
        int enemiesPerPoint = currentWave.spawnSettings.enemiesPerSpawnPoint;
        if (enemiesPerPoint <= 0) enemiesPerPoint = positions.Count; // 0이면 모든 적을 같은 위치에 스폰

        List<GameObject> warnings = new List<GameObject>();

        // 경고 표시 생성
        foreach (Vector2 pos in positions)
        {
            GameObject warning = ObjectPool.Instance.SpawnFromPool("SpawnWarning", pos, Quaternion.identity);
            warnings.Add(warning);
        }

        // 경고 표시 대기 시간
        yield return new WaitForSeconds(1f);

        // 경고 표시 비활성화
        foreach (GameObject warning in warnings)
        {
            ObjectPool.Instance.ReturnToPool("SpawnWarning", warning);
        }

        // 적 스폰
        int totalEnemies = positions.Count;
        int spawnedCount = 0;

        foreach (Vector2 pos in positions)
        {
            // 현재 위치에 스폰할 적 수 계산
            int enemiesToSpawn = Mathf.Min(enemiesPerPoint, totalEnemies - spawnedCount);

            // 이 위치에 적 스폰
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy(pos);
                spawnedCount++;
            }

            // 모든 적을 스폰했으면 종료
            if (spawnedCount >= totalEnemies)
                break;
        }
    }
    private void SpawnEnemy(Vector2 position)
    {
        if (!isWaveActive || isInSurvivalPhase) return;

        EnemyData enemyData = waveData.GetRandomEnemy(currentWave);
        if (enemyData == null)
        {
            Debug.LogWarning("Failed to get enemy data for spawning");
            return;
        }

        GameObject enemyObject = ObjectPool.Instance.SpawnFromPool(
            enemyData.enemyName,
            position,
            Quaternion.identity
        );

        if (enemyObject != null)
        {
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            EnemyAI enemyAI = enemyObject.GetComponent<EnemyAI>();

            if (enemy != null && enemyAI != null)
            {
                // 적 초기화
                enemy.SetEnemyData(enemyData);
                enemy.Initialize(GameManager.Instance.PlayerTransform);
                enemyAI.Initialize(GameManager.Instance.PlayerTransform);

                // 컬링 매니저 참조 설정
                EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();
                if (cullingManager != null)
                {
                    enemy.SetCullingManager(cullingManager);
                }

                // 활성화된 적 목록에 추가 (Enemy 컴포넌트 직접 저장)
                spawnedEnemies.Add(enemy);
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
        // 맵에서 가장자리 위치 가져오기
        Vector2 spawnPosition = gameMap.GetRandomEdgePosition();

        // 플레이어 위치
        Vector2 playerPos = playerStats.transform.position;

        // 플레이어와 거리 체크
        int maxAttempts = 5;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            float distance = Vector2.Distance(playerPos, spawnPosition);

            // 플레이어로부터 최소 거리를 만족하는지 확인
            if (distance >= minDistanceFromPlayer)
            {
                // 화면에 보이지 않는지 확인
                if (!IsPositionVisible(spawnPosition))
                {
                    break;
                }
            }

            // 다른 위치 시도
            spawnPosition = gameMap.GetRandomEdgePosition();
            attempts++;
        }

        return spawnPosition;
    }

    private bool IsPositionVisible(Vector2 position)
    {
        if (mainCamera == null) return false;

        Vector2 viewportPoint = mainCamera.WorldToViewportPoint(position);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    #region Spawn Formations
    // 원형 포위 위치 생성
    private List<Vector2> GenerateSurroundPositions(int count, float radius, float angleOffset)
    {
        List<Vector2> positions = new List<Vector2>(count);
        Vector2 playerPos = playerStats.transform.position;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep + angleOffset;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = playerPos + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;

            // 맵 경계 내에 있는지 확인하고 조정
            if (gameMap != null && !gameMap.IsPositionInMap(position))
            {
                position = gameMap.GetRandomEdgePosition();
            }

            positions.Add(position);
        }

        return positions;
    }
    // 사각형 포위 위치 생성
    private List<Vector2> GenerateRectanglePositions(int count, float distance)
    {
        List<Vector2> positions = new List<Vector2>(count);
        Vector2 playerPos = playerStats.transform.position;

        // 사각형의 네 변에 적들을 균등하게 배치
        int enemiesPerSide = Mathf.CeilToInt(count / 4f);
        int remainingEnemies = count;

        // 상단 변
        int topCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < topCount; i++)
        {
            float t = (topCount == 1) ? 0.5f : (float)i / (topCount - 1);
            float xPos = playerPos.x - distance + distance * 2 * t;
            float yPos = playerPos.y + distance;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= topCount;

        // 우측 변
        int rightCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < rightCount; i++)
        {
            float t = (rightCount == 1) ? 0.5f : (float)i / (rightCount - 1);
            float xPos = playerPos.x + distance;
            float yPos = playerPos.y + distance - distance * 2 * t;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= rightCount;

        // 하단 변
        int bottomCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < bottomCount; i++)
        {
            float t = (bottomCount == 1) ? 0.5f : (float)i / (bottomCount - 1);
            float xPos = playerPos.x + distance - distance * 2 * t;
            float yPos = playerPos.y - distance;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= bottomCount;

        // 좌측 변
        int leftCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < leftCount; i++)
        {
            float t = (leftCount == 1) ? 0.5f : (float)i / (leftCount - 1);
            float xPos = playerPos.x - distance;
            float yPos = playerPos.y - distance + distance * 2 * t;
            positions.Add(new Vector2(xPos, yPos));
        }

        // 맵 경계 확인 및 조정
        for (int i = 0; i < positions.Count; i++)
        {
            if (gameMap != null && !gameMap.IsPositionInMap(positions[i]))
            {
                positions[i] = gameMap.GetRandomEdgePosition();
            }
        }

        return positions;
    }
    // 직선 위치 생성
    private List<Vector2> GenerateLinePositions(int count, Vector2 start, Vector2 end)
    {
        List<Vector2> positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            Vector2 position = Vector2.Lerp(start, end, t);

            // 맵 내부 위치 조정
            if (gameMap != null && !gameMap.IsPositionInMap(position))
            {
                position = gameMap.GetRandomEdgePosition();
            }

            positions.Add(position);
        }

        return positions;
    }

    // 고정 스폰 포인트 사용
    private List<Vector2> GetFixedSpawnPositions(int count, List<int> fixedPoints)
    {
        List<Vector2> positions = new List<Vector2>(count);

        // 지정된 스폰 포인트가 없거나 맵이 없으면 랜덤 생성
        if (fixedPoints == null || fixedPoints.Count == 0 || gameMap == null)
        {
            for (int i = 0; i < count; i++)
            {
                positions.Add(gameMap.GetRandomEdgePosition());
            }
            return positions;
        }

        // 지정된 스폰 포인트 사용
        for (int i = 0; i < count; i++)
        {
            int pointIndex = fixedPoints[i % fixedPoints.Count];
            positions.Add(gameMap.GetSpawnPosition(pointIndex));
        }

        return positions;
    }
    // 맵 내부 랜덤 위치 생성
    private List<Vector2> GenerateRandomPositions(int count)
    {
        List<Vector2> positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            positions.Add(gameMap.GetRandomPositionInMap());
        }

        return positions;
    }
    #endregion

    private void HandlePlayerDeath()
    {
        isWaveActive = false;

        // 스폰 코루틴 중지
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void CompleteWave()
    {
        isWaveActive = false;
        isInSurvivalPhase = false;

        // 스폰 코루틴 중지
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // 남아있는 모든 적에게 9999 데미지 주기
        KillAllRemainingEnemies();

        // 웨이브 보상 지급
        if (playerStats != null && currentWave != null)
        {
            playerStats.AddCoins(currentWave.coinReward);
        }

        // 웨이브 완료 배너 표시 및 지연 후 상점 열기
        StartCoroutine(ShowCompletionBannerThenOpenShop());
    }

    private IEnumerator ShowCompletionBannerThenOpenShop()
    {
        // 배너 활성화
        if (waveCompleteBanner != null)
        {
            // 텍스트 설정
            if (waveCompleteText != null)
            {
                waveCompleteText.text = $"Wave {currentWaveNumber} Complete!";
            }

            // 배너 애니메이션 시작
            waveCompleteBanner.SetActive(true);

            // 배너가 표시될 시간 대기
            yield return new WaitForSeconds(2.0f);

            // 배너 숨기기
            waveCompleteBanner.SetActive(false);
        }

        // 게임 종료 체크
        if (AreAllWavesCompleted())
        {
            Debug.Log("Game Clear");
            //GameManager.Instance.HandleGameVictory();
        }
        else
        {
            
            GameManager.Instance.SetGameState(GameState.Paused);
            if (currentWaveNumber >= 1 && shopController != null)
            {
                shopController.isFirstShop = false;
            }
            // 상점 열기
            shopController.OpenShop();
        }
    }
    public bool AreAllWavesCompleted()
    {
        return waveData.GetNextWaveNumber(currentWaveNumber) < 0;
    }

    private void UpdateWaveUI()
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {currentWaveNumber}";
        }
    }
    private void KillAllRemainingEnemies()
    {
        // 살아있는 모든 적 한번에 데미지 주기
        const float massDeathDamage = 9999f;

        // 모든 Enemy 컴포넌트에 직접 접근 (GetComponent 호출 없음)
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeDamage(massDeathDamage);
            }
            else
            {
                // 이미 파괴된 적 제거
                spawnedEnemies.RemoveAt(i);
            }
        }
    }
    private void UpdateTimerUI()
    {
        // 문자열 생성
        string timerDisplay;

        if (isInSurvivalPhase)
        {
            // 생존 단계 - 남은 생존 시간 표시
            float remainingTime = currentWave.survivalDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(SURVIVAL_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }
        else
        {
            // 웨이브 단계 - 남은 웨이브 시간 표시
            float remainingTime = currentWave.waveDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(WAVE_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }

        // PlayerUIController에 있는 시간 표시와 동기화
        if (playerUIController != null)
        {
            playerUIController.SetExternalTimer(timerDisplay);
        }
    }

    private void CleanupDestroyedEnemies()
    {
        // 비활성화된 적 오브젝트 정리
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
    }
    public void EnsureInitialized(GameMap map)
    {
        if (map != null && gameMap == null)
        {
            gameMap = map;
            InitializeSystem();
        }
    }
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked -= StartNextWave;
        }

        // 코루틴 정리
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
}