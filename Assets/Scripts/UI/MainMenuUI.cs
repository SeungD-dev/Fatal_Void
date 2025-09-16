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
    [SerializeField] private float touchDelay = 0.5f; // �ʱ� ���� �ð�
    [SerializeField] private float touchCooldown = 0.5f; // �ɼ� �г� ���� �� ��ٿ� �ð�

    private TouchActions touchActions;
    private bool canStartGame = false;
    private bool isTransitioning = false;
    private float lastOptionPanelCloseTime = 0f;

    private void Awake()
    {
        // Input Actions �ʱ�ȭ
        touchActions = new TouchActions();
    }

    private void OnEnable()
    {
        // Input Actions Ȱ��ȭ
        touchActions.Enable();

        // ��ġ �̺�Ʈ ���
        touchActions.Touch.Press.started += OnTouchStarted;
    }

    private void OnDisable()
    {
        // Input Actions ��Ȱ��ȭ �� �̺�Ʈ ����
        touchActions.Touch.Press.started -= OnTouchStarted;
        touchActions.Disable();
    }

    private void Start()
    {
        // GameManager�� �ɼ� �г� ���� ����
        if (GameManager.Instance != null && optionPanel != null)
        {
            GameManager.Instance.SetStartSceneReferences(optionPanel);
        }

        // �ɼ� ��ư ������ ���
        if (optionButton != null)
        {
            optionButton.onClick.AddListener(OnOptionButtonClick);
        }

        // �ɼ� �ݱ� ��ư ������ ���
        if (closeOptionButton != null)
        {
            closeOptionButton.onClick.AddListener(OnCloseOptionButtonClick);
        }

        // �ణ�� ���� �� ��ġ Ȱ��ȭ
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
            // �ɼ� �г� ���� �ð� ���
            lastOptionPanelCloseTime = Time.time;
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // 1. ���� ���� ������ �������� Ȯ��
        if (!canStartGame || isTransitioning) return;

        // 2. �ɼ� �г��� Ȱ��ȭ�� �������� Ȯ��
        if (optionPanel != null && optionPanel.activeSelf) return;

        // 3. �ɼ� �г� ���� �������� Ȯ�� (��ٿ� ����)
        if (Time.time - lastOptionPanelCloseTime < touchCooldown) return;

        // 4. UI ��� ������ ��ġ�� ������� Ȯ��
        if (IsPointerOverUI()) return;

        // ��� ������ ����ϸ� ���� ����
        OnScreenTouch();
    }

    private bool IsPointerOverUI()
    {
        // ��ġ/Ŭ�� ��ġ ��������
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
            return false; // �Է��� ������ UI ���� �ƴ�
        }

        // EventSystem���� �ش� ��ġ�� UI ��Ұ� �ִ��� Ȯ��
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

        // ȿ���� ���
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.currentSoundBank == null)
            {
                SoundManager.Instance.LoadSoundBank("IntroSoundBank");
            }

            SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        }

        // �ؽ�Ʈ ������ ȿ�� ����
        if (touchToEngageText != null)
        {
            touchToEngageText.StopBlink();
        }

        // ���� ���� ��ȯ
        StartCoroutine(TransitionToGameStart());
    }

    private IEnumerator TransitionToGameStart()
    {
        // ª�� ���� �ð�
        yield return new WaitForSeconds(0.3f);

        // ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void OnDestroy()
    {
        // Input Actions ����
        if (touchActions != null)
        {
            touchActions.Touch.Press.started -= OnTouchStarted;
            touchActions.Disable();
            touchActions.Dispose();
        }

        // �̺�Ʈ ������ ����
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