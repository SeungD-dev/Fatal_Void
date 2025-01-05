using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public GameState currentGameState { get; private set; }
    public Dictionary<GameState, int> gameScene { get; private set; }

    // 씬 참조들
    private PlayerStats playerStats;
    private ShopController shopController;
    private CombatController combatController;

    // Public 속성들
    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;
    public bool IsInitialized { get; private set; }

    public event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameScenes();

            // StartScene(MainMenu)에서 시작하므로 IntroSoundBank 로드
            SoundManager.Instance.LoadSoundBank("IntroSoundBank");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGameScenes()
    {
        gameScene = new Dictionary<GameState, int>()
        {
            { GameState.MainMenu, 0 },   // StartScene
            { GameState.Playing, 1 },     // CombatScene
            { GameState.Paused, 1 },      // 같은 CombatScene에서 Pause
            { GameState.GameOver, 1 }     // 같은 CombatScene에서 GameOver
        };
    }

    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat)
    {
        playerStats = stats;
        shopController = shop;
        combatController = combat;
        if (playerStats != null)
        {
            playerStats.InitializeStats();
            IsInitialized = true;
        }
    }

    public void ClearSceneReferences()
    {
        playerStats = null;
        shopController = null;
        combatController = null;
        IsInitialized = false;
    }

    public void StartGame()
    {
        // CombatScene으로 전환 시 사운드 설정
        SoundManager.Instance.LoadSoundBank("CombatSoundBank");
        if (!SoundManager.Instance.IsBGMPlaying("BGM_Battle"))
        {
        SoundManager.Instance.PlaySound("BGM_Battle", 1f, true);

        }

        SetGameState(GameState.Playing);
        SceneManager.LoadScene(gameScene[GameState.Playing], LoadSceneMode.Single);
    }

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
                    Time.timeScale = 0f;
                    break;
            }
        }
    }

    public bool IsPlaying() => currentGameState == GameState.Playing;

    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }

    private void SavePlayerProgress()
    {
        // TODO: 필요한 경우 플레이어 진행 상황 저장
    }
}