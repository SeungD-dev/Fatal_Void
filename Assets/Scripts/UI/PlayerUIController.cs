using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

public class PlayerUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timeTxt;
    [SerializeField] private TextMeshProUGUI killCountTxt;
    [SerializeField] private TextMeshProUGUI coinTxt;
    [SerializeField] private TextMeshProUGUI lvlTxt;
    [SerializeField] private Slider expBar;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject optionPanel;

    [Header("Boss Warning System")]
    [SerializeField] private TextMeshProUGUI bossWarningTxt;
    [SerializeField] private GameObject transitionEffect;
    [SerializeField] private BossWarningEffects bossWarningEffects;


    [Header("UI Update Settings")]
    [SerializeField] private float uiUpdateInterval = 0.1f;

    // ĳ�õ� ����
    private PlayerStats playerStats;
    private GameManager gameManager;
    private StringBuilder stringBuilder;

    // ���� ����
    private float gameTime;
    private bool isInitialized;
    private float nextUpdateTime;
    private bool useExternalTimer = false;

    // ĳ�õ� �ð� ����
    private int cachedMinutes;
    private int cachedSeconds;

    // ĳ�õ� ���ڿ� ����
    private const string TIME_FORMAT = "{0:00}:{1:00}";
    private const string KILL_FORMAT = "Kills: {0}";
    private const string LEVEL_FORMAT = "Lv.{0}";
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);

    private void Awake()
    {
        stringBuilder = new StringBuilder(32);
        gameManager = GameManager.Instance;
    }

    private void Start()
    {
        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetOptionPanelReference(optionPanel);
        }
        StartCoroutine(WaitForInitialization());
    }


    public GameObject GetOptionPanel()
    {
        return optionPanel;
    }
    private IEnumerator WaitForInitialization()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (!gameManager.IsInitialized && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return InitializationDelay;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("UI initialization timed out!");
            yield break;
        }

        InitializeUI();
    }
    public void SetExternalTimer(string timerDisplay)
    {
        if (timeTxt != null)
        {
            useExternalTimer = true;
            timeTxt.text = timerDisplay;
        }
    }
    private void InitializeUI()
    {
        playerStats = gameManager.PlayerStats;
        if (playerStats != null)
        {
            // UI �̺�Ʈ ����
            SubscribeToEvents();
            // �ʱ� UI ����
            ResetUI();
            isInitialized = true;
        }
    }

    private void SubscribeToEvents()
    {
        playerStats.OnHealthChanged += UpdateHealthBar;
        playerStats.OnExpChanged += UpdateExpBar;
        playerStats.OnLevelUp += UpdateLevel;
        playerStats.OnKillCountChanged += UpdateKillCount;
        playerStats.OnCoinChanged += UpdateCoinCount;
        playerStats.OnPlayerDeath += HandlePlayerDeath;
        gameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void Update()
    {
        if (!isInitialized || !gameManager.IsPlaying()) return;

        // �ܺ� Ÿ�̸Ӹ� ������� ���� ���� �ð� ������Ʈ
        if (!useExternalTimer)
        {
            gameTime += Time.deltaTime;

            if (Time.time >= nextUpdateTime)
            {
                UpdateTimeDisplay();
                nextUpdateTime = Time.time + uiUpdateInterval;
            }
        }
    }

    private void ResetUI()
    {
        gameTime = 0f;
        nextUpdateTime = 0f;

        UpdateTimeDisplay();
        UpdateKillCount(playerStats.KillCount);
        UpdateCoinCount(playerStats.CoinCount);
        UpdateLevel(playerStats.Level);
        UpdateExpBar(playerStats.CurrentExp);
        UpdateHealthBar(playerStats.CurrentHealth);
    }

    #region UI Update Methods
    private void UpdateTimeDisplay()
    {
        if (timeTxt == null) return;

        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);

        // �ð��� ����Ǿ��� ���� �ؽ�Ʈ ������Ʈ
        if (minutes != cachedMinutes || seconds != cachedSeconds)
        {
            stringBuilder.Clear();
            stringBuilder.AppendFormat(TIME_FORMAT, minutes, seconds);
            timeTxt.text = stringBuilder.ToString();

            cachedMinutes = minutes;
            cachedSeconds = seconds;
        }
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


        GameManager.Instance.SetGameState(isActive ? GameState.Paused : GameState.Playing);
    }
    private void UpdateKillCount(int count)
    {
        if (killCountTxt == null) return;

        stringBuilder.Clear();
        stringBuilder.AppendFormat(KILL_FORMAT, count);
        killCountTxt.text = stringBuilder.ToString();
    }

    private void UpdateCoinCount(int count)
    {
        if (coinTxt == null) return;
        coinTxt.text = count.ToString();
    }

    private void UpdateLevel(int level)
    {
        if (lvlTxt == null) return;

        stringBuilder.Clear();
        stringBuilder.AppendFormat(LEVEL_FORMAT, level);
        lvlTxt.text = stringBuilder.ToString();
    }

    private void UpdateExpBar(float currentExp)
    {
        if (expBar == null || playerStats == null) return;
        expBar.value = currentExp / playerStats.RequiredExp;
    }

    private void UpdateHealthBar(float currentHealth)
    {
        if (healthBar == null || playerStats == null) return;
        healthBar.value = currentHealth / playerStats.MaxHealth;
    }
    #endregion

    #region Event Handlers
    private void HandlePlayerDeath()
    {
        enabled = false;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        enabled = (newState == GameState.Playing);
    }
    #endregion

    #region Boss Warning System
    public void StartBossWarningSequence(System.Action onComplete = null)
    {
        StartCoroutine(BossWarningSequence(onComplete));
    }

    private IEnumerator BossWarningSequence(System.Action onComplete)
    {
        // 1. BossWarningTxt 활성화 및 글리치 효과 시작 (2초)
        if (bossWarningTxt != null)
        {
            bossWarningTxt.gameObject.SetActive(true);
            if (bossWarningEffects != null)
            {
                bossWarningEffects.StartGlitchEffect();
            }
        }

        yield return new WaitForSeconds(2.0f);

        // 2. 글리치 효과 중지
        if (bossWarningEffects != null)
        {
            bossWarningEffects.StopGlitchEffect();
        }

        // 3. TransitionEffect 활성화 및 플레이어 위치 이동 (0.3초)
        if (transitionEffect != null)
        {
            transitionEffect.SetActive(true);
            
            // 플레이어를 (0, -0.8, 0) 위치로 이동
            if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            {
                GameManager.Instance.PlayerTransform.position = new Vector3(0, -0.8f, 0);
                Debug.Log("Player moved to (0, -0.8, 0) for boss entrance");
            }
        }

        yield return new WaitForSeconds(0.3f);

        // 4. 모든 효과 비활성화
        HideBossWarningEffects();

        // 5. 완료 콜백 호출
        onComplete?.Invoke();
    }

    public void HideBossWarningEffects()
    {
        if (bossWarningTxt != null)
        {
            bossWarningTxt.gameObject.SetActive(false);
        }

        if (transitionEffect != null)
        {
            transitionEffect.SetActive(false);
        }

        if (bossWarningEffects != null)
        {
            bossWarningEffects.StopGlitchEffect();
        }
    }
    #endregion

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void UnsubscribeFromEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnExpChanged -= UpdateExpBar;
            playerStats.OnLevelUp -= UpdateLevel;
            playerStats.OnKillCountChanged -= UpdateKillCount;
            playerStats.OnCoinChanged -= UpdateCoinCount;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
}