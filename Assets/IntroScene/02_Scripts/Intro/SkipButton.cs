using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SkipButton : MonoBehaviour
{
    [SerializeField] private Button skipButton;
    [SerializeField] private float skipButtonActiveTime = 3.0f;
    public string nextSceneName; // 스킵 버튼 터치 시 전환될 씬 이름
    private Coroutine hideButtonCoroutine;

    void Start()
    {
        skipButton.gameObject.SetActive(false);
        skipButton.onClick.AddListener(OnClickSkipButton);
    }

    void Update()
    {
        // 화면 아무 곳이나 터치 시 스킵 버튼 활성화
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ActivateSkipButton();
        }
    }

    private void OnEnable()
    {
        // 활성화된 뒤 일정 시간(skipButtonActiveTime) 후 스킵 버튼 비활성화(코루틴)
        if (skipButton.gameObject.activeSelf)
        {
            if (hideButtonCoroutine != null)
            {
                StopCoroutine(hideButtonCoroutine);
            }
            hideButtonCoroutine = StartCoroutine(HideButtonAfterDelay());
        }
    }

    private void ActivateSkipButton()
    {
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
        skipButton.gameObject.SetActive(false);
        hideButtonCoroutine = null;
    }

    /// <summary>
    /// 다음 씬으로 이동
    /// </summary>
    void OnClickSkipButton()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}