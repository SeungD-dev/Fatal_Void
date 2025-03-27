using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button closeOptionButton; 
    [SerializeField] private TextBlinkEffect touchToEngageText;

    [Header("Touch Detection")]
    [SerializeField] private float touchDelay = 0.5f; // 초기 지연 시간
    [SerializeField] private float touchCooldown = 0.5f; // 옵션 패널 닫은 후 쿨다운 시간

    private TouchActions touchActions;
    private bool canStartGame = false;
    private bool isTransitioning = false;
    private float lastOptionPanelCloseTime = 0f;

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

        // 옵션 닫기 버튼 리스너 등록
        if (closeOptionButton != null)
        {
            closeOptionButton.onClick.AddListener(OnCloseOptionButtonClick);
        }

        // 약간의 지연 후 터치 활성화
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

    private void OnCloseOptionButtonClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleOptionPanel();
            // 옵션 패널 닫은 시간 기록
            lastOptionPanelCloseTime = Time.time;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // 1. 게임 시작 가능한 상태인지 확인
        if (!canStartGame || isTransitioning) return;

        // 2. 옵션 패널이 활성화된 상태인지 확인
        if (optionPanel != null && optionPanel.activeSelf) return;

        // 3. 옵션 패널 닫은 직후인지 확인 (쿨다운 적용)
        if (Time.time - lastOptionPanelCloseTime < touchCooldown) return;

        // 4. UI 요소 위에서 터치한 경우인지 확인
        if (IsPointerOverUI()) return;

        // 모든 조건을 통과하면 게임 시작
        OnScreenTouch();
    }

    private bool IsPointerOverUI()
    {
        // 터치/클릭 위치 가져오기
        Vector2 position;
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            position = Touchscreen.current.touches[0].position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
        }
        else
        {
            return false; // 입력이 없으면 UI 위가 아님
        }

        // EventSystem으로 해당 위치에 UI 요소가 있는지 확인
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = position;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
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

        // 텍스트 깜빡임 효과 중지
        if (touchToEngageText != null)
        {
            touchToEngageText.StopBlink();
        }

        // 게임 시작 전환
        StartCoroutine(TransitionToGameStart());
    }

    private IEnumerator TransitionToGameStart()
    {
        // 짧은 지연 시간
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
        }

        // 이벤트 리스너 정리
        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OnOptionButtonClick);
        }

        if (closeOptionButton != null)
        {
            closeOptionButton.onClick.RemoveListener(OnCloseOptionButtonClick);
        }
    }
}