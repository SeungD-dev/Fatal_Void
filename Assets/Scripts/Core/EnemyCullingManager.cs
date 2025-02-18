using UnityEngine;
using System.Collections.Generic;

public class EnemyCullingManager : MonoBehaviour
{
    [Header("Culling Settings")]
    [SerializeField] private float cullingDistance = 30f;  // 컬링 거리
    [SerializeField] private float updateInterval = 0.5f;  // 컬링 체크 주기

    private Camera mainCamera;
    private Transform playerTransform;
    private float nextUpdateTime;
    private HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private readonly Vector3[] boundsCorners = new Vector3[4];

    private void Start()
    {
        mainCamera = Camera.main;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        CalculateScreenBounds();
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateEnemyCulling();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
    }

    private void UpdateEnemyCulling()
    {
        Vector2 playerPos = playerTransform.position;
        float cullingDistanceSqr = cullingDistance * cullingDistance;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            Vector2 enemyPos = enemy.transform.position;
            float distanceSqr = Vector2.SqrMagnitude(enemyPos - playerPos);

            // 컬링 거리 밖이면 비활성화
            if (distanceSqr > cullingDistanceSqr)
            {
                SetEnemyActive(enemy, false);
                continue;
            }

            // 화면 안에 있는지 확인
            bool isVisible = IsPointInCameraView(enemyPos);
            SetEnemyActive(enemy, isVisible);
        }
    }

    private void SetEnemyActive(Enemy enemy, bool active)
    {
        if (enemy.gameObject.activeSelf != active)
        {
            enemy.gameObject.SetActive(active);

            // AI 및 물리 컴포넌트 최적화
            var enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.enabled = active;
            }

            var rigidbody = enemy.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.simulated = active;
            }
        }
    }

    private void CalculateScreenBounds()
    {
        float z = -mainCamera.transform.position.z;
        boundsCorners[0] = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, z));
        boundsCorners[1] = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, z));
        boundsCorners[2] = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, z));
        boundsCorners[3] = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, z));
    }

    private bool IsPointInCameraView(Vector2 point)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(point);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerTransform.position, cullingDistance);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(boundsCorners[i], boundsCorners[(i + 1) % 4]);
        }
    }
#endif
}