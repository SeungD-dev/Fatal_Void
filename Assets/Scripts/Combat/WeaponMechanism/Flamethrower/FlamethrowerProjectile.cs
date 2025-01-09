using UnityEngine;

public class FlamethrowerProjectile : MonoBehaviour
{
    private float damage;
    private float damageInterval = 0.25f;
    private float lastDamageTime;
    private Vector2 direction;
    private float range;
    private bool isActive;

    [SerializeField] private ParticleSystem flameParticles;
    [SerializeField] private ParticleSystem smokeParticles;

    private void Awake()
    {
        if (flameParticles == null)
        {
            flameParticles = GetComponent<ParticleSystem>();
        }

        if (smokeParticles == null)
        {
            Debug.LogError("Smoke ParticleSystem reference is missing!");
        }
    }

    public void Initialize(float damage, Vector2 direction, float range, float knockback)
    {
        this.damage = damage;
        this.direction = direction;
        this.range = range;

        // 파티클 시스템 길이 설정
        var flameShape = flameParticles.shape;
        flameShape.length = range;
        var smokeShape = smokeParticles.shape;
        smokeShape.length = range * 1.2f;

        // 방향에 따른 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 파티클 시스템 시작
        flameParticles.Play();
        smokeParticles.Play();

        isActive = true;
        lastDamageTime = Time.time - damageInterval; // 바로 첫 데미지 적용되도록
    }

    private void Update()
    {
        if (!isActive) return;

        if (Time.time >= lastDamageTime + damageInterval)
        {
            ApplyDamage();
            lastDamageTime = Time.time;
        }
    }

    private void ApplyDamage()
    {
        Vector2 boxCenter = (Vector2)transform.position + direction * (range * 0.5f);
        Vector2 boxSize = new Vector2(range, range * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, angle, LayerMask.GetMask("Enemy"));

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        // 디버깅용 박스 표시
        Debug.DrawRay(boxCenter, Quaternion.Euler(0, 0, angle) * Vector2.right * (boxSize.x * 0.5f), Color.red, damageInterval);
        Debug.DrawRay(boxCenter, Quaternion.Euler(0, 0, angle) * Vector2.left * (boxSize.x * 0.5f), Color.red, damageInterval);
        Debug.DrawRay(boxCenter, Quaternion.Euler(0, 0, angle) * Vector2.up * (boxSize.y * 0.5f), Color.red, damageInterval);
        Debug.DrawRay(boxCenter, Quaternion.Euler(0, 0, angle) * Vector2.down * (boxSize.y * 0.5f), Color.red, damageInterval);
    }

    public void DeactivateProjectile()
    {
        isActive = false;
        flameParticles.Stop();
        smokeParticles.Stop();
    }

    private void OnDisable()
    {
        DeactivateProjectile();
    }
}