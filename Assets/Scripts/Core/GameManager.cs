using System.Collections;
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

    // PlayerStats 관리 추가
    private PlayerStats playerStats;
    public PlayerStats PlayerStats => playerStats;

    public event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        gameScene = new Dictionary<GameState, int>()
        {
            { GameState.MainMenu, 0 },
            { GameState.Playing, 1 },
            { GameState.Paused, 2 },
            {GameState.GameOver, 3 }
        };
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

   public bool IsPlaying() { return currentGameState == GameState.Playing; }


    public void StartGame()
    {
        currentGameState = GameState.Playing;
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single); 
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentGameState == GameState.Playing && scene.name == "CombatScene")
        {
            StartCoroutine(InitializeAfterSceneLoad());
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        // 씬이 완전히 로드될 때까지 대기
        yield return new WaitForEndOfFrame();

        // Player 찾기 및 초기화
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.InitializeStats();
            }
        }
    }

    public void FindPlayerStats()
    {
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
    }

    private void InitializeCombatScene()
    {
        ClearCombatData();
        // 게임 시작 시 자동으로 레벨 1로 시작
        if (playerStats != null)
        {
            playerStats.InitializeStats();
        }
    }


    private void ClearCombatData()
    {
        playerStats = null;
    }

    // PlayerStats 관리 메서드들
    public void SetPlayerStats(PlayerStats stats)
    {
        if (currentGameState == GameState.Playing)
        {
            playerStats = stats;
            Debug.Log("PlayerStats registered with GameManager");
        }
    }

    public void ClearPlayerStats()
    {
        playerStats = null;
    }

    // 씬 전환 시 PlayerStats 데이터 저장을 위한 메서드들
    public void SavePlayerProgress()
    {
        if (playerStats != null)
        {
            // TODO: 필요한 경우 플레이어 진행 상황 저장
        }
    }

    public void LoadPlayerProgress()
    {
        if (playerStats != null)
        {
            // TODO: 저장된 플레이어 진행 상황 로드
        }
    }

    // 게임 종료 시 정리
    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }
}
