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
    // 마우스 입력 지원용
    private InputAction mousePress;
    private bool skipProcessed = false;  // 중복 호출 방지를 위한 플래그

    private void Awake()
    {
        // TouchActions 초기화
        touchActions = new TouchActions();

        // 마우스 입력 액션 초기화
        mousePress = new InputAction("MousePress", InputActionType.Button, "<Mouse>/leftButton");

        // ��ư ����
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
        touchActions.Touch.Press.started += OnInputStarted;

        // 마우스 입력 활성화
        mousePress.Enable();
        mousePress.started += OnInputStarted;

        skipProcessed = false;  // 초기화
    }

    private void OnDisable()
    {
        // Input Actions 비활성화
        touchActions.Touch.Press.started -= OnInputStarted;
        touchActions.Disable();

        // 마우스 입력 비활성화
        mousePress.started -= OnInputStarted;
        mousePress.Disable();

        // �ڷ�ƾ ����
        if (hideButtonCoroutine != null)
        {
            StopCoroutine(hideButtonCoroutine);
            hideButtonCoroutine = null;
        }
    }

    private void OnInputStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Input detected - activating skip button");
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
        // �ߺ� ���� ����
        if (skipProcessed) return;
        skipProcessed = true;

        Debug.Log("��ŵ ��ư Ŭ���� - ��Ʈ�� ��ŵ �õ�");

        // ȿ���� ���
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }

        // ���� IntroSequenceManager ã�Ƽ� ȣ�� - ���� �ŷڼ� �ִ� ���
        IntroSequenceManager introManager = FindAnyObjectByType<IntroSequenceManager>();
        if (introManager != null)
        {
            Debug.Log("IntroSequenceManager.SkipIntro() ȣ��");
            introManager.SkipIntro();
        }
        else
        {
            Debug.LogError("IntroSequenceManager�� ã�� �� ����");
        }
    }

    private void OnDestroy()
    {
        if (touchActions != null)
        {
            touchActions.Touch.Press.started -= OnInputStarted;
            touchActions.Disable();
            touchActions.Dispose();
        }

        if (mousePress != null)
        {
            mousePress.started -= OnInputStarted;
            mousePress.Disable();
            mousePress.Dispose();
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClick);
        }
    }
}