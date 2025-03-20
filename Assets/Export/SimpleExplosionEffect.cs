using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SimpleExplosionEffect : MonoBehaviour
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
        new Color(1f, 0f, 0f),
        new Color(0f, 0f, 0f),
        new Color(65/255f, 65/255f, 65/255f)
    };

    private Transform particleContainer;
    private ObjectPool squarePool;

    private void Awake()
    {
        // 파티클을 담을 빈 컨테이너 생성
        particleContainer = new GameObject("ParticleContainer").transform;
        particleContainer.SetParent(transform);
        particleContainer.localPosition = Vector3.zero;

        // 오브젝트 풀 생성
        squarePool = new ObjectPool(CreateSquareParticle, particleCount * 2);
    }

    // 몬스터가 죽을 때 호출
    public void PlayExplosion()
    {
        StartCoroutine(CreateExplosion(transform.position));
    }

    private IEnumerator CreateExplosion(Vector3 position)
    {
        for (int i = 0; i < particleCount; i++)
        {
            GameObject square = squarePool.GetObject();
            if (square != null)
            {
                // 파티클 초기화
                square.transform.position = position;
                square.transform.rotation = Quaternion.identity;
                square.transform.localScale = Vector3.one;
                square.SetActive(true);

                // 랜덤 설정
                float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
                Color color = particleColors[Random.Range(0, particleColors.Length)];
                float angle = Random.Range(0f, 360f);
                float distance = explosionRadius * Random.Range(0.5f, 1f);

                // 사각형 렌더러 설정
                SpriteRenderer renderer = square.GetComponent<SpriteRenderer>();
                renderer.color = color;

                // 목표 위치 계산
                Vector3 targetPos = position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                    0f
                );

                // DOTween 애니메이션
                Sequence seq = DOTween.Sequence();

                // 크기 조정
                seq.Append(square.transform.DOScale(new Vector3(size, size, 1f), explosionDuration * 0.2f));

                // 이동
                seq.Join(square.transform.DOMove(targetPos, explosionDuration)
                    .SetEase(Ease.OutQuad));

                // 회전
                seq.Join(square.transform.DORotate(
                    new Vector3(0f, 0f, Random.Range(-180f, 180f)),
                    explosionDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.OutQuad));

                // 페이드 아웃
                seq.Join(renderer.DOFade(0f, explosionDuration)
                    .SetEase(Ease.InQuad));

                // 완료 후 오브젝트 풀로 반환
                seq.OnComplete(() => {
                    square.SetActive(false);
                    squarePool.ReturnObject(square);
                });
            }

            // 약간의 시간차를 두고 파티클 생성
            yield return new WaitForSeconds(0.02f);
        }
    }

    // 사각형 파티클 생성
    private GameObject CreateSquareParticle()
    {
        GameObject square = new GameObject("SquareParticle");
        square.transform.SetParent(particleContainer);

        // 스프라이트 렌더러 추가
        SpriteRenderer renderer = square.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.sortingOrder = 10; // 몬스터보다 앞에 표시되도록

        square.SetActive(false);
        return square;
    }

    // 사각형 스프라이트 생성
    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color fillColor = Color.white;

        // 텍스처 채우기
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, fillColor);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    // 간단한 오브젝트 풀 구현
    private class ObjectPool
    {
        private GameObject[] pool;
        private bool[] isUsed;
        private System.Func<GameObject> createFunc;

        public ObjectPool(System.Func<GameObject> createFunc, int size)
        {
            this.createFunc = createFunc;
            pool = new GameObject[size];
            isUsed = new bool[size];

            // 풀 미리 채우기
            for (int i = 0; i < size; i++)
            {
                pool[i] = createFunc();
                isUsed[i] = false;
            }
        }

        public GameObject GetObject()
        {
            // 사용 가능한 오브젝트 찾기
            for (int i = 0; i < pool.Length; i++)
            {
                if (!isUsed[i])
                {
                    isUsed[i] = true;
                    return pool[i];
                }
            }

            // 풀이 가득 찬 경우 새로 생성 (선택적)
            // GameObject newObj = createFunc();
            // System.Array.Resize(ref pool, pool.Length + 1);
            // System.Array.Resize(ref isUsed, isUsed.Length + 1);
            // pool[pool.Length - 1] = newObj;
            // isUsed[isUsed.Length - 1] = true;
            // return newObj;

            // 또는 null 반환
            return null;
        }

        public void ReturnObject(GameObject obj)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == obj)
                {
                    isUsed[i] = false;
                    break;
                }
            }
        }
    }

    // 테스트용 메서드
    [ContextMenu("테스트 폭발")]
    public void TestExplosion()
    {
        PlayExplosion();
    }

    // (0, 0) 좌표에 폭발 이펙트 생성
    public void PlayExplosionAtOrigin()
    {
        StartCoroutine(CreateExplosion(Vector3.zero));
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}