using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 오버 UI와 관련 기능을 관리하는 컨트롤러
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;
    private PlayerStats playerStats;
    private SoundManager soundManager;
    private bool isInitialized = false;

    private void Awake()
    {
        soundManager = SoundManager.Instance;
        InitializeUI();
    }  
    private void OnEnable()
    {
        // 컴포넌트가 활성화될 때마다 이벤트 등록 시도
        RegisterEvents();
    }
    private IEnumerator TryRegisterEventsNextFrame()
    {
        yield return null;
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        if (isInitialized) return;

        playerStats = GameManager.Instance?.PlayerStats;
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += ShowGameOverPanel;
            isInitialized = true;
            Debug.Log("GameOverController: Events registered successfully");
        }
        else
        {
            // PlayerStats가 없다면 다음 프레임에서 다시 시도
            StartCoroutine(TryRegisterEventsNextFrame());
        }
    }
    private void InitializeUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 버튼 이벤트 설정
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    /// <summary>
    /// 게임 오버 패널을 표시하고 관련 정보를 업데이트합니다.
    /// </summary>
    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            // 시간과 무관하게 동작하도록 설정
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Retry 버튼 클릭 시 게임을 재시작합니다.
    /// </summary>
    private void OnRetryButtonClicked()
    {
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        // 게임 상태 초기화
        GameManager.Instance.ClearSceneReferences();

        // 게임 재시작
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Quit 버튼 클릭 시 게임을 종료합니다.
    /// </summary>
    private void OnQuitButtonClicked()
    {
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("SFX_ButtonClick", 0f, false);
        }

        // 게임 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void OnDisable()
    {
        // 컴포넌트가 비활성화될 때 이벤트 해제
        UnregisterEvents();
    }
    private void UnregisterEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= ShowGameOverPanel;
        }
        isInitialized = false;
    }
    private void OnDestroy()
    {
        UnregisterEvents();

        if (retryButton != null)
            retryButton.onClick.RemoveAllListeners();
        if (quitButton != null)
            quitButton.onClick.RemoveAllListeners();
    }
}