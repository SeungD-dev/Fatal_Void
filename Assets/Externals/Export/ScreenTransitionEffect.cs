using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class ScreenTransitionEffect : MonoBehaviour
{
    [Header("트랜지션 설정")]
    public float transitionDuration = 0.5f;
    public Ease scaleEase = Ease.InOutQuad;
    public bool useBlackScreen = true;

    [Header("선택적 설정")]
    public bool autoRevert = false; // 이펙트 나오고서 바로 reverDelay후에 다시 효과 반전되서 그대로 출력되게 할것인지
    public float revertDelay = 0.2f;
    public bool reverseEffect = false; // 효과 반전 여부 결정

    private RectTransform rectTransform;
    private Image image;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalScale = rectTransform.localScale;

        
        gameObject.SetActive(false);
    }

    // 테스트 코드 제거 - Update 메서드 전체 삭제

    public void PlayTransition(Action onTransitionComplete = null)
    {
        Debug.Log("PlayTransition called");

        // 기존 Tween 종료
        DOTween.Kill(rectTransform);

        // 초기 상태로 설정
        if (reverseEffect)
        {
            // 반전 효과: X 스케일이 0에서 시작
            rectTransform.localScale = new Vector3(0, originalScale.y, originalScale.z);
        }
        else
        {
            // 원래 효과: 기본 크기에서 시작
            rectTransform.localScale = originalScale;
        }

        gameObject.SetActive(true);

        // 검은색 이미지를 사용하는 경우
        if (useBlackScreen && image != null)
        {
            image.color = Color.black;
        }

        if (reverseEffect)
        {
            // 반전 효과: X 스케일이 0에서 원래 크기로 커짐
            rectTransform.DOScaleX(originalScale.x, transitionDuration)
                .SetEase(scaleEase)
                .SetUpdate(true) // 타임스케일 영향 받지 않게
                .OnComplete(() => {
                    // 트랜지션 완료 후 콜백 호출
                    onTransitionComplete?.Invoke();

                    // 여기에 비활성화 코드 추가
                    gameObject.SetActive(false);

                    // 자동 복구가 활성화된 경우
                    if (autoRevert)
                    {
                        DOVirtual.DelayedCall(revertDelay, () => {
                            RevertTransition();
                        }).SetUpdate(true);
                    }
                });
        }
        else
        {
            // 원래 효과: X 스케일이 원래 크기에서 0으로 줄어듦
            rectTransform.DOScaleX(0, transitionDuration)
                .SetEase(scaleEase)
                .SetUpdate(true) // 타임스케일 영향 받지 않게
                .OnComplete(() => {
                    // 트랜지션 완료 후 콜백 호출
                    onTransitionComplete?.Invoke();

                    // 여기에 비활성화 코드 추가
                    gameObject.SetActive(false);

                    // 자동 복구가 활성화된 경우
                    if (autoRevert)
                    {
                        DOVirtual.DelayedCall(revertDelay, () => {
                            RevertTransition();
                        }).SetUpdate(true);
                    }
                });
        }
    }
    public void RevertTransition(Action onRevertComplete = null)
    {
        if (reverseEffect)
        {
            // 반전 효과의 복구: X 스케일이 원래 크기에서 0으로 줄어듦
            rectTransform.DOScaleX(0, transitionDuration)
                .SetEase(scaleEase)
                .OnComplete(() => {
                    onRevertComplete?.Invoke();
                    gameObject.SetActive(false); // 트랜지션 완료 후 비활성화
                });
        }
        else
        {
            // 원래 효과의 복구: X 스케일이 0에서 원래 크기로 커짐
            rectTransform.DOScaleX(originalScale.x, transitionDuration)
                .SetEase(scaleEase)
                .OnComplete(() => {
                    onRevertComplete?.Invoke();
                    gameObject.SetActive(false); // 트랜지션 완료 후 비활성화
                });
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(rectTransform);
    }
}