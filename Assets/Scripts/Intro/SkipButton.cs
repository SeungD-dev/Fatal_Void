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
    private bool skipProcessed = false;  // �ߺ� ȣ�� ������ �÷���

    private void Awake()
    {
        // TouchActions �ʱ�ȭ
        touchActions = new TouchActions();

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
        // Input Actions Ȱ��ȭ
        touchActions.Enable();
        touchActions.Touch.Press.started += OnTouchStarted;
        skipProcessed = false;  // �ʱ�ȭ
    }

    private void OnDisable()
    {
        // Input Actions ��Ȱ��ȭ
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();

        // �ڷ�ƾ ����
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
            touchActions.Touch.Press.started -= OnTouchStarted;
            touchActions.Disable();
            touchActions.Dispose();
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClick);
        }
    }
}