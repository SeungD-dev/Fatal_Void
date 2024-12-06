using UnityEngine;


public class AxeProjectile : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private Vector2 returnDirection;
    private float speed;
    private bool isReturning = false;
    private Vector2 startPosition;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float returnSpeedMultiplier = 1f;

    public void Initialize(float damage, Vector2 direction, float speed)
    {
        this.damage = damage;
        this.direction = direction.normalized;
        this.speed = speed;
        startPosition = transform.position;
    }

    private void Update()
    {
        // 도끼 회전
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        if (!isReturning)
        {
            // 전진 방향으로 이동
            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            // 최대 거리 도달 체크
            if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            {
                isReturning = true;
                // 돌아가는 방향을 현재 진행 방향의 정반대로 설정
                returnDirection = -direction;
            }
        }
        else
        {
            // 정해진 returnDirection으로 계속 이동
            transform.position += (Vector3)(returnDirection * speed * returnSpeedMultiplier * Time.deltaTime);
        }
    }

    // OnBecameInvisible은 오브젝트가 화면 밖으로 나갈 때 호출됨
    private void OnBecameInvisible()
    {
        // 돌아가는 중에만 파괴
        if (isReturning)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}