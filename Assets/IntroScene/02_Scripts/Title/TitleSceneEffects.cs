using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TitleSceneEffects : MonoBehaviour
{
    [Header("타이틀 이미지 설정")]
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private float glitchInterval = 0.5f;
    [SerializeField] private float glitchDuration = 0.1f;
    [SerializeField] private float shakeStrength = 5f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [Header("글리치 효과 설정")]
    [SerializeField] private float colorGlitchIntensity = 0.1f;
    [SerializeField] private float positionGlitchIntensity = 10f;
    [SerializeField] private bool useColorGlitch = true;
    [SerializeField] private bool usePositionGlitch = true;

    [Header("슬라이딩 이미지 설정")]
    [SerializeField] private GameObject slidingImagePrefab; // 슬라이딩 이미지 프리팹
    [SerializeField] private Transform slidingImagesParent; // 슬라이딩 이미지 부모 오브젝트
    [SerializeField] private bool isVerticalSlide = true; // true: 위/아래, false: 좌/우
    [SerializeField] private bool startFromTop = true; // true: 위에서 아래로, false: 아래서 위로
    [SerializeField] private bool startFromLeft = true; // true: 왼쪽에서 오른쪽으로, false: 오른쪽에서 왼쪽으로
    [SerializeField] private float slideDuration = 3f; // 이동에 걸리는 시간
    [SerializeField] private float spawnInterval = 2f; // 이미지 생성 간격
    [SerializeField] private int poolSize = 10; // 오브젝트 풀 크기

    [Header("이미지 효과 설정")]
    [SerializeField] private float minScale = 0.5f; // 최소 크기
    [SerializeField] private float maxScale = 1.5f; // 최대 크기
    [SerializeField] private float blinkChance = 0.3f; // 깜빡임 확률 (0-1)
    [SerializeField] private float blinkInterval = 0.1f; // 깜빡임 간격
    [SerializeField] private int maxBlinkCount = 5; // 최대 깜빡임 횟수

    // 컴포넌트 캐싱
    private Image titleImageComponent;
    private Color originalColor;
    private Vector2 originalPosition;

    // 시퀀스 저장용 변수
    private Sequence titleSequence;
    private Coroutine glitchCoroutine;
    private Coroutine spawnCoroutine;

    // 오브젝트 풀
    private List<RectTransform> slidingImagePool;
    private Queue<RectTransform> availableImages;
    private Canvas parentCanvas;

    void Awake()
    {
        // 캔버스 참조 얻기
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponent<Canvas>();
        }

        // 오브젝트 풀 초기화
        InitializeObjectPool();
    }

    void Start()
    {
        // 컴포넌트 초기화
        if (titleImage != null)
        {
            titleImageComponent = titleImage.GetComponent<Image>();
            if (titleImageComponent != null)
            {
                originalColor = titleImageComponent.color;
                originalPosition = titleImage.anchoredPosition;
            }
        }

        // 애니메이션 시작
        InitializeGlitchEffect();
        spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
    }

    void OnDestroy()
    {
        // 시퀀스 정리
        titleSequence?.Kill();

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // 원래 상태로 복원
        if (titleImageComponent != null)
        {
            titleImageComponent.color = originalColor;
            titleImage.anchoredPosition = originalPosition;
        }

        // 활성화된 모든 슬라이딩 이미지 정리
        foreach (var img in slidingImagePool)
        {
            if (img != null && img.gameObject.activeSelf)
            {
                DOTween.Kill(img);
                img.gameObject.SetActive(false);
            }
        }
    }

    private void InitializeObjectPool()
    {
        if (slidingImagePrefab == null || slidingImagesParent == null)
        {
            Debug.LogError("슬라이딩 이미지 프리팹 또는 부모 오브젝트가 할당되지 않았습니다.");
            return;
        }

        slidingImagePool = new List<RectTransform>();
        availableImages = new Queue<RectTransform>();

        // 풀 사이즈에 맞게 모든 오브젝트 미리 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject newObj = Instantiate(slidingImagePrefab, slidingImagesParent);
            RectTransform rectTransform = newObj.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Debug.LogError("슬라이딩 이미지 프리팹에 RectTransform 컴포넌트가 없습니다.");
                continue;
            }

            // 이미지 컴포넌트 캐싱 (깜빡임 효과용)
            Image imageComponent = newObj.GetComponent<Image>();
            if (imageComponent == null)
            {
                Debug.LogWarning("슬라이딩 이미지 프리팹에 Image 컴포넌트가 없습니다. 깜빡임 효과가 적용되지 않습니다.");
            }

            newObj.name = "SlidingImage_" + i;
            newObj.SetActive(false);
            slidingImagePool.Add(rectTransform);
            availableImages.Enqueue(rectTransform);
        }

        Debug.Log($"슬라이딩 이미지 풀 초기화 완료: {poolSize}개 생성됨");
    }

    private RectTransform GetSlidingImageFromPool()
    {
        if (availableImages.Count == 0)
        {
            // 모든 오브젝트가 사용 중이면 가장 오래된 것을 재활용
            RectTransform oldestImage = slidingImagePool[0];
            DOTween.Kill(oldestImage); // 기존 애니메이션 정리
            oldestImage.gameObject.SetActive(false);
            return oldestImage;
        }

        RectTransform image = availableImages.Dequeue();
        image.gameObject.SetActive(true);
        return image;
    }

    private void ReturnImageToPool(RectTransform image)
    {
        image.gameObject.SetActive(false);
        availableImages.Enqueue(image);
    }

    private void InitializeGlitchEffect()
    {
        if (titleImage == null) return;

        // 글리치 코루틴 시작
        glitchCoroutine = StartCoroutine(GlitchEffectRoutine());

        // 흔들림 효과를 위한 시퀀스 생성 - 순수하게 위치 흔들림만 적용
        titleSequence = DOTween.Sequence();

        // 지속적인 작은 흔들림 효과
        titleSequence.Append(
            titleImage.DOShakePosition(
                duration: 1.5f,
                strength: new Vector3(shakeStrength * 0.7f, shakeStrength * 0.3f, 0),
                vibrato: shakeVibrato,
                randomness: shakeRandomness,
                snapping: false,
                fadeOut: true
            )
        ).SetLoops(-1, LoopType.Restart);
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
        if (titleImageComponent == null) return;

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
                titleImageComponent.color = glitchColor;
            }

            // 2. 위치 글리치 - DOTween 흔들림과 별개의 추가 효과
            if (usePositionGlitch)
            {
                Vector2 glitchPosition = new Vector2(
                    originalPosition.x + Random.Range(-positionGlitchIntensity, positionGlitchIntensity),
                    originalPosition.y + Random.Range(-positionGlitchIntensity, positionGlitchIntensity)
                );
                titleImage.anchoredPosition = glitchPosition;
            }
        }
        else
        {
            // 원래 상태로 색상만 복원 (위치는 DOTween에서 처리)
            titleImageComponent.color = originalColor;

            // 위치 글리치를 적용했을 경우, 위치도 복원
            if (usePositionGlitch)
            {
                titleImage.anchoredPosition = originalPosition;
            }
        }
    }

    private IEnumerator SpawnSlidingImagesRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            SpawnSlidingImage();
            yield return wait;
        }
    }

    private void SpawnSlidingImage()
    {
        if (slidingImagePool.Count == 0 || availableImages.Count == 0 || parentCanvas == null) return;

        RectTransform imageRect = GetSlidingImageFromPool();
        Image imageComponent = imageRect.GetComponent<Image>();

        // 화면 크기 계산 (캔버스의 RectTransform 사용)
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // 랜덤 스케일 적용
        float randomScale = Random.Range(minScale, maxScale);
        imageRect.localScale = new Vector3(randomScale, randomScale, 1f);

        // 시작 및 끝 위치 계산
        Vector2 startPos, endPos;

        // X 위치에 약간의 랜덤성 추가
        float randomXOffset = Random.Range(-canvasWidth * 0.3f, canvasWidth * 0.3f);
        // Y 위치에 약간의 랜덤성 추가
        float randomYOffset = Random.Range(-canvasHeight * 0.3f, canvasHeight * 0.3f);

        if (isVerticalSlide)
        {
            // 수직 이동 (위에서 아래 또는 아래서 위)
            if (startFromTop)
            {
                // 위에서 시작
                startPos = new Vector2(randomXOffset, canvasHeight / 2 + 100);
                endPos = new Vector2(randomXOffset, -canvasHeight / 2 - 100);
            }
            else
            {
                // 아래서 시작
                startPos = new Vector2(randomXOffset, -canvasHeight / 2 - 100);
                endPos = new Vector2(randomXOffset, canvasHeight / 2 + 100);
            }
        }
        else
        {
            // 수평 이동 (왼쪽에서 오른쪽 또는 오른쪽에서 왼쪽)
            if (startFromLeft)
            {
                // 왼쪽에서 시작
                startPos = new Vector2(-canvasWidth / 2 - 100, randomYOffset);
                endPos = new Vector2(canvasWidth / 2 + 100, randomYOffset);
            }
            else
            {
                // 오른쪽에서 시작
                startPos = new Vector2(canvasWidth / 2 + 100, randomYOffset);
                endPos = new Vector2(-canvasWidth / 2 - 100, randomYOffset);
            }
        }

        // 시작 위치 설정
        imageRect.anchoredPosition = startPos;

        // 이동 시퀀스 생성
        Sequence slideSequence = DOTween.Sequence();

        // 직선 이동 (sway 없이)
        slideSequence.Append(
            imageRect.DOAnchorPos(endPos, slideDuration).SetEase(Ease.Linear)
        );

        // 깜빡임 효과 추가 (랜덤하게) - 즉시 알파값 변경 방식으로 수정
        if (imageComponent != null && Random.value < blinkChance)
        {
            // 몇 번 깜빡일지 랜덤하게 결정
            int blinkCount = Random.Range(2, maxBlinkCount);

            for (int i = 0; i < blinkCount; i++)
            {
                // 랜덤한 시간에 깜빡임 발생
                float blinkTime = Random.Range(slideDuration * 0.1f, slideDuration * 0.9f);

                // 순간적으로 알파값 0으로 변경 (Ease.INTERNAL_Zero = 즉시 변경)
                slideSequence.InsertCallback(blinkTime, () => {
                    Color tempColor = imageComponent.color;
                    tempColor.a = 0f;
                    imageComponent.color = tempColor;
                });

                // 순간적으로 알파값 1로 복구 (즉시 복구)
                slideSequence.InsertCallback(blinkTime + blinkInterval, () => {
                    Color tempColor = imageComponent.color;
                    tempColor.a = 1f;
                    imageComponent.color = tempColor;
                });
            }
        }

        // 완료 후 오브젝트 풀에 반환
        slideSequence.OnComplete(() => ReturnImageToPool(imageRect));
    }

    // 인스펙터에서 효과 재시작 버튼용
    public void RestartEffects()
    {
        // 기존 효과 정리
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        titleSequence?.Kill();

        // 원래 상태로 복원
        if (titleImageComponent != null)
        {
            titleImageComponent.color = originalColor;
            titleImage.anchoredPosition = originalPosition;
        }

        // 활성화된 모든 슬라이딩 이미지 정리
        foreach (var img in slidingImagePool)
        {
            if (img != null && img.gameObject.activeSelf)
            {
                DOTween.Kill(img);
                img.gameObject.SetActive(false);
                availableImages.Enqueue(img);
            }
        }

        // 효과 다시 시작
        glitchCoroutine = StartCoroutine(GlitchEffectRoutine());
        InitializeGlitchEffect();
        spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
    }

    // 외부에서 효과 설정을 변경할 수 있는 메서드들
    public void SetGlitchIntensity(float colorIntensity, float positionIntensity)
    {
        colorGlitchIntensity = colorIntensity;
        positionGlitchIntensity = positionIntensity;
    }

    public void SetGlitchInterval(float interval, float duration)
    {
        glitchInterval = Mathf.Max(0.1f, interval);
        glitchDuration = Mathf.Min(duration, glitchInterval);

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = StartCoroutine(GlitchEffectRoutine());
        }
    }

    public void SetSlideDirection(bool vertical, bool fromTopOrLeft)
    {
        isVerticalSlide = vertical;
        if (vertical)
        {
            startFromTop = fromTopOrLeft;
        }
        else
        {
            startFromLeft = fromTopOrLeft;
        }
    }

    public void SetSpawnRate(float interval)
    {
        spawnInterval = interval;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnSlidingImagesRoutine());
        }
    }

    public void SetScaleRange(float min, float max)
    {
        minScale = Mathf.Max(0.1f, min);
        maxScale = Mathf.Max(minScale, max);
    }

    public void SetBlinkEffect(float chance, float interval)
    {
        blinkChance = Mathf.Clamp01(chance);
        blinkInterval = Mathf.Max(0.01f, interval);
    }
}
