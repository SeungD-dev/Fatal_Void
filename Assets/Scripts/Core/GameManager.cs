using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 게임의 전반적인 상태와 시스템을 관리하는 매니저 클래스
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GameManager").AddComponent<GameManager>();
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

    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;

    private GameOverController gameOverController;
    public GameOverController GameOverController => gameOverController;
    public bool IsInitialized { get; private set; }

    public event System.Action<GameState> OnGameStateChanged;
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
        gameScene = new Dictionary<GameState, int>()
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
        // StartScene(MainMenu)에서 시작하므로 IntroSoundBank 로드
        var soundManager = SoundManager.Instance;
        soundManager.LoadSoundBank("IntroSoundBank");
    }

    /// <summary>
    /// CombatScene의 주요 컴포넌트들을 설정합니다.
    /// </summary>
    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat, GameOverController gameOver)
    {
        playerStats = stats;
        shopController = shop;
        combatController = combat;
        gameOverController = gameOver;

        if (playerStats != null)
        {
            playerStats.InitializeStats();
            IsInitialized = true;
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
        IsInitialized = false;
    }

    /// <summary>
    /// 게임을 시작하고 CombatScene으로 전환합니다.
    /// </summary>
    public void StartGame()
    {
        var soundManager = SoundManager.Instance;

        // CombatScene으로 전환 시 사운드뱅크 로드
        soundManager.LoadSoundBank("CombatSoundBank");

        // 기존 볼륨 설정 유지한 채로 배경음악 재생
        if (!soundManager.IsBGMPlaying("BGM_Battle"))
        {
            soundManager.PlaySound("BGM_Battle", 1f, true);
        }

        SetGameState(GameState.Playing);
        SceneManager.LoadScene(gameScene[GameState.Playing], LoadSceneMode.Single);
    }

    /// <summary>
    /// 게임의 상태를 변경하고 관련 시스템을 업데이트합니다.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            currentGameState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    if (gameOverController != null)
                    {
                        gameOverController.ShowGameOverPanel();
                    }
                    HandleGameOver();
                    Time.timeScale = 0f;
                    break;
            }
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
        // 현재는 SoundManager가 자체적으로 볼륨 설정을 저장하므로
        // 추가적인 데이터 저장이 필요한 경우 여기에 구현
    }
}