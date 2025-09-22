using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 클리어 UI를 관리하고 관련 동작을 처리하는 컨트롤러
/// </summary>
public class GameClearController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;
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

        // GameManager가 초기화되었는지 확인
        if (GameManager.Instance != null)
        {
            isInitialized = true;
            Debug.Log("GameClearController: Events registered successfully");
        }
        else
        {
            // GameManager가 없다면 다음 프레임에서 다시 시도
            StartCoroutine(TryRegisterEventsNextFrame());
        }
    }

    private void InitializeUI()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(false);
        }

        // 버튼 이벤트 등록
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    /// <summary>
    /// 게임 클리어 패널을 표시하고 관련 작업을 업데이트합니다.
    /// </summary>
    public void ShowGameClearPanel()
    {
        if (gameClearPanel != null)
        {
            // timeScale과 상관없이 UI가 활성화
            gameClearPanel.SetActive(true);

            // Canvas Group이나 Animation이 있다면 정상 표시되도록 설정
            var canvasGroup = gameClearPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        // 게임 클리어 사운드 재생
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("GameClear_sfx", 1f, false);
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

        // 게임클리어 패널 즉시 비활성화
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(false);
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