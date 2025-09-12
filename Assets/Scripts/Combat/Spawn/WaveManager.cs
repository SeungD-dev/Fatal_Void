using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using static WaveData;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveData waveData;
    [SerializeField] private SpawnWarningController warningController;
    [SerializeField] private ShopController shopController;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private PlayerUIController playerUIController;
    [SerializeField] private GameObject warningPrefab; // ��� ������

    [Header("Wave UI")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private GameObject waveCompleteBanner;
    [SerializeField] private TextMeshProUGUI waveCompleteText;
    [SerializeField] private TextMeshProUGUI waveRewardText;

    [Header("Spawn Settings")]
    [SerializeField] private float minDistanceFromPlayer = 8f; // �÷��̾�κ��� �ּ� ���� �Ÿ�

    // ���̺� ����
    [SerializeField] private int _currentWaveNumber = 0;  // Serialized for inspector visibility
    private bool isWaveActive = false;
    private bool isInSurvivalPhase = false;
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private WaveData.Wave currentWave;
    private Coroutine spawnCoroutine;

    // Public access to current wave number
    public int currentWaveNumber => _currentWaveNumber;

    // ���̺� �Ϸ� �̺�Ʈ
    public System.Action OnWaveCompleted;
    public System.Action OnWaveStarted;

    // ĳ��
    private PlayerStats playerStats;
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private Camera mainCamera;
    private GameMap gameMap;
    
    // 보스 웨이브 관련
    private CalamityBoss currentBoss;
    private bool isBossWave = false;

    // ���ڿ� ĳ��
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

        
        ConnectWithShopController();
    }

    private void ConnectWithShopController()
    {
        
        if (shopController != null)
        {
            
            var waveManagerField = shopController.GetType().GetField("waveManager",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (waveManagerField != null)
            {
                waveManagerField.SetValue(shopController, this);
                Debug.Log("Connected WaveManager to ShopController");
            }
        }
        else
        {
            
            shopController = FindAnyObjectByType<ShopController>();
            if (shopController != null)
            {
                
                var waveManagerField = shopController.GetType().GetField("waveManager",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (waveManagerField != null)
                {
                    waveManagerField.SetValue(shopController, this);
                    Debug.Log("Connected WaveManager to found ShopController");
                }
            }
        }
    }

    private void ValidateReferences()
    {
        if (waveData == null)
        {
            Debug.LogError("WaveData is not assigned!");
            enabled = false;
            return;
        }
        
        if (playerUIController == null)
        {
            playerUIController = FindAnyObjectByType<PlayerUIController>();
        }
    }

    private void Start()
    {
        // �ʼ� �������� ��� �غ�� ������ ��ٸ��� �ڷ�ƾ ����
        StartCoroutine(WaitForDependencies());

        // ���������� �ʱ�ȭ�� �� �ִ� �۾� ���� ����
        InitializeWarningPool();

        // �κ��丮 ��Ʈ�ѷ��� ���� ��ư �̺�Ʈ ����
        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked += StartNextWave;
        }
    }

    private IEnumerator WaitForDependencies()
    {
        float timeOut = 5f;
        float elapsed = 0f;

        // GameManager ������ Ȯ��
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

        // PlayerStats ������ �ʱ�ȭ
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

        // MapManager ������ Ȯ��
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

        // �� �ε� ���
        yield return StartCoroutine(WaitForMapLoad());

        // ������ �̺�Ʈ ����
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        // PlayerUIController가 Inspector에서 할당되지 않은 경우에만 찾기
        if (playerUIController == null)
        {
            playerUIController = FindAnyObjectByType<PlayerUIController>();
        }

        
        if (GameManager.Instance != null)
        {
            
            var currentWaveProperty = GameManager.Instance.GetType().GetProperty("CurrentWave");
            if (currentWaveProperty != null)
            {
                currentWaveProperty.SetValue(GameManager.Instance, _currentWaveNumber);
            }
        }
    }

    private IEnumerator WaitForMapLoad()
    {
        float timeOut = 2f; // �� ª�� Ÿ�Ӿƿ� (5�ʡ�2��)
        float elapsed = 0f;

        // MapManager�� ���� ���� �ε�� ������ ��ٸ�
        while (MapManager.Instance.CurrentMap == null && elapsed < timeOut)
        {
            elapsed += 0.05f; // �� ª�� �������� üũ (0.1�ʡ�0.05��)
            yield return new WaitForSeconds(0.05f);
        }

        // �� ���� ��������
        gameMap = MapManager.Instance.CurrentMap;

        if (gameMap == null)
        {
            Debug.LogError("GameMap not found after timeout!");
            yield break;
        }

        // ���� �ε�� �Ŀ� ����Ǿ�� �ϴ� �ʱ�ȭ ����
        InitializeEnemyPools();
        SetupFirstWave();

        Debug.Log("WaveManager fully initialized with map reference");
    }

    private void InitializeSystem()
    {
        // ���� �ε�� �Ŀ� ����Ǿ�� �ϴ� �ʱ�ȭ ����
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
        
        // 보스 탄막용 ObjectPool 초기화
        InitializeBossPool();
    }
    
    private void InitializeBossPool()
    {
        if (ObjectPool.Instance == null) return;
        
        // BossBullet 풀 생성
        GameObject bossBulletPrefab = Resources.Load<GameObject>("Prefabs/Projectiles/Calamity_Projectile");
        if (bossBulletPrefab != null)
        {
            if (!ObjectPool.Instance.DoesPoolExist("BossBullet"))
            {
                ObjectPool.Instance.CreatePool("BossBullet", bossBulletPrefab, 50);
                Debug.Log("BossBullet ObjectPool created with 50 bullets");
            }
        }
        else
        {
            Debug.LogError("BossBullet prefab not found at Resources/Prefabs/Projectiles/Calamity_Projectile");
        }
    }

    private void InitializeEnemyPools()
    {
        // ��� ���̺꿡�� ���Ǵ� �� ���� ����
        HashSet<EnemyData> allEnemyTypes = new HashSet<EnemyData>();

        foreach (var wave in waveData.waves)
        {
            // 일반 적들 추가
            foreach (var enemy in wave.enemies)
            {
                if (enemy.enemyData != null)
                {
                    allEnemyTypes.Add(enemy.enemyData);
                }
            }
            
            // 보스는 ObjectPool을 사용하지 않으므로 제외
            // if (wave.isBossWave && wave.boss != null)
            // {
            //     allEnemyTypes.Add(wave.boss);
            // }
        }

        // �� �� ������ ���� Ǯ ����
        foreach (var enemyData in allEnemyTypes)
        {
            // 보스 타입인지 확인하여 올바른 프리팹 사용
            GameObject prefabToUse = (enemyData.enemyType == EnemyType.Boss && enemyData.bossPrefab != null) 
                ? enemyData.bossPrefab 
                : enemyData.enemyPrefab;
                
            if (prefabToUse != null)
            {
                // �̹� Ǯ�� �ִ��� Ȯ��
                if (!ObjectPool.Instance.DoesPoolExist(enemyData.enemyName))
                {
                    // �ø� �Ŵ��� ���� ���
                    EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();

                    // Enemy ������Ʈ �ʱ�ȭ
                    if (cullingManager != null)
                    {
                        Enemy enemyComponent = prefabToUse.GetComponent<Enemy>();
                        if (enemyComponent != null)
                        {
                            enemyComponent.SetCullingManager(cullingManager);
                        }
                    }

                    // Ǯ ����
                    ObjectPool.Instance.CreatePool(
                        enemyData.enemyName,
                        prefabToUse,
                        enemyData.initialPoolSize
                    );
                }
            }
        }
    }

    private void SetupFirstWave()
    {
        _currentWaveNumber = 1;
        currentWave = waveData.GetWave(_currentWaveNumber);
        if (currentWave != null)
        {
            UpdateWaveUI();
            UpdateGameManagerWaveNumber();
        }
        else
        {
            Debug.LogError("Failed to get first wave data!");
        }
    }

    
    private void UpdateGameManagerWaveNumber()
    {
        if (GameManager.Instance != null)
        {
            
            var currentWaveProperty = GameManager.Instance.GetType().GetProperty("CurrentWave");
            if (currentWaveProperty != null && currentWaveProperty.CanWrite)
            {
                currentWaveProperty.SetValue(GameManager.Instance, _currentWaveNumber);
            }
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // ���� �÷��� ������ ���� ���̺� ����
        if (newState == GameState.Playing)
        {
            // ������ ���۵Ǹ� ù ���̺� ����
            if (!isWaveActive && _currentWaveNumber == 1 && waveTimer == 0f)
            {
                StartWave(_currentWaveNumber);
            }
            else if (isWaveActive && spawnCoroutine == null)
            {
                // �Ͻ����� �� �簳 �� ���� �ڷ�ƾ �ٽ� ����
                spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());
            }
        }
        else if (newState == GameState.Paused || newState == GameState.GameOver)
        {
            // �Ͻ������� ���ӿ��� �� ���� �ڷ�ƾ ����
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

        // 보스 웨이브인 경우 시간 체크 안함 (보스 처치 시에만 완료)
        if (isBossWave)
        {
            // 보스가 살아있는지만 체크하고 타이머는 업데이트하지 않음
            return;
        }

        // Ÿ�̸� ������Ʈ (일반 웨이브만)
        waveTimer += Time.deltaTime;

        // Ÿ�̸� UI ������Ʈ
        UpdateTimerUI();

        // ���̺� �ܰ� ���� (일반 웨이브만)
        if (!isInSurvivalPhase && waveTimer >= currentWave.waveDuration)
        {
            // ���̺� �ð� ���� - ���� �ܰ� ����
            isInSurvivalPhase = true;
            waveTimer = 0f; // Ÿ�̸� ����

            // ���� �ڷ�ƾ ����
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
        else if (isInSurvivalPhase && waveTimer >= currentWave.survivalDuration)
        {
            // ���� �ܰ� �Ϸ� - ���̺� Ŭ����
            CompleteWave();
        }

        // �ֱ������� �ı��� �� ����
        if (Time.frameCount % 60 == 0) // �� 1�ʸ���
        {
            CleanupDestroyedEnemies();
        }
    }

    public void StartNextWave()
    {
        int nextWaveNumber;
        if (_currentWaveNumber == 0) 
        {
            nextWaveNumber = 1;
        }
        else
        {
            nextWaveNumber = waveData.GetNextWaveNumber(_currentWaveNumber);
        }

        
        if (nextWaveNumber > 0)
        {
            
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

        // ���̺� ���� ����
        _currentWaveNumber = waveNumber;
        waveTimer = 0f;
        isWaveActive = true;
        isInSurvivalPhase = false;

        
        UpdateGameManagerWaveNumber();

        // UI ������Ʈ
        UpdateWaveUI();

        // 웨이브 시작 이벤트 발생
        OnWaveStarted?.Invoke();

        // 보스 웨이브인지 체크
        isBossWave = currentWave.isBossWave;
        
        
        if (isBossWave)
        {
            // 보스 경고 시퀀스 시작
            StartBossWarningSequence();
        }
        else
        {
            // 일반 적 스폰 코루틴 시작
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());
        }

        // ���� ���� �÷��̷� ����
        GameManager.Instance.SetGameState(GameState.Playing);
    }
    
    private void StartBossWarningSequence()
    {
        // PlayerUIController가 없다면 다시 찾기 시도
        if (playerUIController == null)
        {
            playerUIController = FindAnyObjectByType<PlayerUIController>();
        }
        
        if (playerUIController == null)
        {
            Debug.LogError("PlayerUIController를 찾을 수 없습니다! Inspector에서 직접 할당하거나 씬에 PlayerUIController가 있는지 확인하세요.");
            // 보스 경고 없이 바로 스폰
            SpawnBoss();
            return;
        }

        Debug.Log("보스 경고 시퀀스 시작");
        // PlayerUIController의 보스 경고 시퀀스 시작 (완료 시 보스 스폰)
        playerUIController.StartBossWarningSequence(SpawnBoss);
    }

    private void SpawnBoss()
    {
        if (currentWave == null)
        {
            Debug.LogError("currentWave가 null입니다!");
            return;
        }
        
        if (currentWave.boss == null)
        {
            Debug.LogError("Boss wave에 boss EnemyData가 설정되지 않았습니다!");
            return;
        }
        
        // 보스 스폰 위치 (고정 위치)
        Vector3 bossSpawnPosition = new Vector3(0, 6.5f, 0);
        
        // 보스 프리팹 확인
        GameObject bossPrefab = currentWave.boss.bossPrefab;
        if (bossPrefab == null)
        {
            Debug.LogError("Boss EnemyData에 bossPrefab이 설정되지 않았습니다!");
            return;
        }
        
        // 보스 직접 생성 (ObjectPool 사용 안함)
        GameObject spawnedBoss = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
        if (spawnedBoss != null)
        {
            currentBoss = spawnedBoss.GetComponent<CalamityBoss>();
            if (currentBoss != null)
            {
                // 보스 데이터 먼저 설정
                var enemyComponent = currentBoss.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.SetEnemyData(currentWave.boss);
                    enemyComponent.Initialize(GameManager.Instance.PlayerTransform);
                    
                    // 보스는 컬링에서 제외되므로 컬링 매니저 설정하지 않음
                }
                
                // CalamityBoss 전용 초기화 (웨이브 데이터와 참조들 전달)
                currentBoss.InitializeBoss(
                    currentWave,
                    ObjectPool.Instance,
                    GameManager.Instance.PlayerTransform,
                    this
                );
                
                // 보스 활성화 (OnObjectSpawn 호출)
                currentBoss.OnObjectSpawn();
                
                Debug.Log($"Boss {currentWave.boss.enemyName} spawned and initialized for wave {_currentWaveNumber}");
            }
            else
            {
                Debug.LogError("Spawned boss does not have CalamityBoss component!");
            }
        }
        else
        {
            Debug.LogError("Failed to instantiate boss prefab!");
        }
    }
    
    public void OnBossDefeated()
    {
        if (isBossWave && currentBoss != null)
        {
            Debug.Log("Boss defeated! Completing wave...");
            currentBoss = null;
            
            // 보스 웨이브 즉시 완료
            CompleteWave();
        }
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        // ���� Ÿ�̸� �ʱ�ȭ
        spawnTimer = 0f;

        // ���̺� Ȱ��ȭ ���� �� ���� �ܰ谡 �ƴ� ���� ����
        while (isWaveActive && !isInSurvivalPhase)
        {
            // ������ �÷��� ������ ���� ����
            if (GameManager.Instance.currentGameState == GameState.Playing)
            {
                spawnTimer += Time.deltaTime;

                // ���� �ð��� �Ǿ��� ��
                if (spawnTimer >= currentWave.spawnInterval)
                {
                    // �� ����
                    SpawnEnemyBatch(currentWave.spawnAmount);
                    spawnTimer = 0f;
                }

                // ���̺� �ð��� ����Ǿ����� Ȯ��
                if (waveTimer >= currentWave.waveDuration)
                {
                    break; // ���� �ߴ�
                }
            }

            yield return null;
        }

        spawnCoroutine = null;
    }

    private void SpawnEnemyBatch(int count)
    {
        if (currentWave == null) return;
        
        // 보스 웨이브에서는 일반 적 스폰하지 않음
        if (isBossWave) return;

        List<Vector2> spawnPositions = new List<Vector2>(count);

        // ���� ���̺��� ���� ���� ��������
        SpawnSettings settings = currentWave.spawnSettings;

        // �� ���� ��ġ ����
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
                // ���� ��� - �����ڸ� ����
                for (int i = 0; i < count; i++)
                {
                    spawnPositions.Add(GetOptimizedSpawnPosition());
                }
                break;
        }

        // ��� �� ���� �ڷ�ƾ ����
        StartCoroutine(ShowWarningsAndSpawnBatch(spawnPositions));
    }

    private IEnumerator ShowWarningsAndSpawnBatch(List<Vector2> positions)
    {
        // ���� �������� ���� ����Ʈ�� �� �� ��������
        int enemiesPerPoint = currentWave.spawnSettings.enemiesPerSpawnPoint;
        if (enemiesPerPoint <= 0) enemiesPerPoint = positions.Count; // 0�̸� ��� ���� ���� ��ġ�� ����

        List<GameObject> warnings = new List<GameObject>();

        // ��� ǥ�� ����
        foreach (Vector2 pos in positions)
        {
            GameObject warning = ObjectPool.Instance.SpawnFromPool("SpawnWarning", pos, Quaternion.identity);
            warnings.Add(warning);
        }

        // ��� ǥ�� ��� �ð�
        yield return new WaitForSeconds(1f);

        // ��� ǥ�� ��Ȱ��ȭ
        foreach (GameObject warning in warnings)
        {
            ObjectPool.Instance.ReturnToPool("SpawnWarning", warning);
        }

        // �� ����
        int totalEnemies = positions.Count;
        int spawnedCount = 0;

        foreach (Vector2 pos in positions)
        {
            // ���� ��ġ�� ������ �� �� ���
            int enemiesToSpawn = Mathf.Min(enemiesPerPoint, totalEnemies - spawnedCount);

            // �� ��ġ�� �� ����
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy(pos);
                spawnedCount++;
            }

            // ��� ���� ���������� ����
            if (spawnedCount >= totalEnemies)
                break;
        }
    }

    private void SpawnEnemy(Vector2 position)
    {
        if (!isWaveActive || isInSurvivalPhase) return;
        
        // 보스 웨이브에서는 일반 적 스폰하지 않음
        if (isBossWave) return;

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
                // �� �ʱ�ȭ
                enemy.SetEnemyData(enemyData);
                enemy.Initialize(GameManager.Instance.PlayerTransform);
                enemyAI.Initialize(GameManager.Instance.PlayerTransform);

                // �ø� �Ŵ��� ���� ����
                EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();
                if (cullingManager != null)
                {
                    enemy.SetCullingManager(cullingManager);
                }

                // Ȱ��ȭ�� �� ��Ͽ� �߰� (Enemy ������Ʈ ���� ����)
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
        // �ʿ��� �����ڸ� ��ġ ��������
        Vector2 spawnPosition = gameMap.GetRandomEdgePosition();

        // �÷��̾� ��ġ
        Vector2 playerPos = playerStats.transform.position;

        // �÷��̾�� �Ÿ� üũ
        int maxAttempts = 5;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            float distance = Vector2.Distance(playerPos, spawnPosition);

            // �÷��̾�κ��� �ּ� �Ÿ��� �����ϴ��� Ȯ��
            if (distance >= minDistanceFromPlayer)
            {
                // ȭ�鿡 ������ �ʴ��� Ȯ��
                if (!IsPositionVisible(spawnPosition))
                {
                    break;
                }
            }

            // �ٸ� ��ġ �õ�
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
    // ���� ���� ��ġ ����
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

            // �� ��� ���� �ִ��� Ȯ���ϰ� ����
            if (gameMap != null && !gameMap.IsPositionInMap(position))
            {
                position = gameMap.GetRandomEdgePosition();
            }

            positions.Add(position);
        }

        return positions;
    }

    // �簢�� ���� ��ġ ����
    private List<Vector2> GenerateRectanglePositions(int count, float distance)
    {
        List<Vector2> positions = new List<Vector2>(count);
        Vector2 playerPos = playerStats.transform.position;

        // �簢���� �� ���� ������ �յ��ϰ� ��ġ
        int enemiesPerSide = Mathf.CeilToInt(count / 4f);
        int remainingEnemies = count;

        // ��� ��
        int topCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < topCount; i++)
        {
            float t = (topCount == 1) ? 0.5f : (float)i / (topCount - 1);
            float xPos = playerPos.x - distance + distance * 2 * t;
            float yPos = playerPos.y + distance;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= topCount;

        // ���� ��
        int rightCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < rightCount; i++)
        {
            float t = (rightCount == 1) ? 0.5f : (float)i / (rightCount - 1);
            float xPos = playerPos.x + distance;
            float yPos = playerPos.y + distance - distance * 2 * t;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= rightCount;

        // �ϴ� ��
        int bottomCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < bottomCount; i++)
        {
            float t = (bottomCount == 1) ? 0.5f : (float)i / (bottomCount - 1);
            float xPos = playerPos.x + distance - distance * 2 * t;
            float yPos = playerPos.y - distance;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= bottomCount;

        // ���� ��
        int leftCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < leftCount; i++)
        {
            float t = (leftCount == 1) ? 0.5f : (float)i / (leftCount - 1);
            float xPos = playerPos.x - distance;
            float yPos = playerPos.y - distance + distance * 2 * t;
            positions.Add(new Vector2(xPos, yPos));
        }

        // �� ��� Ȯ�� �� ����
        for (int i = 0; i < positions.Count; i++)
        {
            if (gameMap != null && !gameMap.IsPositionInMap(positions[i]))
            {
                positions[i] = gameMap.GetRandomEdgePosition();
            }
        }

        return positions;
    }

    // ���� ��ġ ����
    private List<Vector2> GenerateLinePositions(int count, Vector2 start, Vector2 end)
    {
        List<Vector2> positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            Vector2 position = Vector2.Lerp(start, end, t);

            // �� ���� ��ġ ����
            if (gameMap != null && !gameMap.IsPositionInMap(position))
            {
                position = gameMap.GetRandomEdgePosition();
            }

            positions.Add(position);
        }

        return positions;
    }

    // ���� ���� ����Ʈ ���
    private List<Vector2> GetFixedSpawnPositions(int count, List<int> fixedPoints)
    {
        List<Vector2> positions = new List<Vector2>(count);

        // ������ ���� ����Ʈ�� ���ų� ���� ������ ���� ����
        if (fixedPoints == null || fixedPoints.Count == 0 || gameMap == null)
        {
            for (int i = 0; i < count; i++)
            {
                positions.Add(gameMap.GetRandomEdgePosition());
            }
            return positions;
        }

        // ������ ���� ����Ʈ ���
        for (int i = 0; i < count; i++)
        {
            int pointIndex = fixedPoints[i % fixedPoints.Count];
            positions.Add(gameMap.GetSpawnPosition(pointIndex));
        }

        return positions;
    }

    // �� ���� ���� ��ġ ����
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

        // ���� �ڷ�ƾ ����
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

        // ���� �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // �����ִ� ��� ������ 9999 ������ �ֱ�
        KillAllRemainingEnemies();

        // ���̺� ���� ����
        if (playerStats != null && currentWave != null)
        {
            playerStats.AddCoins(currentWave.coinReward);
        }

        // Update GameManager's wave number
        UpdateGameManagerWaveNumber();

        // ���̺� �Ϸ� �̺�Ʈ �߻� (EnhancedWeaponManager���� ó��)
        OnWaveCompleted?.Invoke();

        // EnhancedWeaponManager���� ó���ϵ��� �ϰ�, ���⼭�� ���� ���� ó�� ����
    }

    private IEnumerator ShowCompletionBannerThenOpenShop()
    {
        // ��� Ȱ��ȭ
        if (waveCompleteBanner != null)
        {
            // �ؽ�Ʈ ����
            if (waveCompleteText != null)
            {
                waveCompleteText.text = $"Wave {_currentWaveNumber} Complete!";
            }
            
            // 리워드 텍스트 설정
            if (waveRewardText != null && currentWave != null)
            {
                waveRewardText.text = $"Reward: {currentWave.coinReward}";
            }

            // ��� �ִϸ��̼� ����
            waveCompleteBanner.SetActive(true);

            // ��ʰ� ǥ�õ� �ð� ���
            yield return new WaitForSeconds(2.0f);

            // ��� �����
            waveCompleteBanner.SetActive(false);
        }

        // ���� ���� üũ
        if (AreAllWavesCompleted())
        {
            Debug.Log("Game Clear");
            //GameManager.Instance.HandleGameVictory();
        }
        else
        {
            GameManager.Instance.SetGameState(GameState.Paused);
            if (_currentWaveNumber >= 1 && shopController != null)
            {
                shopController.isFirstShop = false;
            }
            // ���� ����
            shopController.OpenShop();
        }
    }

    public bool AreAllWavesCompleted()
    {
        return waveData.GetNextWaveNumber(_currentWaveNumber) < 0;
    }

    /// <summary>
    /// EnhancedWeaponManager���� ȣ���ϴ� ����/��� ǥ�� �� ���� ���� �޼���
    /// </summary>
    public void ShowBannerAndOpenShop()
    {
        StartCoroutine(ShowCompletionBannerThenOpenShop());
    }

    private void UpdateWaveUI()
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {_currentWaveNumber}";
        }
    }

    private void KillAllRemainingEnemies()
    {
        // ����ִ� ��� �� �ѹ��� ������ �ֱ�
        const float massDeathDamage = 9999f;

        // ��� Enemy ������Ʈ�� ���� ���� (GetComponent ȣ�� ����)
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeDamage(massDeathDamage);
            }
            else
            {
                // �̹� �ı��� �� ����
                spawnedEnemies.RemoveAt(i);
            }
        }
    }

    private void UpdateTimerUI()
    {
        // ���ڿ� ����
        string timerDisplay;

        if (isInSurvivalPhase)
        {
            // ���� �ܰ� - ���� ���� �ð� ǥ��
            float remainingTime = currentWave.survivalDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(SURVIVAL_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }
        else
        {
            // ���̺� �ܰ� - ���� ���̺� �ð� ǥ��
            float remainingTime = currentWave.waveDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(WAVE_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }

        // PlayerUIController�� �ִ� �ð� ǥ�ÿ� ����ȭ
        if (playerUIController != null)
        {
            playerUIController.SetExternalTimer(timerDisplay);
        }
    }

    private void CleanupDestroyedEnemies()
    {
        // ��Ȱ��ȭ�� �� ������Ʈ ����
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
        // �̺�Ʈ ���� ����
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

        // �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
}