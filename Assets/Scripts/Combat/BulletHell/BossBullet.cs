using UnityEngine;

public class BossBullet : MonoBehaviour, IPooledObject
{
    // Bullet Properties (Initialize에서만 설정)
    
    [Header("Visual Effects")]
    [SerializeField] private bool rotateWithMovement = true;
    [SerializeField] private float rotationSpeed = 0f;
    
    // 캐시된 컴포넌트들
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D col;
    
    // 런타임 변수들 (Initialize에서 설정됨)
    private Vector2 direction;
    private float damage;
    private float speed;
    private float lifetime;
    private float currentLifetime;
    private bool isInitialized = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        
        // Rigidbody2D 설정
        if (rb != null)
        {
            rb.gravityScale = 0f; // 중력 무시
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        // Collider를 Trigger로 설정
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    public void Initialize(Vector2 direction, float speed, float damage = 10f, float lifetime = 10f)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.currentLifetime = 0f;
        this.isInitialized = true;
        
        // 즉시 속도 적용
        if (rb != null)
        {
            rb.linearVelocity = this.direction * this.speed;
        }
        
        // 회전 설정
        if (rotateWithMovement && direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        // 수명 관리
        currentLifetime += Time.deltaTime;
        if (currentLifetime >= lifetime)
        {
            DeactivateBullet();
            return;
        }
        
        // 추가 회전 효과
        if (rotationSpeed != 0f)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 처리
        if (other.CompareTag("Player"))
        {
            var playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);
            }
            
            // 충돌 이펙트 생성 (선택사항)
            CreateHitEffect();
            
            // 발사체 비활성화
            DeactivateBullet();
        }
    }
    
    private void CreateHitEffect()
    {
        // 간단한 충돌 이펙트 (나중에 파티클로 확장 가능)
        // ObjectPool에서 이펙트 프리팹을 가져와서 사용할 수 있음
    }
    
    private void DeactivateBullet()
    {
        isInitialized = false;
        gameObject.SetActive(false);
    }
    
    // IPooledObject 인터페이스 구현
    public void OnObjectSpawn()
    {
        // ObjectPool에서 스폰될 때 호출
        currentLifetime = 0f;
        isInitialized = false;
        
        // 색상 초기화 (투명도 등)
        if (spriteRenderer != null)
        {
            var color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
    
    // 화면 밖으로 나갔는지 체크 (성능 최적화용)
    private void OnBecameInvisible()
    {
        // 화면 밖으로 나가면 비활성화
        if (isInitialized && currentLifetime > 1f) // 1초 후부터 체크
        {
            DeactivateBullet();
        }
    }
    
    // 디버그용 - Inspector에서 실시간으로 속성 변경 가능
    private void OnValidate()
    {
        if (Application.isPlaying && isInitialized && rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    // 외부에서 속성을 실시간으로 변경할 수 있는 메서드들
    public void SetSpeed(float newSpeed)
    {
        this.speed = newSpeed;
        if (rb != null && isInitialized)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    public void SetDirection(Vector2 newDirection)
    {
        this.direction = newDirection.normalized;
        if (rb != null && isInitialized)
        {
            rb.linearVelocity = direction * speed;
        }
        
        // 회전 업데이트
        if (rotateWithMovement && newDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    
    public void SetDamage(float newDamage)
    {
        this.damage = newDamage;
    }
    
    public void SetLifetime(float newLifetime)
    {
        this.lifetime = newLifetime;
    }
}