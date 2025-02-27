using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 상태와 시스템을 관리하는 매니저 클래스
/// </summary>
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager instance;
    private static readonly object _lock = new object();

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        var go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                    }
                }
            }
            return instance;
        }
    }

    #region Properties
    public GameState currentGameState { get; private set; }
    public Dictionary<GameState, int> gameScene { get; private set; }

    private PlayerStats playerStats;
    private ShopController shopController;
    private CombatController combatController;
    private GameOverController gameOverController;

    // 자주 사용되는 속성들을 캐싱
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;
    public GameOverController GameOverController => gameOverController;

    public event System.Action<GameState> OnGameStateChanged;

    // 로딩 관련 이벤트
    public event System.Action OnLoadingCompleted;
    public event System.Action OnLoadingCancelled;

    // 자주 사용되는 WaitForSeconds 캐싱
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds ResourceLoadDelay = new WaitForSeconds(0.02f);
    #endregion

    // 로딩 시스템 관련 속성
    [Header("로딩 설정")]
    [SerializeField] private float minimumLoadingTime = 1.5f;
    [SerializeField] private bool aggressiveMemoryOptimization = true;
    public float LoadingProgress { get; private set; }

    // 리소스 캐싱 및 로딩 상태 제어
    private bool isLoadingCancelled = false;
    private AsyncOperation currentSceneLoadOperation;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameScenes();
            InitializeSound();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 씬 정보를 초기화합니다.
    /// </summary>
    private void InitializeGameScenes()
    {
        gameScene = new Dictionary<GameState, int>(5) // 초기 용량 지정으로 재할당 방지
        {
            { GameState.MainMenu, 0 },    // StartScene
            { GameState.Loading, 1 },     // LoadingScene
            { GameState.Playing, 2 },     // CombatScene
            { GameState.Paused, 2 },      // 같은 CombatScene에서 Pause
            { GameState.GameOver, 2 }     // 같은 CombatScene에서 GameOver
        };
    }

    /// <summary>
    /// 사운드 시스템을 초기화합니다.
    /// </summary>
    private void InitializeSound()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.LoadSoundBank("IntroSoundBank");
        }
    }

    /// <summary>
    /// 전투용 사운드 시스템을 초기화합니다.
    /// </summary>
    private void InitializeCombatSound()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.LoadSoundBank("CombatSoundBank");
        }
    }

    /// <summary>
    /// CombatScene의 주요 컴포넌트들을 설정합니다.
    /// </summary>
    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat, GameOverController gameOver)
    {
        bool shouldInitialize = !isInitialized && stats != null;

        playerStats = stats;
        shopController = shop;
        combatController = combat;
        gameOverController = gameOver;

        if (shouldInitialize)
        {
            playerStats.InitializeStats();
            isInitialized = true;
        }
    }

    /// <summary>
    /// 씬 전환 시 참조를 초기화합니다.
    /// </summary>
    public void ClearSceneReferences()
    {
        playerStats = null;
        shopController = null;
        combatController = null;
        gameOverController = null;
        isInitialized = false;
    }

    /// <summary>
    /// 게임을 시작하고 로딩 화면으로 전환합니다.
    /// </summary>
    public void StartGame()
    {
        // 로딩 진행 상태 초기화
        LoadingProgress = 0f;
        isLoadingCancelled = false;

        // 메모리 최적화 (선택적)
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // 로딩 상태로 전환
        SetGameState(GameState.Loading);

        // 로딩 씬으로 전환
        int loadingSceneIndex;
        if (gameScene.TryGetValue(GameState.Loading, out loadingSceneIndex))
        {
            SceneManager.LoadScene(loadingSceneIndex);
        }
    }

    /// <summary>
    /// 로딩 프로세스를 시작합니다. 로딩 씬에서 호출됩니다.
    /// </summary>
    public void StartLoadingProcess()
    {
        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        float startTime = Time.time;

        // 1. 초기화 작업 수행
        yield return StartCoroutine(PerformInitializationSteps());

        if (isLoadingCancelled)
        {
            OnLoadingCancelled?.Invoke();
            yield break;
        }

        // 2. 전투 씬 비동기 로드
        int combatSceneIndex;
        if (!gameScene.TryGetValue(GameState.Playing, out combatSceneIndex))
        {
            combatSceneIndex = 2; // 기본값
        }

        currentSceneLoadOperation = SceneManager.LoadSceneAsync(combatSceneIndex);
        currentSceneLoadOperation.allowSceneActivation = false; // 로딩이 완료되어도 바로 활성화하지 않음

        // 씬 로딩 진행률 업데이트 (90% -> 100%)
        while (currentSceneLoadOperation.progress < 0.9f)
        {
            LoadingProgress = 0.9f + (currentSceneLoadOperation.progress / 10f);
            yield return null;

            if (isLoadingCancelled)
            {
                OnLoadingCancelled?.Invoke();
                yield break;
            }
        }

        // 3. 최소 로딩 시간 보장
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // 4. 로딩 완료
        LoadingProgress = 1.0f;

        // 로딩 완료 이벤트 발생
        OnLoadingCompleted?.Invoke();

        // 5. 씬 활성화
        SetGameState(GameState.Playing);
        currentSceneLoadOperation.allowSceneActivation = true;

        // 6. 게임 시작 음악 재생
        var soundManager = SoundManager.Instance;
        if (soundManager != null && !soundManager.IsBGMPlaying("BGM_Battle"))
        {
            soundManager.LoadSoundBank("CombatSoundBank");
            soundManager.PlaySound("BGM_Battle", 1f, true);
        }
    }

    /// <summary>
    /// 로딩을 취소합니다.
    /// </summary>
    public void CancelLoading()
    {
        isLoadingCancelled = true;

        // 씬 로드 작업 취소 (가능한 경우)
        if (currentSceneLoadOperation != null && !currentSceneLoadOperation.isDone)
        {
            // Unity는 직접적으로 AsyncOperation을 취소할 방법을 제공하지 않음
            // 대신 메인 메뉴로 돌아갈 때 메모리 정리
            if (aggressiveMemoryOptimization)
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
        }
    }

    // 모든 초기화 단계를 수행하는 코루틴
    private IEnumerator PerformInitializationSteps()
    {
        // 1. 사운드 시스템 초기화 (0% -> 10%)
        InitializeCombatSound(); // 전투용 사운드 준비
        LoadingProgress = 0.1f;
        yield return InitializationDelay;

        if (isLoadingCancelled) yield break;

        // 2. 오브젝트 풀 초기화 (10% -> 25%)
        yield return InitializeObjectPools();
        LoadingProgress = 0.25f;

        if (isLoadingCancelled) yield break;

        // 3. 게임 리소스 로드 (25% -> 50%)
        yield return PreloadGameResources();
        LoadingProgress = 0.5f;

        if (isLoadingCancelled) yield break;

        // 4. 전투 시스템 준비 (50% -> 75%)
        yield return PrepareCombatSystem();
        LoadingProgress = 0.75f;

        if (isLoadingCancelled) yield break;

        // 5. 최종 준비 (75% -> 90%)
        yield return FinalizeInitialization();
        LoadingProgress = 0.9f;
    }

    private IEnumerator InitializeObjectPools()
    {
        // ObjectPool이 이미 존재하는지 확인
        if (ObjectPool.Instance == null)
        {
            GameObject poolObject = new GameObject("ObjectPool");
            poolObject.AddComponent<ObjectPool>();
            DontDestroyOnLoad(poolObject);
        }

        // 기본 풀 초기화
        ObjectPool.Instance.CreatePool("BulletDestroyVFX", Resources.Load<GameObject>("Prefabs/VFX/BulletDestroyVFX"), 30);
        LoadingProgress = 0.15f;
        yield return ResourceLoadDelay;

        // 주요 무기 풀 초기화
        string[] weaponTypes = { "Buster", "Machinegun", "BeamSaber", "Shotgun", "Cutter", "Sawblade", "Grinder", "ForceField" };
        for (int i = 0; i < weaponTypes.Length; i++)
        {
            string poolName = $"{weaponTypes[i]}_Projectile";
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Weapons/Projectile{poolName}");
            if (prefab != null)
            {
                int poolSize = GetOptimalPoolSize(weaponTypes[i]);
                ObjectPool.Instance.CreatePool(poolName, prefab, poolSize);
            }

            // 진행률 업데이트
            LoadingProgress = 0.15f + (0.1f * (i + 1) / weaponTypes.Length);
            yield return ResourceLoadDelay;

            if (isLoadingCancelled) yield break;
        }
    }

    private int GetOptimalPoolSize(string weaponType)
    {
        // 무기 타입에 따라 적절한 풀 크기 반환
        switch (weaponType)
        {
            case "Machinegun": return 50;
            case "Shotgun": return 20;
            case "Buster": return 15;
            case "Cutter": return 20;
            case "Sawblade": return 10;
            case "BeamSaber": return 12;
            case "Grinder": return 12;
            case "ForceField": return 4;
            default: return 15;
        }
    }

    private IEnumerator PreloadGameResources()
    {
        // 적 프리팹 로드
        string[] enemyTypes = { "Walker", "Hunter", "Heavy"};
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            GameObject enemyPrefab = Resources.Load<GameObject>($"Prefabs/Characters/{enemyTypes[i]}");
            if (enemyPrefab != null)
            {
                ObjectPool.Instance.CreatePool(enemyTypes[i], enemyPrefab, 15);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.35f;

        if (isLoadingCancelled) yield break;

        // 아이템 프리팹 로드
        string[] itemTypes = {
            "ExperienceSmall", "ExperienceMedium", "ExperienceLarge",
            "Coin", "Potion", "Magnet"
        };

        for (int i = 0; i < itemTypes.Length; i++)
        {
            GameObject itemPrefab = Resources.Load<GameObject>($"Prefabs/Collectibles/{itemTypes[i]}");
            if (itemPrefab != null)
            {
                int poolSize = (itemTypes[i].Contains("Experience")) ? 30 : 10;
                ObjectPool.Instance.CreatePool(itemTypes[i], itemPrefab, poolSize);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.45f;

        // VFX 로드
        string[] vfxTypes = { "HitVFX", "ExplosionVFX", "LevelUpVFX", "PickupVFX" };
        for (int i = 0; i < vfxTypes.Length; i++)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>($"Prefabs/VFX/{vfxTypes[i]}");
            if (vfxPrefab != null)
            {
                ObjectPool.Instance.CreatePool(vfxTypes[i], vfxPrefab, 10);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.5f;
    }

    private IEnumerator PrepareCombatSystem()
    {
        // SpawnSettings 로드
        var spawnSettings = Resources.Load<ScriptableObject>("Data/SpawnSettings");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.6f;

        // 데이터베이스 로드
        var weaponDatabase = Resources.Load<ScriptableObject>("Data/WeaponDatabase");
        var enemySpawnDatabase = Resources.Load<ScriptableObject>("Data/EnemySpawnDatabase");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.7f;

        // 기타 필요한 데이터 로드
        yield return ResourceLoadDelay;
        LoadingProgress = 0.75f;
    }

    private IEnumerator FinalizeInitialization()
    {
        // 메모리 최적화
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // 초기화 완료 대기
        yield return InitializationDelay;
    }

    /// <summary>
    /// 게임의 상태를 변경하고 관련 시스템을 업데이트합니다.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        GameState previousState = currentGameState;
        currentGameState = newState;

        // 상태 변경 전에 필요한 준비 작업
        PrepareForStateTransition(previousState, newState);

        // 이벤트 발생
        OnGameStateChanged?.Invoke(newState);

        // 상태에 따른 게임 설정 변경
        switch (newState)
        {
            case GameState.MainMenu:
            case GameState.Loading:
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f;
                if (newState == GameState.GameOver)
                {
                    HandleGameOver();
                }
                break;
        }
    }

    /// <summary>
    /// 상태 전환 전 필요한 준비 작업을 수행합니다.
    /// </summary>
    private void PrepareForStateTransition(GameState previousState, GameState newState)
    {
        // 예: 메인 메뉴에서 로딩으로 전환 시
        if (previousState == GameState.MainMenu && newState == GameState.Loading)
        {
            // 메모리 정리 등의 작업이 필요한 경우
        }
    }

    /// <summary>
    /// 게임 오버 상태에서의 처리를 담당합니다.
    /// </summary>
    private void HandleGameOver()
    {
        SavePlayerProgress();

        // 게임 오버 효과음 재생
        SoundManager.Instance?.PlaySound("GameOver_sfx", 1f, false);

        if (gameOverController != null)
        {
            gameOverController.ShowGameOverPanel();
        }
        else
        {
            Debug.LogError("GameOverController reference is missing!");
        }
    }

    public bool IsPlaying() => currentGameState == GameState.Playing;

    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 앱이 백그라운드로 갈 때 (pauseStatus == true)
        if (pauseStatus && IsPlaying())
        {
            // 진행 상황 자동 저장
            SavePlayerProgress();
        }
    }

    /// <summary>
    /// 플레이어의 진행 상황을 저장합니다.
    /// </summary>
    public void SavePlayerProgress()
    {
        // 현재는 SoundManager가 자체적으로 볼륨 설정을 저장
        // 추가적인 저장 로직이 필요하면 여기에 구현
    }
}
