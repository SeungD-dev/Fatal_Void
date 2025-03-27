using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextBlinkEffect : MonoBehaviour
{
    [Header("깜빡임 설정")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float blinkInterval = 0.5f; // 깜빡임 간격 (초 단위)
    [SerializeField] private float visibleDuration = 0.3f; // 텍스트가
    [SerializeField] private float invisibleDuration = 0.2f; // 텍스트가 보이지 않는 시간
    [SerializeField] private bool startBlinkOnAwake = true; // 시작 시 자동 깜빡임 여부

    private Coroutine blinkCoroutine;
    private bool isBlinking = false;

    void Awake()
    {
        // 타겟 텍스트가 지정되지 않았다면 현재 게임오브젝트에서 찾기
        if (targetText == null)
        {
            targetText = GetComponent<TextMeshProUGUI>();
        }

        if (targetText == null)
        {
            Debug.LogError("TextBlinkEffect: TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (startBlinkOnAwake)
        {
            StartBlink();
        }
    }

    void OnDisable()
    {
        StopBlink();
    }

    /// <summary>
    /// 깜빡임 효과 시작
    /// </summary>
    public void StartBlink()
    {
        if (isBlinking || targetText == null) return;

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// 깜빡임 효과 정지
    /// </summary>
    public void StopBlink()
    {
        if (!isBlinking) return;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 텍스트 보이게 복구
        if (targetText != null)
        {
            targetText.enabled = true;
        }

        isBlinking = false;
    }

    /// <summary>
    /// 깜빡임 간격과 지속 시간 설정
    /// </summary>
    public void SetBlinkTiming(float interval, float visibleTime, float invisibleTime)
    {
        blinkInterval = Mathf.Max(0.1f, interval);
        visibleDuration = Mathf.Max(0.01f, visibleTime);
        invisibleDuration = Mathf.Max(0.01f, invisibleTime);

        // 이미 깜빡임이 진행 중이면 재시작하여 새 설정 적용
        if (isBlinking)
        {
            StopBlink();
            StartBlink();
        }
    }

    private IEnumerator BlinkRoutine()
    {
        WaitForSeconds visibleWait = new WaitForSeconds(visibleDuration);
        WaitForSeconds invisibleWait = new WaitForSeconds(invisibleDuration);
        WaitForSeconds intervalWait = new WaitForSeconds(blinkInterval);

        while (isBlinking)
        {
            // 일정 간격으로 깜빡임

            // 1. 텍스트 보이기
            targetText.enabled = true;
            yield return visibleWait;

            // 2. 텍스트 숨기기
            targetText.enabled = false;
            yield return invisibleWait;

            // 3. 다음 깜빡임 사이클 전 대기
            targetText.enabled = true;
            yield return intervalWait;
        }
    }
}