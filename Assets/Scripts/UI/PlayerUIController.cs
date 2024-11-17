using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timeTxt;
    [SerializeField] private TextMeshProUGUI killCountTxt;
    [SerializeField] private TextMeshProUGUI coinTxt;
    [SerializeField] private TextMeshProUGUI lvlTxt;
    [SerializeField] private Slider expBar;
    [SerializeField] private Slider healthBar;

    [Header("Format Settings")]
    [SerializeField] private string timeFormat = "mm:ss";

    private float gameTime;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    private IEnumerator WaitForInitialization()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (!GameManager.Instance.IsInitialized && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("UI initialization timed out!");
            yield break;
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        var playerStats = GameManager.Instance.PlayerStats;
        if (playerStats != null)
        {
            // UI 관련 이벤트 구독
            SubscribeToEvents(playerStats);

            // 초기 UI 설정
            ResetUI(playerStats);
            isInitialized = true;
            Debug.Log("UI Initialized Successfully");  // 디버그 로그 추가
        }
    }

    private void SubscribeToEvents(PlayerStats playerStats)
    {
        playerStats.OnHealthChanged += UpdateHealthBar;
        playerStats.OnExpChanged += UpdateExpBar;
        playerStats.OnLevelUp += UpdateLevel;
        playerStats.OnKillCountChanged += UpdateKillCount;
        playerStats.OnCoinChanged += UpdateCoinCount;
        playerStats.OnPlayerDeath += HandlePlayerDeath;

        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void Update()
    {
        if (isInitialized && GameManager.Instance.IsPlaying())
        {
            gameTime += Time.deltaTime;
            UpdateTimeDisplay();
            // Debug.Log($"Game Time: {gameTime}");  // 필요시 디버그 로그
        }
    }

    private void ResetUI(PlayerStats playerStats)
    {
        gameTime = 0f;

        UpdateTimeDisplay();
        UpdateKillCount(playerStats.KillCount);
        UpdateCoinCount(playerStats.CoinCount);
        UpdateLevel(playerStats.Level);
        UpdateExpBar(playerStats.CurrentExp, playerStats.RequiredExp);
        UpdateHealthBar(playerStats.CurrentHealth, playerStats.MaxHealth);
    }

    #region UI Update Methods
    private void UpdateTimeDisplay()
    {
        if (timeTxt != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            timeTxt.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void UpdateKillCount(int count)
    {
        if (killCountTxt != null)
        {
            killCountTxt.text = $"Kills: {count}";
        }
    }

    private void UpdateCoinCount(int count)
    {
        if (coinTxt != null)
        {
            coinTxt.text = $"Coins: {count}";
        }
    }

    private void UpdateLevel(int level)
    {
        if (lvlTxt != null)
        {
            lvlTxt.text = $"Level {level}";
        }
    }

    private void UpdateExpBar(float currentExp, float requiredExp)
    {
        if (expBar != null)
        {
            expBar.value = currentExp / requiredExp;
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
    #endregion

    #region Event Handlers
    private void UpdateExpBar(float newExp)
    {
        var playerStats = GameManager.Instance.PlayerStats;
        UpdateExpBar(newExp, playerStats.RequiredExp);
    }

    private void UpdateHealthBar(float newHealth)
    {
        var playerStats = GameManager.Instance.PlayerStats;
        UpdateHealthBar(newHealth, playerStats.MaxHealth);
    }

    private void HandlePlayerDeath()
    {
        enabled = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        enabled = (newState == GameState.Playing);
        Debug.Log($"Game State Changed to: {newState}, UI Enabled: {enabled}");  // 디버그 로그 추가
    }
    #endregion

    private void OnDestroy()
    {
        var playerStats = GameManager.Instance?.PlayerStats;
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnExpChanged -= UpdateExpBar;
            playerStats.OnLevelUp -= UpdateLevel;
            playerStats.OnKillCountChanged -= UpdateKillCount;
            playerStats.OnCoinChanged -= UpdateCoinCount;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
}