using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Infinity Disc (Sawblade X-Tier) 전용 투사체
/// 무한히 튕기며 지속적으로 데미지를 주는 투사체
/// </summary>
public class InfinityDiscProjectile : BaseProjectile
{
    [SerializeField] private float rotationSpeed = 720f;
    private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>(16);
    private Camera mainCamera;
    private float angleZ;
    private InfinityDiscMechanism parentMechanism;

    // 카메라 경계 캐싱
    private float cameraHeight;
    private float cameraWidth;
    private Vector2 cameraPosition;
    private float leftBound;
    private float rightBound;
    private float bottomBound;
    private float topBound;
    private Vector2 currentPosition;
    private Vector2 newDirection;
    private Vector2 knockbackForce;

    // 적 히트 리스트 갱신용
    private float lastHitClearTime;
    private const float HIT_CLEAR_INTERVAL = 0.5f; // 0.5초마다 히트 리스트 갱신

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        UpdateCameraBounds();
    }

    public override void Initialize(float damage, Vector2 direction, float speed,
        float knockbackPower = 0f, float range = 10f, float projectileSize = 1f,
        bool canPenetrate = false, int maxPenetrations = 0, float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize,
            canPenetrate, maxPenetrations, damageDecay);
        
        // 무한 관통 설정
        this.canPenetrate = true;
        this.remainingPenetrations = -1; // 무한 관통
    }

    /// <summary>
    /// 부모 메커니즘 참조 설정
    /// </summary>
    public void SetParentMechanism(InfinityDiscMechanism mechanism)
    {
        parentMechanism = mechanism;
    }

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        hitEnemies.Clear();
        angleZ = 0f;
        lastHitClearTime = Time.time;
        UpdateCameraBounds();
    }

    private void UpdateCameraBounds()
    {
        if (mainCamera == null) return;

        cameraHeight = 2f * mainCamera.orthographicSize;
        cameraWidth = cameraHeight * mainCamera.aspect;
        cameraPosition = mainCamera.transform.position;

        leftBound = cameraPosition.x - cameraWidth * 0.5f;
        rightBound = cameraPosition.x + cameraWidth * 0.5f;
        bottomBound = cameraPosition.y - cameraHeight * 0.5f;
        topBound = cameraPosition.y + cameraHeight * 0.5f;
    }

    protected override void Update()
    {
        // 회전 효과
        angleZ = (angleZ + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        // 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 주기적으로 경계 체크 및 히트 리스트 갱신
        if (Time.frameCount % 5 == 0) // 5프레임마다 경계 체크
        {
            UpdateCameraBounds();
            CheckCameraBounds();
        }

        // 주기적으로 히트된 적 리스트 갱신 (같은 적을 다시 타격 가능하게)
        if (Time.time >= lastHitClearTime + HIT_CLEAR_INTERVAL)
        {
            hitEnemies.Clear();
            lastHitClearTime = Time.time;
        }
    }

    private void CheckCameraBounds()
    {
        currentPosition = transform.position;
        bool bounced = false;
        newDirection = direction;

        // 좌우 경계 체크
        if (currentPosition.x <= leftBound || currentPosition.x >= rightBound)
        {
            newDirection.x = -direction.x;
            bounced = true;
            currentPosition.x = Mathf.Clamp(currentPosition.x, leftBound, rightBound);
        }

        // 상하 경계 체크
        if (currentPosition.y <= bottomBound || currentPosition.y >= topBound)
        {
            newDirection.y = -direction.y;
            bounced = true;
            currentPosition.y = Mathf.Clamp(currentPosition.y, bottomBound, topBound);
        }

        if (bounced)
        {
            // 무한 튕기기: 경계에 닿을 때마다 방향만 바뀌고 사라지지 않음
            transform.position = currentPosition;
            direction = newDirection;
            
            // 튕길 때마다 히트 리스트 갱신 (같은 적을 다시 타격 가능)
            hitEnemies.Clear();
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        // 최근에 히트한 적이 아닌 경우에만 데미지 적용
        if (!hitEnemies.Add(enemy)) return;

        // 데미지 적용
        enemy.TakeDamage(damage);

        // 넉백 적용
        if (knockbackPower > 0)
        {
            knockbackForce.x = direction.x * knockbackPower;
            knockbackForce.y = direction.y * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        // 무한 관통이므로 HandlePenetration 호출하지 않음 (투사체가 사라지지 않음)
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out Enemy enemy))
        {
            ApplyDamageAndEffects(enemy);
        }
    }

    /// <summary>
    /// 강제로 투사체를 풀에 반환 (무기 제거, 플레이어 사망 등)
    /// </summary>
    public void ForceReturn()
    {
        if (parentMechanism != null)
        {
            parentMechanism.OnProjectileReturned();
        }
        ReturnToPool();
    }

    protected override void ReturnToPool()
    {
        if (parentMechanism != null)
        {
            parentMechanism.OnProjectileReturned();
        }
        base.ReturnToPool();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hitEnemies.Clear();
        angleZ = 0f;
        parentMechanism = null;
    }

    // Infinity Disc는 기본적으로 사라지지 않으므로 거리 체크를 오버라이드하여 무시
    protected override void HandlePenetration()
    {
        // 무한 관통이므로 아무것도 하지 않음 (투사체가 사라지지 않음)
    }
}