using UnityEngine;

public class SwordProjectile : MonoBehaviour
{
    private int damage;
    private Vector2 direction;
    private float speed;
    [SerializeField] private float maxDistance = 20f; // 최대 비행 거리
    private Vector2 startPosition;

    [SerializeField] private float rotationOffset = -60f;

    public void Initialize(int damage, Vector2 direction, float speed)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        startPosition = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0,0,angle + rotationOffset);
    }

    private void Update()
    {
        // 투사체 이동
        //transform.Translate(Vector2.right * speed * Time.deltaTime);
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
            // 투사체는 관통하므로 파괴하지 않음
        }
    }
}

