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
            { GameState.MainMenu, 0 },
            { GameState.Playing, 1 },
            { GameState.Paused, 2 },
            { GameState.GameOver, 3 }
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
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
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
