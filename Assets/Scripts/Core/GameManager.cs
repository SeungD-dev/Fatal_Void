using UnityEngine;
using UnityEngine.SceneManagement;
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

    // 자주 사용되는 WaitForSeconds 캐싱
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);
    #endregion

    private void Awake()
    {
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
        gameScene = new Dictionary<GameState, int>(4) // 초기 용량 지정으로 재할당 방지
        {
            { GameState.MainMenu, 0 },    // StartScene
            { GameState.Playing, 1 },     // CombatScene
            { GameState.Paused, 1 },      // 같은 CombatScene에서 Pause
            { GameState.GameOver, 1 }     // 같은 CombatScene에서 GameOver
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
    /// 게임을 시작하고 CombatScene으로 전환합니다.
    /// </summary>
    public void StartGame()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            if (!soundManager.IsBGMPlaying("BGM_Battle"))
            {
                soundManager.LoadSoundBank("CombatSoundBank");
                soundManager.PlaySound("BGM_Battle", 1f, true);
            }
        }

        int sceneIndex;
        if (gameScene.TryGetValue(GameState.Playing, out sceneIndex))
        {
            SetGameState(GameState.Playing);
            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// 게임의 상태를 변경하고 관련 시스템을 업데이트합니다.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        currentGameState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.MainMenu:
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
    /// 게임 오버 상태에서의 처리를 담당합니다.
    /// </summary>
    private void HandleGameOver()
    {
        SavePlayerProgress();

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

    /// <summary>
    /// 플레이어의 진행 상황을 저장합니다.
    /// </summary>
    public void SavePlayerProgress()
    {
        // 현재는 SoundManager가 자체적으로 볼륨 설정을 저장
    }
}