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
            if(instance == null)
            {
                instance = new GameObject("GameManager").AddComponent<GameManager>();
            }
            return instance;
        }
    }
    public GameState currentGameState { get; private set; }

    public Dictionary<GameState,int> gameScene { get; private set; }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        gameScene = new Dictionary<GameState, int>() 
        {
            { GameState.MainMenu, 0 },
            {GameState.Playing, 1 },
        };
    }

    public void StartGame()
    {
        currentGameState = GameState.Playing;
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
