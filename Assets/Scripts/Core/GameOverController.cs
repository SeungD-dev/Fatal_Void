using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ���� ���� UI�� ���� ����� �����ϴ� ��Ʈ�ѷ�
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
        // ������Ʈ�� Ȱ��ȭ�� ������ �̺�Ʈ ��� �õ�
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
            // PlayerStats�� ���ٸ� ���� �����ӿ��� �ٽ� �õ�
            StartCoroutine(TryRegisterEventsNextFrame());
        }
    }
    private void InitializeUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // ��ư �̺�Ʈ ����
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    /// <summary>
    /// ���� ���� �г��� ǥ���ϰ� ���� ������ ������Ʈ�մϴ�.
    /// </summary>
    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            // timeScale�� ������ UI�� Ȱ��ȭ
            gameOverPanel.SetActive(true);
            
            // Canvas Group�̳� Animation�� �ִٸ� ���� ǥ���ǵ��� ����
            var canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }

    /// <summary>
    /// Retry ��ư Ŭ�� �� ������ ������մϴ�.
    /// </summary>
    private void OnRetryButtonClicked()
    {
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }

        // 게임오버 패널 즉시 비활성화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Time.timeScale 정상화 (혹시 멈춰있었다면)
        Time.timeScale = 1f;

        // 게임 상태 초기화
        GameManager.Instance.ClearSceneReferences();

        // 약간의 지연 후 게임 재시작 (정리 작업 완료를 기다림)
        StartCoroutine(RestartGameAfterDelay());
    }

    private System.Collections.IEnumerator RestartGameAfterDelay()
    {
        // 1프레임 대기하여 정리 작업이 완료되도록 함
        yield return null;

        // 게임 재시작
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Quit ��ư Ŭ�� �� ������ �����մϴ�.
    /// </summary>
    private void OnQuitButtonClicked()
    {
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("SFX_ButtonClick", 0f, false);
        }

        // ���� ����
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�� �� �̺�Ʈ ����
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