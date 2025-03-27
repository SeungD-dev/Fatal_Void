using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton;
    [SerializeField] private TextBlinkEffect touchToEngageText;

    [Header("Touch Detection")]
    [SerializeField] private float touchDelay = 0.5f; // 실수로 터치하는 것을 방지하기 위한 지연 시간

    private TouchActions touchActions;
    private bool canStartGame = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Input Actions 초기화
        touchActions = new TouchActions();
    }

    private void OnEnable()
    {
        // Input Actions 활성화
        touchActions.Enable();

        // 터치 이벤트 등록
        touchActions.Touch.Press.started += OnTouchStarted;
    }

    private void OnDisable()
    {
        // Input Actions 비활성화 및 이벤트 해제
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();
    }

    private void Start()
    {
        // GameManager에 옵션 패널 참조 전달
        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetStartSceneReferences(optionPanel);
        }

        // 옵션 버튼 리스너 등록
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClick);
        }

        // 약간의 지연 후 터치 활성화 (씬 전환 직후 우발적 터치 방지)
        StartCoroutine(EnableTouchAfterDelay());
    }

    private IEnumerator EnableTouchAfterDelay()
    {
        yield return new WaitForSeconds(touchDelay);
        canStartGame = true;
    }

    private void OnOptionButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleOptionPanel();
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // 게임 시작 가능한 상태이고 전환 중이 아닐 때만 처리
        if (canStartGame && !isTransitioning)
        {
            // UI 요소 위에서 터치한 경우는 처리하지 않음
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            OnScreenTouch();
        }
    }

    private void OnScreenTouch()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // 효과음 재생
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.currentSoundBank == null)
            {
                SoundManager.Instance.LoadSoundBank("IntroSoundBank");
            }

            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }

        // 텍스트 깜빡임 효과 중지 (있는 경우)
        if (touchToEngageText != null)
        {
            touchToEngageText.StopBlink();
        }

        // 화면 페이드 아웃 효과와 함께 게임 시작
        StartCoroutine(TransitionToGameStart());
    }

    private IEnumerator TransitionToGameStart()
    {
        // 짧은 지연 시간 (효과음, 시각 효과 등을 위한 시간)
        yield return new WaitForSeconds(0.3f);

        // 게임 시작
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnDestroy()
    {
        // Input Actions 정리
        if (touchActions != null)
        {
            touchActions.Touch.Press.started -= OnTouchStarted;
            touchActions.Disable();
            touchActions = null;
        }

        // 이벤트 리스너 정리
        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OnOptionButtonClick);
        }
    }
}