using UnityEngine;
using System.Collections.Generic;

public class EnemyCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float cullingDistance = 30f;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float screenBuffer = 2f;

    [Header("Optimization")]
    [SerializeField] private bool useDistanceBasedInterval = true;
    [SerializeField] private float nearUpdateInterval = 0.1f;   // ����� �� ������Ʈ ����
    [SerializeField] private float farUpdateInterval = 0.3f;    // �� �� ������Ʈ ����
    [SerializeField] private float distanceThreshold = 15f;     // �����/�� �Ÿ� ����

    // ĳ�̵� ������
    private Camera mainCamera;
    private Transform playerTransform;
    private float nextUpdateTime;
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private List<Enemy> enemiesCache = new List<Enemy>();
    private Vector2 screenBounds;
    private float aspectRatio;

    // �Ÿ� ��� ������Ʈ�� ���� ������
    private HashSet<Enemy> nearEnemies = new HashSet<Enemy>();
    private HashSet<Enemy> farEnemies = new HashSet<Enemy>();
    private float nextNearUpdateTime;
    private float nextFarUpdateTime;
    private float sqrDistanceThreshold;

    private void Awake()
    {
        // �Ÿ� �Ӱ谪 ���� (�Ź� ������ ��� ȸ��)
        sqrDistanceThreshold = distanceThreshold * distanceThreshold;
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // GameManager���� �÷��̾� ���� ��������
        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
        {
            playerTransform = GameManager.Instance.PlayerTransform;
        }
        else
        {
            // �������� Find ��� (�ʱ�ȭ �� �� ����)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform not found!");
        }

        CalculateScreenBounds();

        // �ʱ� ������Ʈ �ð� ����
        nextUpdateTime = Time.time + updateInterval;
        nextNearUpdateTime = Time.time + nearUpdateInterval;
        nextFarUpdateTime = Time.time + farUpdateInterval;
    }
    private void CalculateScreenBounds()
    {
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize * 2;
        aspectRatio = (float)Screen.width / Screen.height;
        float cameraWidth = cameraHeight * aspectRatio;
        screenBounds = new Vector2(cameraWidth / 2, cameraHeight / 2);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float currentTime = Time.time;

        if (useDistanceBasedInterval)
        {
            // ����� ���� �� ���� ������Ʈ
            if (currentTime >= nextNearUpdateTime)
            {
                UpdateNearEnemyCulling();
                nextNearUpdateTime = currentTime + nearUpdateInterval;
            }

            // �� ���� �� ���� ������Ʈ
            if (currentTime >= nextFarUpdateTime)
            {
                UpdateFarEnemyCulling();
                nextFarUpdateTime = currentTime + farUpdateInterval;
            }
        }
        else
        {
            // ���� ��� - ��� ���� ������ �������� ������Ʈ
            if (currentTime >= nextUpdateTime)
            {
                UpdateAllEnemyCulling();
                nextUpdateTime = currentTime + updateInterval;
            }
        }
    }

    // ����� ���� ������Ʈ
    private void UpdateNearEnemyCulling()
    {
        enemiesCache.Clear();
        enemiesCache.AddRange(nearEnemies);

        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                nearEnemies.Remove(enemy);
                activeEnemies.Remove(enemy);
                continue;
            }

            // �Ÿ� Ȯ�� �� �ʿ�� ���� ��з�
            Vector2 enemyPos = enemy.transform.position;
            Vector2 playerPos = playerTransform.position;
            float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

            if (distanceSqr > sqrDistanceThreshold)
            {
                // �־������Ƿ� far�� �̵�
                nearEnemies.Remove(enemy);
                farEnemies.Add(enemy);
            }
            else
            {
                // ������ �����Ƿ� �ø� ���� ������Ʈ
                UpdateSingleEnemyCulling(enemy);
            }
        }
    }

    // �� ���� ������Ʈ
    private void UpdateFarEnemyCulling()
    {
        enemiesCache.Clear();
        enemiesCache.AddRange(farEnemies);

        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                farEnemies.Remove(enemy);
                activeEnemies.Remove(enemy);
                continue;
            }

            // �Ÿ� Ȯ�� �� �ʿ�� ���� ��з�
            Vector2 enemyPos = enemy.transform.position;
            Vector2 playerPos = playerTransform.position;
            float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

            if (distanceSqr <= sqrDistanceThreshold)
            {
                // ����������Ƿ� near�� �̵�
                farEnemies.Remove(enemy);
                nearEnemies.Add(enemy);
            }
            else
            {
                // ������ �ָ� �����Ƿ� �ø� ���� ������Ʈ
                UpdateSingleEnemyCulling(enemy);
            }
        }
    }

    // ��� ���� ������Ʈ (���� ���)
    private void UpdateAllEnemyCulling()
    {
        if (playerTransform == null) return;

        // ���� Ȱ��ȭ�� ������ ����� ĳ�ÿ� ����
        enemiesCache.Clear();
        enemiesCache.AddRange(activeEnemies);

        // ĳ�õ� ����� ����Ͽ� �ø� ������Ʈ
        foreach (var enemy in enemiesCache)
        {
            if (enemy == null)
            {
                activeEnemies.Remove(enemy);
                continue;
            }

            UpdateSingleEnemyCulling(enemy);
        }

        // �ı��� ���� ����
        activeEnemies.RemoveWhere(e => e == null);
    }

    // �� ���
    public void RegisterEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            // 보스는 컬링에서 제외
            if (enemy.GetComponent<CalamityBoss>() != null)
            {
                Debug.Log("Boss enemy excluded from culling system");
                return;
            }

            activeEnemies.Add(enemy);

            // �ø� �Ŵ��� ���� ����
            enemy.SetCullingManager(this);

            // �÷��̾� ���� ����
            if (playerTransform != null)
            {
                enemy.Initialize(playerTransform);
            }

            // �Ÿ� ��� �з�
            if (useDistanceBasedInterval && playerTransform != null)
            {
                float distanceSqr = Vector2.SqrMagnitude(
                    (Vector2)enemy.transform.position - (Vector2)playerTransform.position);

                if (distanceSqr <= sqrDistanceThreshold)
                {
                    nearEnemies.Add(enemy);
                }
                else
                {
                    farEnemies.Add(enemy);
                }
            }

            // �ʱ� �ø� ���� ����
            UpdateSingleEnemyCulling(enemy);
        }
    }

    // �� ��� ����
    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        nearEnemies.Remove(enemy);
        farEnemies.Remove(enemy);
    }

    // ���� �� �ø� ���� ������Ʈ
    private void UpdateSingleEnemyCulling(Enemy enemy)
    {
        if (enemy == null || playerTransform == null) return;

        Vector2 enemyPos = enemy.transform.position;
        Vector2 playerPos = playerTransform.position;
        float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

        // �Ÿ� ��� �ø�
        if (distanceSqr > cullingDistance * cullingDistance)
        {
            enemy.SetCullingState(false);
            return;
        }

        // ȭ�� ��� �ø� (���ü� Ȯ��)
        Vector2 viewportPoint = mainCamera.WorldToViewportPoint(enemyPos);
        bool isVisible = IsInScreenBounds(viewportPoint);
        enemy.SetCullingState(isVisible);
    }

    private bool IsInScreenBounds(Vector2 viewportPoint)
    {
        float buffer = screenBuffer;
        return viewportPoint.x >= -buffer && viewportPoint.x <= (1 + buffer) &&
               viewportPoint.y >= -buffer && viewportPoint.y <= (1 + buffer);
    }

    // ȭ�� ũ�� ���� �� ��谪 ����
    private void OnRectTransformDimensionsChange()
    {
        CalculateScreenBounds();
    }

    // ����ȭ ���� �ǽð� ���� �޼���
    public void SetDistanceBasedInterval(bool enable)
    {
        useDistanceBasedInterval = enable;

        if (!enable)
        {
            // ��Ȱ��ȭ �� ��� ���� �� �� ������Ʈ
            UpdateAllEnemyCulling();
        }
        else
        {
            // Ȱ��ȭ �� ������ �Ÿ��� ���� �з�
            ReclassifyEnemiesByDistance();
        }
    }

    // �Ÿ��� ���� �� ��з�
    private void ReclassifyEnemiesByDistance()
    {
        if (playerTransform == null) return;

        nearEnemies.Clear();
        farEnemies.Clear();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            float distanceSqr = Vector2.SqrMagnitude(
                (Vector2)enemy.transform.position - (Vector2)playerTransform.position);

            if (distanceSqr <= sqrDistanceThreshold)
            {
                nearEnemies.Add(enemy);
            }
            else
            {
                farEnemies.Add(enemy);
            }
        }
    }
    public void SetPlayerReference(Transform player)
    {
        if (player != null)
        {
            playerTransform = player;

            // �̹� ��ϵ� ��� ������ �÷��̾� ���� ����
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.Initialize(playerTransform);
                }
            }
        }
    }


#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (!Application.isPlaying || playerTransform == null) return;

    // �ø� ���� �ð�ȭ
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(playerTransform.position, cullingDistance);

    // ȭ�� ���� �ð�ȭ
    Gizmos.color = Color.yellow;
    Vector3 center = playerTransform.position;
    Vector3 size = new Vector3(screenBounds.x * 2, screenBounds.y * 2, 0);
    Gizmos.DrawWireCube(center, size);

    // ���� ���� �ð�ȭ
    Gizmos.color = Color.green;
    float bufferSize = screenBuffer * 2;
    Vector3 bufferSizeVec = new Vector3(
        size.x * (1 + bufferSize),
        size.y * (1 + bufferSize),
        0
    );
    Gizmos.DrawWireCube(center, bufferSizeVec);

    // �Ÿ� ��� ������Ʈ �Ӱ谪 �ð�ȭ
    if (useDistanceBasedInterval)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTransform.position, distanceThreshold);
    }
}
}
#endif