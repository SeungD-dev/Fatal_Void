using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// ������ �������� ���¿� �ý����� �����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class GameManager : MonoBehaviour
{
    // �̱��� �ν��Ͻ�
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
    public Transform PlayerTransform { get; private set; }

    private PlayerStats playerStats;
    private ShopController shopController;
    private CombatController combatController;
    private GameOverController gameOverController;
    private PhysicsInventoryManager physicsInventoryManager; // �߰�: ���� �κ��丮 �ý��� ����
    [SerializeField] private int _currentWave = 0;
    public int CurrentWave
    {
        get { return _currentWave; }
        set { _currentWave = value; }
    }


    // ���� ���Ǵ� �Ӽ����� ĳ��
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;
    public GameOverController GameOverController => gameOverController;
    public PhysicsInventoryManager PhysicsInventoryManager => physicsInventoryManager; // �߰�: ���� �κ��丮 �Ŵ��� ������Ƽ

    public event System.Action<GameState> OnGameStateChanged;

    // �ε� ���� �̺�Ʈ
    public event System.Action OnLoadingCompleted;
    public event System.Action OnLoadingCancelled;

    // ���� ���Ǵ� WaitForSeconds ĳ��
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds ResourceLoadDelay = new WaitForSeconds(0.02f);
    #endregion

    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    public GameObject OptionPanel => optionPanel;
    // �ε� �ý��� ���� �Ӽ�
    [Header("�ε� ����")]
    [SerializeField] private float minimumLoadingTime = 1.5f;
    [SerializeField] private bool aggressiveMemoryOptimization = true;
    public float LoadingProgress { get; private set; }

    // ���ҽ� ĳ�� �� �ε� ���� ����
    private bool isLoadingCancelled = false;
    private AsyncOperation currentSceneLoadOperation;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        DOTween.SetTweensCapacity(500, 125);

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
    /// ���� �� ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeGameScenes()
    {
        gameScene = new Dictionary<GameState, int>(5) // �ʱ� �뷮 �������� ���Ҵ� ����
        {
            {GameState.Intro, 0 },
            { GameState.MainMenu, 1 },    // TitleScene
            { GameState.Loading, 2 },     // LoadingScene
            { GameState.Playing, 3 },     // CombatScene
            { GameState.Paused, 3 },      // ���� CombatScene���� Pause
            { GameState.GameOver, 3 }     // ���� CombatScene���� GameOver
        };
    }
    public void StartApplication()
    {
        // ù ���� Ȯ�� ���� �׻� ��Ʈ�ξ����� �̵�
        SetGameState(GameState.Intro);
        LoadIntroScene();
    }

    private void LoadIntroScene()
    {
        int introSceneIndex;
        if (gameScene.TryGetValue(GameState.Intro, out introSceneIndex))
        {
            SceneManager.LoadScene(introSceneIndex);
        }
        else
        {
            Debug.LogError("��Ʈ�ξ� �ε����� ã�� �� �����ϴ�!");
            // ����: ���� �޴��� �̵�
            SetGameState(GameState.MainMenu);
            LoadMainMenuScene();
        }
    }

    public void CompleteIntro()
    {
        LoadMainMenuScene();
    }

    private void LoadMainMenuScene()
    {
        int mainMenuSceneIndex;
        if (gameScene.TryGetValue(GameState.MainMenu, out mainMenuSceneIndex))
        {
            SetGameState(GameState.MainMenu);
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }
    /// <summary>
    /// ���� �ý����� �ʱ�ȭ�մϴ�.
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
    /// ������ ���� �ý����� �ʱ�ȭ�մϴ�.
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
    /// CombatScene�� �ֿ� ������Ʈ���� �����մϴ�.
    /// </summary>
    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat, GameOverController gameOver,GameObject optionPanelRef)
    {
        bool shouldInitialize = !isInitialized && stats != null;

        playerStats = stats;
        shopController = shop;
        combatController = combat;
        gameOverController = gameOver;
        optionPanel = optionPanelRef;
        // �÷��̾� Transform ĳ��
        if (stats != null)
        {
            PlayerTransform = stats.transform;

            // ������Ʈ Ǯ�� �÷��̾� ���� ����
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.SetPlayerReference(PlayerTransform);
            }

            // �ø� �Ŵ��� ã�Ƽ� �÷��̾� ���� ����
            EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();
            if (cullingManager != null)
            {
                cullingManager.SetPlayerReference(PlayerTransform);
            }
        }

        // ���� �κ��丮 �Ŵ��� ���� ���� (�߰�)
        if (shop != null)
        {
            var inventoryController = shop.GetComponentInChildren<InventoryController>();
            if (inventoryController != null)
            {
                physicsInventoryManager = inventoryController.GetComponent<PhysicsInventoryManager>();
                if (physicsInventoryManager == null)
                {
                    // PhysicsInventoryManager�� ������ ����
                    physicsInventoryManager = inventoryController.gameObject.AddComponent<PhysicsInventoryManager>();
                    Debug.Log("PhysicsInventoryManager component added to InventoryController");
                }
            }
        }

        if (shouldInitialize)
        {
            playerStats.InitializeStats();
            isInitialized = true;
        }
    }

    /// <summary>
    /// �� ��ȯ �� ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    public void ClearSceneReferences()
    {
        playerStats = null;
        shopController = null;
        combatController = null;
        gameOverController = null;
        physicsInventoryManager = null; // ���� �κ��丮 �Ŵ��� ���� ����
        isInitialized = false;
    }

    /// <summary>
    /// ������ �����ϰ� �ε� ȭ������ ��ȯ�մϴ�.
    /// </summary>
    public void StartGame()
    {
        // �ε� ���� ���� �ʱ�ȭ
        LoadingProgress = 0f;
        isLoadingCancelled = false;

        // �޸� ����ȭ (������)
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // �ε� ���·� ��ȯ
        SetGameState(GameState.Loading);

        // �ε� ������ ��ȯ
        int loadingSceneIndex;
        if (gameScene.TryGetValue(GameState.Loading, out loadingSceneIndex))
        {
            SceneManager.LoadScene(loadingSceneIndex);
        }
    }

    /// <summary>
    /// �ε� ���μ����� �����մϴ�. �ε� ������ ȣ��˴ϴ�.
    /// </summary>
    public void StartLoadingProcess()
    {
        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        float startTime = Time.time;

        // 1. �ʱ�ȭ �۾� ����
        yield return StartCoroutine(PerformInitializationSteps());

        if (isLoadingCancelled)
        {
            OnLoadingCancelled?.Invoke();
            yield break;
        }

        // 2. ���� �� �񵿱� �ε�
        int combatSceneIndex;
        if (!gameScene.TryGetValue(GameState.Playing, out combatSceneIndex))
        {
            combatSceneIndex = 2; // �⺻��
        }

        currentSceneLoadOperation = SceneManager.LoadSceneAsync(combatSceneIndex);
        currentSceneLoadOperation.allowSceneActivation = false; // �ε��� �Ϸ�Ǿ �ٷ� Ȱ��ȭ���� ����

        // �� �ε� ����� ������Ʈ (90% -> 100%)
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

        // 3. �ּ� �ε� �ð� ����
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // 4. �ε� �Ϸ�
        LoadingProgress = 1.0f;

        // �ε� �Ϸ� �̺�Ʈ �߻�
        OnLoadingCompleted?.Invoke();

        // 5. �� Ȱ��ȭ
        SetGameState(GameState.Playing);
        currentSceneLoadOperation.allowSceneActivation = true;

        // 6. ���� ���� ���� ���
        var soundManager = SoundManager.Instance;
        if (soundManager != null && !soundManager.IsBGMPlaying("BGM_Battle"))
        {
            soundManager.LoadSoundBank("CombatSoundBank");
            soundManager.PlaySound("BGM_Battle", 1f, true);
        }
    }


    /// <summary>
    /// �ε��� ����մϴ�.
    /// </summary>
    public void CancelLoading()
    {
        isLoadingCancelled = true;

        // �� �ε� �۾� ��� (������ ���)
        if (currentSceneLoadOperation != null && !currentSceneLoadOperation.isDone)
        {
            // Unity�� ���������� AsyncOperation�� ����� ����� �������� ����
            // ��� ���� �޴��� ���ư� �� �޸� ����
            if (aggressiveMemoryOptimization)
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
        }
    }

    // ��� �ʱ�ȭ �ܰ踦 �����ϴ� �ڷ�ƾ
    private IEnumerator PerformInitializationSteps()
    {
        // 1. ���� �ý��� �ʱ�ȭ (0% -> 10%)
        InitializeCombatSound(); // ������ ���� �غ�
        LoadingProgress = 0.1f;
        yield return InitializationDelay;

        if (isLoadingCancelled) yield break;

        // 2. ������Ʈ Ǯ �ʱ�ȭ (10% -> 25%)
        yield return InitializeObjectPools();
        LoadingProgress = 0.25f;

        if (isLoadingCancelled) yield break;

        // 3. ���� ���ҽ� �ε� (25% -> 50%)
        yield return PreloadGameResources();
        LoadingProgress = 0.5f;

        if (isLoadingCancelled) yield break;

        // 4. ���� �ý��� �غ� (50% -> 65%)
        yield return PrepareCombatSystem();
        LoadingProgress = 0.65f;

        if (isLoadingCancelled) yield break;

        // 5. ���� �κ��丮 �ý��� �ʱ�ȭ (65% -> 75%) - �߰���
        yield return InitializePhysicsInventorySystem();
        LoadingProgress = 0.75f;

        if (isLoadingCancelled) yield break;

        // 6. ���� �غ� (75% -> 90%)
        yield return FinalizeInitialization();
        LoadingProgress = 0.9f;
    }

    private IEnumerator InitializeObjectPools()
    {
        // MapManager �ʱ�ȭ
        if (FindFirstObjectByType<MapManager>() == null)
        {
            GameObject mapManagerObj = new GameObject("MapManager");
            mapManagerObj.AddComponent<MapManager>();
            DontDestroyOnLoad(mapManagerObj);
        }

        // ObjectPool�� �̹� �����ϴ��� Ȯ��
        if (ObjectPool.Instance == null)
        {
            GameObject poolObject = new GameObject("ObjectPool");
            poolObject.AddComponent<ObjectPool>();
            DontDestroyOnLoad(poolObject);
        }

        // VFX Ǯ �ʱ�ȭ
        InitializeVFXPools();
        LoadingProgress = 0.15f;
        yield return ResourceLoadDelay;

        // ���� Ǯ �ʱ�ȭ
        yield return InitializeWeaponPools();
    }
    private void InitializeVFXPools()
    {
        // �⺻ VFX Ǯ �ʱ�ȭ (�ε� �ܰ迡�� ��� �ʿ��� VFX)
        GameObject bulletDestroyVFX = Resources.Load<GameObject>("Prefabs/VFX/BulletDestroyVFX");
        if (bulletDestroyVFX != null)
        {
            ObjectPool.Instance.CreatePool("Bullet_DestroyVFX", bulletDestroyVFX, 30);
        }
        else
        {
            Debug.LogWarning("VFX �������� ã�� �� �����ϴ�: BulletDestroyVFX");
        }

        // �ʿ��� ��� ���⿡ �� ���� �⺻ VFX�� �߰��� �� �ֽ��ϴ�
    }

    private IEnumerator InitializeWeaponPools()
    {
        // �ֿ� ���� Ǯ �ʱ�ȭ
        string[] weaponTypes = { "Buster", "Machinegun", "BeamSaber", "Shotgun", "Cutter", "Sawblade", "Grinder", "ForceField" };
        float progressPerWeapon = 0.1f / weaponTypes.Length;
        float currentProgress = 0.15f;

        for (int i = 0; i < weaponTypes.Length; i++)
        {
            string weaponType = weaponTypes[i];
            string poolName = $"{weaponType}_Projectile";
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Weapons/Projectile{poolName}");

            if (prefab != null)
            {
                int poolSize = GetOptimalPoolSize(weaponType);
                ObjectPool.Instance.CreatePool(poolName, prefab, poolSize);
            }
            else
            {
                Debug.LogWarning($"Weapon Prefab not found: {poolName}");
            }

            // Grinder�� ���� Ư�� ó�� - Effect Ǯ�� ����
            if (weaponType == "Grinder")
            {
                string effectPoolName = "Grinder_Effect";
                GameObject effectPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Grinder_Effect");

                if (effectPrefab != null)
                {
                    int effectPoolSize = GetOptimalPoolSize("Grinder_Effect");
                    ObjectPool.Instance.CreatePool(effectPoolName, effectPrefab, effectPoolSize);
                }
                else
                {
                    Debug.LogWarning($"Grinder Effect Prefab not found: {effectPoolName}");
                }
            }

            // ����� ������Ʈ
            currentProgress += progressPerWeapon;
            LoadingProgress = currentProgress;
            yield return ResourceLoadDelay;
            if (isLoadingCancelled) yield break;
        }

        // Wisp ����ü Ǯ �ʱ�ȭ (�� ����ü)
        GameObject wispProjectilePrefab = Resources.Load<GameObject>("Prefabs/Projectiles/Wisp_Projectile");
        if (wispProjectilePrefab != null)
        {
            int poolSize = GetOptimalPoolSize("Wisp_Projectile");
            ObjectPool.Instance.CreatePool("Wisp_Projectile", wispProjectilePrefab, poolSize);
            Debug.Log("Wisp ����ü Ǯ �ʱ�ȭ��: " + poolSize + "�� ����");
        }
        else
        {
            Debug.LogWarning("Wisp ����ü �������� ã�� �� �����ϴ�!");
        }
    }

    private int GetOptimalPoolSize(string objectType)
    {
        // ������Ʈ Ÿ�Կ� ���� ������ Ǯ ũ�� ��ȯ
        switch (objectType)
        {
            // ���� Ÿ��
            case "Machinegun": return 50;
            case "Shotgun": return 20;
            case "Buster": return 15;
            case "Cutter": return 20;
            case "Sawblade": return 10;
            case "BeamSaber": return 12;
            case "Grinder": return 24;
            case "Grinder_Effect": return 48; // Grinder Projectile�� 2��
            case "ForceField": return 4;

            // �� Ÿ��
            case "Walker": return 15;
            case "Hunter": return 15;
            case "Heavy": return 15;

            //�� ����ü
            case "Wisp_Projectile": return 30;

            // ������ Ÿ��
            case "ExperienceSmall": return 30;
            case "ExperienceMedium": return 30;
            case "ExperienceLarge": return 30;
            case "Coin": return 10;
            case "Potion": return 10;
            case "Magnet": return 10;

            // VFX Ÿ��
            case "BulletDestroyVFX": return 30;
            case "DeathParticle": return 15 * 5;

            // ���� �κ��丮 ������ Ÿ�� (�߰�)
            case "PhysicsInventoryItem": return 20;

            // �⺻��
            default: return 15;
        }
    }

    private IEnumerator PreloadGameResources()
    {
        // �� ������ �ε�
        string[] enemyTypes = { "Walker", "Hunter", "Heavy" };
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            GameObject enemyPrefab = Resources.Load<GameObject>($"Prefabs/Characters/{enemyTypes[i]}");
            if (enemyPrefab != null)
            {
                int poolSize = GetOptimalPoolSize(enemyTypes[i]);
                ObjectPool.Instance.CreatePool(enemyTypes[i], enemyPrefab, poolSize);
            }
            else
            {
                Debug.LogWarning($"Enemy Prefab not found: {enemyTypes[i]}");
            }
            yield return ResourceLoadDelay;
            if (isLoadingCancelled) yield break;
        }
        LoadingProgress = 0.35f;

        if (isLoadingCancelled) yield break;

        // ������ ������ �ε�
        string[] itemTypes = {
        "ExperienceSmall", "ExperienceMedium", "ExperienceLarge",
        "Coin", "Potion", "Magnet"
    };

        for (int i = 0; i < itemTypes.Length; i++)
        {
            GameObject itemPrefab = Resources.Load<GameObject>($"Prefabs/Collectibles/{itemTypes[i]}");
            if (itemPrefab != null)
            {
                int poolSize = GetOptimalPoolSize(itemTypes[i]);
                ObjectPool.Instance.CreatePool(itemTypes[i], itemPrefab, poolSize);
            }
            else
            {
                Debug.LogWarning($"Item Prefab not found: {itemTypes[i]}");
            }
            yield return ResourceLoadDelay;
            if (isLoadingCancelled) yield break;
        }
        LoadingProgress = 0.45f;

        GameObject deathParticlePrefab = Resources.Load<GameObject>("Prefabs/VFX/DeathParticle");
        if (deathParticlePrefab != null)
        {
            // �ִ� ���� ����Ʈ * ����Ʈ �� ��ƼŬ �� = �� ��ƼŬ ��
            int poolSize = 5 * 5; // 5�� ���� ����Ʈ, �� 5�� ��ƼŬ
            ObjectPool.Instance.CreatePool("DeathParticle", deathParticlePrefab, poolSize);
            Debug.Log("Death particle pool initialized");
        }
        else
        {
            Debug.LogWarning("Death particle prefab not found!");
        }
        yield return ResourceLoadDelay;
        LoadingProgress = 0.5f;
    }

    private IEnumerator PrepareCombatSystem()
    {
        // SpawnSettings �ε�
        var spawnSettings = Resources.Load<ScriptableObject>("Data/SpawnSettings");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.55f;

        // �� �Ŵ��� ���� �� �� �ε�
        if (FindFirstObjectByType<MapManager>() == null)
        {
            GameObject mapManagerObj = new GameObject("MapManager");
            mapManagerObj.AddComponent<MapManager>();
            DontDestroyOnLoad(mapManagerObj);
        }

        // �� �ε�
        LoadingProgress = 0.6f;
        yield return ResourceLoadDelay;

        // �����ͺ��̽� �ε�
        var weaponDatabase = Resources.Load<ScriptableObject>("Data/WeaponDatabase");
        var enemySpawnDatabase = Resources.Load<ScriptableObject>("Data/EnemySpawnDatabase");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.65f;
    }

    /// <summary>
    /// ���� ��� �κ��丮 �ý��� �ʱ�ȭ
    /// </summary>
    private IEnumerator InitializePhysicsInventorySystem()
    {
        Debug.Log("Initializing Physics Inventory System...");

        // 1. ���� �κ��丮 ���� ���� ���ҽ� �ε�
        LoadingProgress = 0.68f;
        yield return ResourceLoadDelay;

        // 2. �ʿ��� ������ �ε� �� Ǯ �ʱ�ȭ
        GameObject weaponPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Item");
        if (weaponPrefab != null)
        {
            // ���� �������� ���� ������Ʈ Ǯ ����
            string poolTag = "PhysicsInventoryItem";
            if (ObjectPool.Instance != null && !ObjectPool.Instance.DoesPoolExist(poolTag))
            {
                // Ǯ �ʱ�ȭ (�ʱ� ũ�� 20, �ʿ�� Ȯ�� ����)
                ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, 20);
                Debug.Log("Physics inventory item pool created with 20 items");
            }
            Debug.Log("Physics inventory item prefab loaded");
        }
        else
        {
            Debug.LogWarning("Physics inventory item prefab not found!");
        }

        LoadingProgress = 0.71f;
        yield return ResourceLoadDelay;

        // 3. PhysicsInventoryInitializer�� ���� �߰� �ʱ�ȭ
        PhysicsInventoryInitializer initializer = PhysicsInventoryInitializer.Instance;
        if (initializer != null)
        {
            // �ε� ȭ�鿡�� ���� �κ��丮 �ý��� �̸� �ʱ�ȭ
            yield return initializer.PreloadPhysicsAssets();

            // Ǯ �ý����� ���� �ʱ�ȭ
            yield return PhysicsInventoryInitializer.InitializeInLoadingScreen();
        }

        LoadingProgress = 0.75f;
        yield return ResourceLoadDelay;

        Debug.Log("Physics Inventory System initialized");
    }

    private IEnumerator FinalizeInitialization()
    {
        // �޸� ����ȭ
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // �ʱ�ȭ �Ϸ� ���
        yield return InitializationDelay;
    }

    /// <summary>
    /// ������ ���¸� �����ϰ� ���� �ý����� ������Ʈ�մϴ�.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        GameState previousState = currentGameState;
        currentGameState = newState;

        // ���� ���� ���� �ʿ��� �غ� �۾�
        PrepareForStateTransition(previousState, newState);

        // �̺�Ʈ �߻�
        OnGameStateChanged?.Invoke(newState);

        // ���¿� ���� ���� ���� ����
        switch (newState)
        {
            case GameState.MainMenu:
            case GameState.Loading:
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                // GameOver UI�� ���� ǥ���� �� timeScale ����
                HandleGameOver();
                Time.timeScale = 0f;
                break;
        }
    }

    /// <summary>
    /// ���� ��ȯ �� �ʿ��� �غ� �۾��� �����մϴ�.
    /// </summary>
    private void PrepareForStateTransition(GameState previousState, GameState newState)
    {
        // ��: ���� �޴����� �ε����� ��ȯ ��
        if (previousState == GameState.MainMenu && newState == GameState.Loading)
        {
            // �޸� ���� ���� �۾��� �ʿ��� ���
        }
    }

    /// <summary>
    /// ���� ���� ���¿����� ó���� ����մϴ�.
    /// </summary>
    private void HandleGameOver()
    {
        SavePlayerProgress();

        // ���� ���� ȿ���� ���
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

    /// <summary>
    /// �־��� ���̺� ���� ���� ���� ���̺갡 ���ԵǾ� �ִ��� Ȯ���մϴ�.
    /// </summary>
    /// <param name="minWave">�ּ� ���̺� ��ȣ (����)</param>
    /// <param name="maxWave">�ִ� ���̺� ��ȣ (����)</param>
    /// <returns>���ԵǾ� ������ true, �ƴϸ� false</returns>
    public bool IsWaveInRange(int minWave, int maxWave)
    {
        return _currentWave >= minWave && _currentWave <= maxWave;
    }
    public void ResetWave()
    {
        _currentWave = 0;
    }
    /// <summary>
    /// ���̺� �Ŵ����� �����Ͽ� ���̺� ������ ������Ʈ�մϴ�.
    /// </summary>
    /// <param name="waveNumber">���� ���̺� ��ȣ</param>
    public void UpdateWaveInfo(int waveNumber)
    {
        _currentWave = waveNumber;
    }
    public void SetOptionPanelReference(GameObject optionPanelRef)
    {
        optionPanel = optionPanelRef;
    }
    public void SetStartSceneReferences(GameObject optionPanelRef)
    {
        optionPanel = optionPanelRef;
    }
    public void ToggleOptionPanel()
    {
        if (optionPanel == null)
        {
            
            optionPanel = GameObject.FindWithTag("OptionPanel");
            if (optionPanel == null)
            {
                Debug.LogError("OptionPanel not found - make sure it's tagged properly");
                return;
            }
        }

        bool isActive = !optionPanel.activeSelf;
        optionPanel.SetActive(isActive);

        
        SoundManager.Instance?.PlaySound("Button_sfx", 0f, false);

        
        SetGameState(isActive ? GameState.Paused : GameState.Playing);
    } 
    public bool IsPlaying() => currentGameState == GameState.Playing;

    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // ���� ��׶���� �� �� (pauseStatus == true)
        if (pauseStatus && IsPlaying())
        {
            // ���� ��Ȳ �ڵ� ����
            SavePlayerProgress();
        }
    }

    /// <summary>
    /// �÷��̾��� ���� ��Ȳ�� �����մϴ�.
    /// </summary>
    public void SavePlayerProgress()
    {
        // ����� SoundManager�� ��ü������ ���� ������ ����
        // �߰����� ���� ������ �ʿ��ϸ� ���⿡ ����
    }
}