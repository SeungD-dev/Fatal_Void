using UnityEngine;

public class CutterProjectile : BaseProjectile
{
    private bool isReturning = false;
    [SerializeField] private float rotationSpeed = 720f;

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        isReturning = false;  // 스폰될 때마다 상태 리셋
    }

    protected override void Update()
    {
        if (startPosition == null || startPosition == Vector2.zero)
        {
            startPosition = transform.position;  // 시작 위치가 없으면 현재 위치로 설정
        }

        // 지속적인 회전
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        float distanceFromStart = Vector2.Distance(startPosition, transform.position);
        float distanceRatio = distanceFromStart / maxTravelDistance;
        if (!isReturning)
        {
            // 진행 방향으로 이동하면서 속도 감소
            float speedMultiplier = Mathf.Lerp(1f, 0.2f, distanceRatio);
            transform.Translate(direction * speed * speedMultiplier * Time.deltaTime, Space.World);

            // 최대 거리 도달 시 귀환 시작
            if (distanceRatio >= 1f)
            {
                isReturning = true;
            }
        }
        else
        {
            // 돌아오는 방향으로 이동하면서 속도 증가
            float returnRatio = 1f - (distanceFromStart / maxTravelDistance);
            float speedMultiplier = Mathf.Lerp(0.5f, 2f, returnRatio);
            transform.Translate(-direction * speed * speedMultiplier * Time.deltaTime, Space.World);

            // 시작 지점 근처에 도달하면 제거
            if (distanceFromStart <= 0.5f)
            {
                ReturnToPool();
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isReturning = false;  // 비활성화될 때도 상태 리셋
    }
}