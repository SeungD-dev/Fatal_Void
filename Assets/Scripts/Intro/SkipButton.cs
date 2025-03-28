using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private Button skipButton;
    [SerializeField] private float skipButtonActiveTime = 3.0f;

    private Coroutine hideButtonCoroutine;
    private TouchActions touchActions;
    private bool skipProcessed = false;  // 중복 호출 방지용 플래그

    private void Awake()
    {
        // TouchActions 초기화
        touchActions = new TouchActions();

        // 버튼 설정
        if (skipButton == null)
        {
            skipButton = GetComponent<Button>();
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(OnSkipButtonClick);
        }
    }

    private void OnEnable()
    {
        // Input Actions 활성화
        touchActions.Enable();
        touchActions.Touch.Press.started += OnTouchStarted;
        skipProcessed = false;  // 초기화
    }

    private void OnDisable()
    {
        // Input Actions 비활성화
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();

        // 코루틴 정리
        if (hideButtonCoroutine != null)
        {
            StopCoroutine(hideButtonCoroutine);
            hideButtonCoroutine = null;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        ActivateSkipButton();
    }

    private void ActivateSkipButton()
    {
        if (skipButton == null) return;

        skipButton.gameObject.SetActive(true);

        if (hideButtonCoroutine != null)
        {
            StopCoroutine(hideButtonCoroutine);
        }

        hideButtonCoroutine = StartCoroutine(HideButtonAfterDelay());
    }

    private IEnumerator HideButtonAfterDelay()
    {
        yield return new WaitForSeconds(skipButtonActiveTime);

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        hideButtonCoroutine = null;
    }

    private void OnSkipButtonClick()
    {
        // 중복 실행 방지
        if (skipProcessed) return;
        skipProcessed = true;

        Debug.Log("스킵 버튼 클릭됨 - 인트로 스킵 시도");

        // 효과음 재생
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }

        // 직접 IntroSequenceManager 찾아서 호출 - 가장 신뢰성 있는 방법
        IntroSequenceManager introManager = FindAnyObjectByType<IntroSequenceManager>();
        if (introManager != null)
        {
            Debug.Log("IntroSequenceManager.SkipIntro() 호출");
            introManager.SkipIntro();
        }
        else
        {
            Debug.LogError("IntroSequenceManager를 찾을 수 없음");
        }
    }

    private void OnDestroy()
    {
        // 이벤트 정리
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClick);
        }
    }
}