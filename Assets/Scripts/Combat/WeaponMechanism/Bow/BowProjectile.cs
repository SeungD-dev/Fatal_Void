using UnityEngine;

public class BowProjectile : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private float speed;
    [SerializeField] private float maxDistance = 20f;
    private Vector2 startPosition;
    [SerializeField] private float rotationOffset;

    public void Initialize(float damage, Vector2 direction, float speed)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        startPosition = transform.position;

        // 화살의 방향 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    private void Update()
    {
        // 투사체 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 최대 거리 체크
        if (Vector2.Distance(startPosition, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 적 체력 감소
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            // 적과 충돌하면 화살 파괴
            Destroy(gameObject);
        }
    }
}
