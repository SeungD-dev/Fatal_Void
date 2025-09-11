using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BossWarningEffects : MonoBehaviour
{
    [Header("보스 경고 텍스트 설정")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float glitchInterval = 0.2f;
    [SerializeField] private float glitchDuration = 0.05f;
    [SerializeField] private float shakeStrength = 3f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [Header("글리치 효과 설정")]
    [SerializeField] private float colorGlitchIntensity = 0.15f;
    [SerializeField] private float positionGlitchIntensity = 8f;
    [SerializeField] private bool useColorGlitch = true;
    [SerializeField] private bool usePositionGlitch = true;

    // 컴포넌트 캐시
    private Color originalColor;
    private Vector2 originalPosition;
    private RectTransform textRectTransform;

    // 시퀀스 관련 변수
    private Sequence shakeSequence;
    private Coroutine glitchCoroutine;

    void Awake()
    {
        if (warningText != null)
        {
            textRectTransform = warningText.GetComponent<RectTransform>();
            originalColor = warningText.color;
            originalPosition = textRectTransform.anchoredPosition;
        }
    }

    void OnDestroy()
    {
        // 시퀀스 정리
        shakeSequence?.Kill();

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }

        // 원래 상태로 복원
        if (warningText != null)
        {
            warningText.color = originalColor;
            textRectTransform.anchoredPosition = originalPosition;
        }
    }

    public void StartGlitchEffect()
    {
        if (warningText == null) return;

        // 글리치 코루틴 시작
        glitchCoroutine = StartCoroutine(GlitchEffectRoutine());

        // 흔들림 효과를 위한 시퀀스 생성
        shakeSequence = DOTween.Sequence();

        // 지속적인 약한 흔들림 효과
        shakeSequence.Append(
            textRectTransform.DOShakePosition(
                duration: 2.0f, // 2초 동안 지속
                strength: new Vector3(shakeStrength, shakeStrength * 0.5f, 0),
                vibrato: shakeVibrato,
                randomness: shakeRandomness,
                snapping: false,
                fadeOut: false
            )
        );
    }

    public void StopGlitchEffect()
    {
        // 모든 효과 중지
        shakeSequence?.Kill();

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        // 원래 상태로 복원
        if (warningText != null)
        {
            warningText.color = originalColor;
            textRectTransform.anchoredPosition = originalPosition;
        }
    }

    private IEnumerator GlitchEffectRoutine()
    {
        WaitForSeconds glitchWait = new WaitForSeconds(glitchDuration);
        WaitForSeconds intervalWait = new WaitForSeconds(glitchInterval - glitchDuration);

        while (true)
        {
            // 글리치 효과 적용
            ApplyGlitchEffect(true);
            yield return glitchWait;

            // 원래 상태로 복원
            ApplyGlitchEffect(false);
            yield return intervalWait;
        }
    }

    private void ApplyGlitchEffect(bool apply)
    {
        if (warningText == null) return;

        if (apply)
        {
            // 1. 색상 글리치
            if (useColorGlitch)
            {
                Color glitchColor = new Color(
                    originalColor.r + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.g + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.b + Random.Range(-colorGlitchIntensity, colorGlitchIntensity),
                    originalColor.a
                );
                warningText.color = glitchColor;
            }

            // 2. 위치 글리치 - DOTween 흔들림에 추가로 추가 효과
            if (usePositionGlitch)
            {
                Vector2 glitchPosition = new Vector2(
                    originalPosition.x + Random.Range(-positionGlitchIntensity, positionGlitchIntensity),
                    originalPosition.y + Random.Range(-positionGlitchIntensity, positionGlitchIntensity)
                );
                textRectTransform.anchoredPosition = glitchPosition;
            }
        }
        else
        {
            // 원래 상태로 복원 (단, 위치는 DOTween에서 처리)
            warningText.color = originalColor;

            // 위치 글리치를 사용하지 않을 때, 위치도 복원
            if (usePositionGlitch)
            {
                textRectTransform.anchoredPosition = originalPosition;
            }
        }
    }

    // 외부에서 효과 강도를 조절할 수 있는 메서드
    public void SetGlitchIntensity(float colorIntensity, float positionIntensity)
    {
        colorGlitchIntensity = colorIntensity;
        positionGlitchIntensity = positionIntensity;
    }

    public void SetGlitchTiming(float interval, float duration)
    {
        glitchInterval = Mathf.Max(0.05f, interval);
        glitchDuration = Mathf.Min(duration, glitchInterval);
    }
}