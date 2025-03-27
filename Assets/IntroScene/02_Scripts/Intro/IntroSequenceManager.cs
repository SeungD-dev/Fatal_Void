using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class IntroSequenceManager : MonoBehaviour
{
    [Header("UI References")]
    public Image blackOverlay;           // 페이드 인/아웃용 검은색 이미지
    public RectTransform scrollImage;    // 스크롤될 세로 이미지
    public TextMeshProUGUI introText;    // 텍스트 표시용 UI

    [Header("Scene Transition")]
    public string nextSceneName;         // 인트로 종료 후 전환될 씬 이름
    public bool loadNextSceneWhenDone = true; // 인트로 종료 후 씬 전환 여부

    [Header("Panel Fade Settings")]
    public float stepDuration = 0.3f;    // 각 단계 사이의 시간 간격
    public int fadeSteps = 4;            // 알파값 단계 수 (기본 4단계: 100%, 75%, 50%, 25%, 0%)
    public float initialPanelAlpha = 1.0f;  // 시작 시 패널 알파값
    public float finalPanelAlpha = 0.0f;    // 메인 화면에서의 패널 알파값

    [Header("Scroll Settings")]
    public float scrollSpeed = 50f;      // 초당 스크롤 픽셀 (값이 클수록 빠름)
    public float initialDelay = 0.5f;    // 페이드 인 후 스크롤 시작 전 대기 시간
    public float scrollEndY = 2000f;     // 스크롤이 끝나는 Y 위치 (양수로 변경)
    public float intervalBetweenTexts = 0.5f;  // 텍스트 사이 간격

    [System.Serializable]
    public class IntroTextItem
    {
        public string text;
        public float displayTime = 3.0f;  // 텍스트가 화면에 표시되는 시간
        public bool useTypewriterEffect = true;
        public float typingSpeed = 0.05f;  // 타이핑 속도 (글자당 초)

        [Header("Panel Settings")]
        public bool showPanelWithText = true;  // 텍스트 표시 시 패널 표시 여부
        public float panelAlpha = 0.5f;        // 텍스트 표시 시 패널 알파값 (0-1)
    }
    public List<IntroTextItem> introTextSequence = new List<IntroTextItem>();

    // 이미지 위치 관련 변수
    private float scrollY = 0f;
    private bool isScrolling = false;
    private bool sequenceCompleted = false;

    void Start()
    {
        // 초기 설정
        if (introText != null)
            introText.alpha = 0f;

        // 초기 패널 설정
        blackOverlay.color = new Color(0, 0, 0, initialPanelAlpha);

        // 초기 스크롤 위치 설정
        scrollY = 0f;
        scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);

        // 인트로 시퀀스 시작
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // 1. 시작 시 패널이 단계적으로 사라짐
        yield return StepFadePanel(initialPanelAlpha, finalPanelAlpha, fadeSteps, stepDuration);

        // 2. 초기 딜레이
        yield return new WaitForSeconds(initialDelay);

        // 3. 스크롤 시작
        isScrolling = true;

        // 4. 텍스트 시퀀스 시작
        yield return ShowTextSequence();

        // 5. 스크롤 종료를 기다림 (Update 함수에서 처리)
        while (!sequenceCompleted)
        {
            yield return null;
        }

        // 6. 종료 시 패널이 단계적으로 나타남
        yield return StepFadePanel(finalPanelAlpha, initialPanelAlpha, fadeSteps, stepDuration);

        // 7. 씬 전환
        if (loadNextSceneWhenDone && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator StepFadePanel(float startAlpha, float targetAlpha, int steps, float stepDelay)
    {
        // 시작값과 목표값 사이의 간격 계산
        float alphaStep = (targetAlpha - startAlpha) / steps;

        for (int i = 0; i <= steps; i++)
        {
            // 현재 단계에 맞는 알파값 계산
            float currentAlpha = startAlpha + (alphaStep * i);

            // 알파값을 즉시 변경
            Color color = blackOverlay.color;
            color.a = currentAlpha;
            blackOverlay.color = color;

            // 다음 단계 전 대기
            yield return new WaitForSeconds(stepDelay);
        }
    }

    IEnumerator ShowTextSequence()
    {
        foreach (IntroTextItem textItem in introTextSequence)
        {
            // 패널 즉시 표시 (텍스트와 함께)
            if (textItem.showPanelWithText)
            {
                // 패널 알파값 즉시 변경
                Color panelColor = blackOverlay.color;
                panelColor.a = textItem.panelAlpha;
                blackOverlay.color = panelColor;
            }

            if (textItem.useTypewriterEffect)
            {
                // 텍스트 초기화
                introText.text = "";

                // 텍스트 즉시 보이게 설정
                Color textColor = introText.color;
                textColor.a = 1f;
                introText.color = textColor;

                // 타이핑 효과
                yield return TypeText(textItem.text, textItem.typingSpeed);

                // 표시 시간 대기
                yield return new WaitForSeconds(textItem.displayTime);
            }
            else
            {
                // 텍스트 설정
                introText.text = textItem.text;

                // 텍스트 즉시 보이게 설정
                Color textColor = introText.color;
                textColor.a = 1f;
                introText.color = textColor;

                // 표시 시간 대기
                yield return new WaitForSeconds(textItem.displayTime);
            }

            // 텍스트 즉시 숨김
            Color hideTextColor = introText.color;
            hideTextColor.a = 0f;
            introText.color = hideTextColor;

            // 패널 알파값 되돌리기
            if (textItem.showPanelWithText)
            {
                // 패널 알파값 즉시 변경
                Color panelColor = blackOverlay.color;
                panelColor.a = finalPanelAlpha;
                blackOverlay.color = panelColor;
            }

            // 모든 텍스트에 동일한 간격 적용
            yield return new WaitForSeconds(intervalBetweenTexts);
        }
    }

    // 텍스트가 타이핑되는 것처럼 한 글자씩 출력하는 함수
    IEnumerator TypeText(string fullText, float typingSpeed)
    {
        introText.text = "";

        for (int i = 0; i <= fullText.Length; i++)
        {
            introText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void Update()
    {
        if (isScrolling)
        {
            // 일정 속도로 위쪽으로 스크롤 (Y 값 증가)
            scrollY += scrollSpeed * Time.deltaTime;
            scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);

            // 스크롤 종료 조건 (끝점에 도달하면)
            if (scrollY > scrollEndY)
            {
                isScrolling = false;
                sequenceCompleted = true;
            }
        }
    }

    // 인스펙터에서 테스트하기 위한 메소드
    public void SkipIntro()
    {
        StopAllCoroutines();
        DOTween.KillAll();
        isScrolling = false;
        sequenceCompleted = true;

        // 즉시 검은 화면으로
        Color color = blackOverlay.color;
        color.a = initialPanelAlpha;
        blackOverlay.color = color;

        // 씬 전환
        if (loadNextSceneWhenDone && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}