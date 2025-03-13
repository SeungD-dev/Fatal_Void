using System.Collections;
using UnityEngine;

public class SpawnWarningController : MonoBehaviour
{
    [Header("Warning Prefab")]
    [SerializeField] private GameObject warningPrefab;

    [Header("Warning Settings")]
    [SerializeField] private float defaultWarningDuration = 1.2f;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private AnimationCurve warningScaleCurve;
    [SerializeField] private AnimationCurve warningAlphaCurve;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;

    private ObjectPool objectPool;
    private const string WARNING_POOL_TAG = "SpawnWarning";

    private void Start()
    {
        objectPool = ObjectPool.Instance;

        // 경고 프리팹 풀 초기화
        if (warningPrefab != null && objectPool != null)
        {
            if (!objectPool.DoesPoolExist(WARNING_POOL_TAG))
            {
                objectPool.CreatePool(WARNING_POOL_TAG, warningPrefab, initialPoolSize);
            }
        }
        else
        {
            Debug.LogError("Warning prefab or ObjectPool not found!");
            enabled = false;
        }
    }

    public IEnumerator ShowWarningAtPosition(Vector2 position, float duration = -1f)
    {
        if (duration <= 0f)
            duration = defaultWarningDuration;

        // 경고 프리팹 스폰
        GameObject warningObj = objectPool.SpawnFromPool(WARNING_POOL_TAG, position, Quaternion.identity);

        if (warningObj != null)
        {
            SpriteRenderer warningRenderer = warningObj.GetComponent<SpriteRenderer>();
            if (warningRenderer != null)
            {
                // 경고 색상 설정
                warningRenderer.color = warningColor;

                // 경고 애니메이션 재생
                yield return StartCoroutine(AnimateWarning(warningObj, warningRenderer, duration));
            }

            // 애니메이션 완료 후 오브젝트 회수
            objectPool.ReturnToPool(WARNING_POOL_TAG, warningObj);
        }
    }

    private IEnumerator AnimateWarning(GameObject warningObj, SpriteRenderer renderer, float duration)
    {
        float timer = 0f;
        float initialScale = 1f;

        Transform warningTransform = warningObj.transform;
        Color originalColor = renderer.color;

        while (timer < duration)
        {
            float t = timer / duration;

            // 스케일 애니메이션
            float scale = initialScale * warningScaleCurve.Evaluate(t);
            warningTransform.localScale = new Vector3(scale, scale, 1f);

            // 알파 애니메이션
            float alpha = warningAlphaCurve.Evaluate(t);
            renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }
    }
}