using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField] private GameObject floatingTextPrefab;
    private Queue<GameObject> textPool = new Queue<GameObject>();
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatDistance = 1f;
    [SerializeField] private Vector2 offset = new Vector2(0, 0.5f); // 머리 위 오프셋
    [SerializeField] private float minXOffset = -0.25f;
    [SerializeField] private float maxXOffset = 0.25f;
    public bool isFloatingTextEnabled = true;

    protected override void Awake()
    {
        base.Awake();
        isFloatingTextEnabled = true;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, transform);
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
        }
    }

    public void ShowFloatingText(string text, Vector3 worldPosition, Color color)
    {
        if (textPool.Count == 0) return;

        GameObject textObj = textPool.Dequeue();
        textObj.SetActive(true);

        TextMeshProUGUI textComponent = textObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
        }

        // 랜덤 X 오프셋 적용
        float randomXOffset = Random.Range(minXOffset, maxXOffset);
        Vector3 randomOffset = new Vector3(randomXOffset, offset.y, 0);

        // 월드 위치에 오프셋 적용
        Vector3 offsetPosition = worldPosition + randomOffset;

        // 텍스트 오브젝트를 월드 스페이스에 배치
        textObj.transform.position = offsetPosition;

        StartCoroutine(FloatingTextAnimation(textObj, offsetPosition));
    }


    private IEnumerator FloatingTextAnimation(GameObject textObj, Vector3 startWorldPos)
    {
        float elapsedTime = 0f;
        float duration = 1f;

        TextMeshProUGUI textComponent = textObj.GetComponentInChildren<TextMeshProUGUI>();

        Vector3 endPos = startWorldPos + new Vector3(0, floatDistance, 0);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            textObj.transform.position = Vector3.Lerp(startWorldPos, endPos, t);

            if (textComponent != null)
            {
                textComponent.alpha = 1 - t;
            }

            yield return null;
        }

        textObj.SetActive(false);
        textPool.Enqueue(textObj);
    }
}
