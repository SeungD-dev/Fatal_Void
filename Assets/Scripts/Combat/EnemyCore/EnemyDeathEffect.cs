using UnityEngine;
using System.Collections;
using DG.Tweening;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("폭발 설정")]
    [SerializeField] private int particleCount = 5;
    [SerializeField] private float explosionDuration = 0.5f;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);

    [Header("파티클 설정")]
    [SerializeField]
    private Color[] particleColors = new Color[]
    {
        new Color(1f, 0f, 0f),      // 빨강
        new Color(0f, 0f, 0f),      // 검정
        new Color(65/255f, 65/255f, 65/255f)  // 회색
    };

    [Header("오브젝트 풀 설정")]
    [SerializeField] private string effectPoolTag = "DeathParticle"; // 풀 태그명은 GameManager와 일치해야 함

    // 파티클 재사용 설정
    private static readonly int maxConcurrentEffects = 3; // 동시에 발생 가능한 최대 효과 수
    private static int activeEffectCount = 0;

    // 캐싱을 위한 변수
    private static readonly WaitForSeconds particleDelay = new WaitForSeconds(0.02f);

    // 생성자에서 정적 필드 초기화 방지 (성능 최적화)
    static EnemyDeathEffect() { }

    // 몬스터가 죽을 때 호출될 메서드
    public void PlayDeathEffect(Vector3 position)
    {
        // 최대 동시 효과 수 제한 확인
        if (activeEffectCount >= maxConcurrentEffects)
            return;

        // 풀 존재 확인 - GameManager에서 이미 초기화했으므로 확인만 함
        if (ObjectPool.Instance == null || !ObjectPool.Instance.DoesPoolExist(effectPoolTag))
        {
            Debug.LogWarning($"DeathParticle pool not found. Skipping effect.");
            return;
        }

        // 효과 실행
        StartCoroutine(CreateDeathEffect(position));
    }

    private IEnumerator CreateDeathEffect(Vector3 position)
    {
        activeEffectCount++;

        for (int i = 0; i < particleCount; i++)
        {
            // 오브젝트 풀에서 파티클 가져오기
            GameObject particle = ObjectPool.Instance.SpawnFromPool(effectPoolTag, position, Quaternion.identity);
            if (particle != null)
            {
                ConfigureAndAnimateParticle(particle, position);
            }

            // 시간차를 두고 파티클 생성
            yield return particleDelay;
        }

        // 모든 파티클이 애니메이션을 완료하기 위한 충분한 시간 대기
        yield return new WaitForSeconds(explosionDuration);

        activeEffectCount--;
    }

    private void ConfigureAndAnimateParticle(GameObject particle, Vector3 position)
    {
        // 렌더러 가져오기
        SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("Particle is missing SpriteRenderer component");
            return;
        }

        // 랜덤 설정
        float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
        Color color = particleColors[Random.Range(0, particleColors.Length)];
        float angle = Random.Range(0f, 360f);
        float distance = explosionRadius * Random.Range(0.5f, 1f);

        // 렌더러 설정
        renderer.color = color;

        // 목표 위치 계산
        Vector3 targetPos = position + new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
            0f
        );

        // 기존 DOTween 애니메이션 제거
        DOTween.Kill(particle.transform);
        DOTween.Kill(renderer);

        // DOTween 애니메이션
        Sequence seq = DOTween.Sequence();

        // 초기 설정
        particle.transform.localScale = Vector3.zero;
        renderer.color = new Color(color.r, color.g, color.b, 1f);

        // 크기 조정
        seq.Append(particle.transform.DOScale(new Vector3(size, size, 1f), explosionDuration * 0.2f));

        // 이동
        seq.Join(particle.transform.DOMove(targetPos, explosionDuration)
            .SetEase(Ease.OutQuad));

        // 회전
        seq.Join(particle.transform.DORotate(
            new Vector3(0f, 0f, Random.Range(-180f, 180f)),
            explosionDuration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad));

        // 페이드 아웃
        seq.Join(renderer.DOFade(0f, explosionDuration)
            .SetEase(Ease.InQuad));

        // 완료 후 오브젝트 풀로 반환
        seq.OnComplete(() => {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(effectPoolTag, particle);
            }
        });

        // 애니메이션이 중간에 중단될 경우를 대비한 안전장치
        seq.SetUpdate(true); // TimeScale에 영향받지 않도록 설정
    }

    private void OnDestroy()
    {
        // 진행 중인 모든 DOTween 애니메이션 종료
        DOTween.Kill(transform);
    }
}