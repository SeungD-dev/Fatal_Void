using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// 인트로 시퀀스 실행 및 관리를 담당하는 클래스
/// 최적화된 버전으로 GameManager와 통합됨
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image blackOverlay;           // 페이드 인/아웃용 검은색 이미지
    [SerializeField] private RectTransform scrollImage;    // 스크롤될 세로 이미지
    [SerializeField] private TextMeshProUGUI introText;    // 텍스트 표시용 UI

    [Header("Panel Fade Settings")]
    [SerializeField] private float stepDuration = 0.3f;    // 각 단계 사이의 시간 간격
    [SerializeField] private int fadeSteps = 4;            // 알파값 단계 수
    [SerializeField] private float initialPanelAlpha = 1.0f;  // 시작 시 패널 알파값
    [SerializeField] private float finalPanelAlpha = 0.0f;    // 메인 화면에서의 패널 알파값

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;      // 초당 스크롤 픽셀
    [SerializeField] private float initialDelay = 0.5f;    // 페이드 인 후 스크롤 시작 전 대기 시간
    [SerializeField] private float scrollEndY = 2000f;     // 스크롤이 끝나는 Y 위치
    [SerializeField] private float intervalBetweenTexts = 0.5f;  // 텍스트 사이 간격

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

    [SerializeField] private List<IntroTextItem> introTextSequence = new List<IntroTextItem>();

    // 이미지 위치 관련 변수
    private float scrollY = 0f;
    private bool isScrolling = false;
    private bool sequenceCompleted = false;
    private bool isTransitioning = false;

    // 코루틴 참조 관리
    private Coroutine introSequenceCoroutine;

    // 캐시된 WaitForSeconds 객체
    private WaitForSeconds initialDelayWait;
    private WaitForSeconds intervalWait;
    private WaitForSeconds stepDelayWait;
    private Dictionary<float, WaitForSeconds> typingDelays = new Dictionary<float, WaitForSeconds>();
    private Dictionary<float, WaitForSeconds> displayTimeWaits = new Dictionary<float, WaitForSeconds>();

    private void Awake()
    {
        // 성능 최적화를 위해 자주 사용되는 WaitForSeconds 객체 캐싱
        initialDelayWait = new WaitForSeconds(initialDelay);
        intervalWait = new WaitForSeconds(intervalBetweenTexts);
        stepDelayWait = new WaitForSeconds(stepDuration);

        // 타입라이터 효과와 표시 시간에 대한 WaitForSeconds 캐싱
        CacheWaitForSecondsObjects();
    }

    private void CacheWaitForSecondsObjects()
    {
        // 텍스트 표시 시간 캐싱
        HashSet<float> displayTimes = new HashSet<float>();
        HashSet<float> typingSpeeds = new HashSet<float>();

        foreach (var item in introTextSequence)
        {
            displayTimes.Add(item.displayTime);
            if (item.useTypewriterEffect)
            {
                typingSpeeds.Add(item.typingSpeed);
            }
        }

        // 고유한 표시 시간에 대한 WaitForSeconds 객체 생성
        foreach (float time in displayTimes)
        {
            if (!displayTimeWaits.ContainsKey(time))
            {
                displayTimeWaits[time] = new WaitForSeconds(time);
            }
        }

        // 고유한 타이핑 속도에 대한 WaitForSeconds 객체 생성
        foreach (float speed in typingSpeeds)
        {
            if (!typingDelays.ContainsKey(speed))
            {
                typingDelays[speed] = new WaitForSeconds(speed);
            }
        }
    }

    private void Start()
    {
        PrepareUI();

        SetupSounds();


        // 인트로 시퀀스 시작
        introSequenceCoroutine = StartCoroutine(PlayIntroSequence());
    }
    private void SetupSounds()
    {
        if (SoundManager.Instance != null)
        {
            // 현재 재생 중인 BGM 확인
            bool isBgmPlaying = SoundManager.Instance.IsBGMPlaying("BGM_Intro");

            // 인트로 사운드뱅크 로드 (아직 로드되지 않았다면)
            if (SoundManager.Instance.currentSoundBank == null ||
                SoundManager.Instance.currentSoundBank.name != "IntroSoundBank")
            {
                SoundManager.Instance.LoadSoundBank("IntroSoundBank");
            }

            // 이미 재생 중인 경우가 아니라면 BGM 재생
            if (!isBgmPlaying)
            {
                SoundManager.Instance.PlaySound("BGM_Intro", 1f, true);
            }
        }
        else
        {
            Debug.LogWarning("SoundManager not found!");
        }
    }
    private void PrepareUI()
    {
        // 초기 설정
        if (introText != null)
            introText.alpha = 0f;

        // 초기 패널 설정
        if (blackOverlay != null)
            blackOverlay.color = new Color(0, 0, 0, initialPanelAlpha);

        // 초기 스크롤 위치 설정
        scrollY = 0f;
        if (scrollImage != null)
            scrollImage.anchoredPosition = new Vector2(scrollImage.anchoredPosition.x, scrollY);
    }

    private IEnumerator PlayIntroSequence()
    {
        // 1. 시작 시 패널이 단계적으로 사라짐
        yield return StepFadePanel(initialPanelAlpha, finalPanelAlpha, fadeSteps, stepDuration);

        // 2. 초기 딜레이
        yield return initialDelayWait;

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

        // 7. 인트로 완료 후 타이틀씬으로 이동
        CompleteIntro();
    }

    private IEnumerator StepFadePanel(float startAlpha, float targetAlpha, int steps, float stepDelay)
    {
        if (blackOverlay == null) yield break;

        // 시작값과 목표값 사이의 간격 계산
        float alphaStep = (targetAlpha - startAlpha) / steps;
        Color color = blackOverlay.color;

        for (int i = 0; i <= steps; i++)
        {
            // 현재 단계에 맞는 알파값 계산
            color.a = startAlpha + (alphaStep * i);
            blackOverlay.color = color;

            // 다음 단계 전 대기
            yield return stepDelayWait;
        }
    }

    private IEnumerator ShowTextSequence()
    {
        if (introText == null) yield break;

        Color textColor = introText.color;
        Color panelColor = blackOverlay != null ? blackOverlay.color : Color.black;

        for (int i = 0; i < introTextSequence.Count; i++)
        {
            IntroTextItem textItem = introTextSequence[i];

            // 패널 표시 (텍스트와 함께)
            if (textItem.showPanelWithText && blackOverlay != null)
            {
                panelColor.a = textItem.panelAlpha;
                blackOverlay.color = panelColor;
            }

            if (textItem.useTypewriterEffect)
            {
                // 텍스트 초기화
                introText.text = "";
                textColor.a = 1f;
                introText.color = textColor;

                // 타이핑 효과
                yield return TypeText(textItem.text, textItem.typingSpeed);

                // 표시 시간 대기 (캐시된 WaitForSeconds 사용)
                yield return GetDisplayTimeWait(textItem.displayTime);
            }
            else
            {
                // 텍스트 설정
                introText.text = textItem.text;
                textColor.a = 1f;
                introText.color = textColor;

                // 표시 시간 대기 (캐시된 WaitForSeconds 사용)
                yield return GetDisplayTimeWait(textItem.displayTime);
            }

            // 텍스트 숨김
            textColor.a = 0f;
            introText.color = textColor;

            // 패널 알파값 되돌리기
            if (textItem.showPanelWithText && blackOverlay != null)
            {
                panelColor.a = finalPanelAlpha;
                blackOverlay.color = panelColor;
            }

            // 모든 텍스트에 동일한 간격 적용
            yield return intervalWait;
        }
    }

    // 텍스트가 타이핑되는 것처럼 한 글자씩 출력하는 함수
    private IEnumerator TypeText(string fullText, float typingSpeed)
    {
        WaitForSeconds typeDelay = GetTypingSpeedWait(typingSpeed);

        introText.text = "";
        for (int i = 0; i <= fullText.Length; i++)
        {
            introText.text = fullText.Substring(0, i);
            yield return typeDelay;
        }
    }

    private WaitForSeconds GetTypingSpeedWait(float speed)
    {
        // 캐시된 WaitForSeconds 객체 반환
        if (typingDelays.TryGetValue(speed, out WaitForSeconds wait))
        {
            return wait;
        }

        // 없으면 새로 생성하고 캐시
        wait = new WaitForSeconds(speed);
        typingDelays[speed] = wait;
        return wait;
    }

    private WaitForSeconds GetDisplayTimeWait(float time)
    {
        // 캐시된 WaitForSeconds 객체 반환
        if (displayTimeWaits.TryGetValue(time, out WaitForSeconds wait))
        {
            return wait;
        }

        // 없으면 새로 생성하고 캐시
        wait = new WaitForSeconds(time);
        displayTimeWaits[time] = wait;
        return wait;
    }

    private void Update()
    {
        if (isScrolling && scrollImage != null)
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

    /// <summary>
    /// 인트로를 완료하고 다음 씬으로 이동
    /// </summary>
    private void CompleteIntro()
    {
        // 이미 전환 중인 경우 중복 실행 방지
        if (isTransitioning) return;
        isTransitioning = true;

        // BGM 페이드 아웃 (CrossfadeBGM을 통해)
        if (SoundManager.Instance != null && SoundManager.Instance.IsBGMPlaying("BGM_Intro"))
        {
            // 다음 씬으로 전환 전 배경음 페이드 아웃
            // SoundManager의 CrossfadeBGM이 내부 메서드이므로 직접 호출하지 않고
            // 다른 빈 사운드로 페이드하거나 볼륨 조절을 통해 처리
            SoundManager.Instance.SetBGMVolume(0f); // 볼륨을 0으로 설정하여 페이드 아웃 효과
        }

        // GameManager를 통해 타이틀씬으로 이동
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteIntro();
        }
        else
        {
            // GameManager가 없는 경우 직접 타이틀씬으로 이동
            SceneManager.LoadScene(1); // TitleScene 인덱스
        }
    }

    /// <summary>
    /// 인트로를 건너뛰고 타이틀씬으로 즉시 이동
    /// </summary>
    public void SkipIntro()
    {
        Debug.Log("IntroSequenceManager.SkipIntro() 시작");

        try
        {
            if (isTransitioning)
            {
                Debug.Log("이미 전환 중이므로 SkipIntro 무시됨");
                return;
            }

            isTransitioning = true;
            Debug.Log("isTransitioning = true로 설정됨");

            // 효과음 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound("Button_sfx", 0.5f, false);
                Debug.Log("효과음 재생됨");
            }

            // 진행 중인 코루틴 중단
            Debug.Log("모든 코루틴 중단");
            StopAllCoroutines();

            // 즉시 검은 화면으로 전환
            if (blackOverlay != null)
            {
                Debug.Log("검은 화면으로 전환");
                Color color = blackOverlay.color;
                color.a = initialPanelAlpha;
                blackOverlay.color = color;
            }

            // 다음 씬으로 전환 - 안전하게 코루틴으로 분리
            Debug.Log("DirectCompleteIntro 코루틴 시작");
            StartCoroutine(DirectCompleteIntro());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SkipIntro에서 예외 발생: {e.Message}\n{e.StackTrace}");

            // 마지막 시도로 직접 씬 로드
            try
            {
                Debug.Log("예외 발생 후 직접 타이틀씬 로드 시도");
                SceneManager.LoadScene(1);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"직접 씬 로드 시도 중 예외 발생: {e2.Message}");
            }
        }
    }

    private IEnumerator DirectCompleteIntro()
    {
        yield return null; // 안전하게 1프레임 대기

        Debug.Log("DirectCompleteIntro 실행 중");

        // 인트로 BGM 페이드 아웃 (0.5초 동안)
        if (SoundManager.Instance != null && SoundManager.Instance.IsBGMPlaying("BGM_Intro"))
        {
            Debug.Log("인트로 BGM 페이드 아웃");
            SoundManager.Instance.FadeOutBGM(0.5f);
        }

        // 페이드 아웃이 완료될 때까지 약간 대기 (선택사항)
        yield return new WaitForSecondsRealtime(0.2f);  // 페이드 아웃의 일부만 기다리고 씬 전환

        try
        {
            // GameManager를 통한 씬 전환으로 수정
            if (GameManager.Instance != null)
            {
                Debug.Log("GameManager.CompleteIntro() 호출");
                GameManager.Instance.CompleteIntro();
            }
            else
            {
                // GameManager가 없는 경우에만 직접 씬 전환
                Debug.Log("GameManager 없음, 직접 타이틀씬으로 전환");
                SceneManager.LoadScene(1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"씬 전환 중 예외 발생: {e.Message}\n{e.StackTrace}");

            // 마지막 시도로 직접 씬 로드
            try
            {
                SceneManager.LoadScene(1);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"최종 씬 로드 시도 중 예외 발생: {e2.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        // 리소스 정리
        StopAllCoroutines();
        DOTween.Kill(transform);

        // 딕셔너리 정리
        typingDelays.Clear();
        displayTimeWaits.Clear();
    }
}