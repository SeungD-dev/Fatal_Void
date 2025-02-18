using UnityEngine;

public class GrinderProjectile : BaseProjectile
{
    [SerializeField] private float rotationSpeed = 720f;
    public GameObject groundEffectPrefab;

    private float spawnTime;
    private Vector2 targetPosition;
    private float attackRadius;
    private float groundEffectDuration;
    private float damageTickInterval;
    private string groundEffectPoolTag;
    private float airTime;
    private const float MAX_HEIGHT = 3f;
    private float angleZ;
    private float currentHeight;
    private Vector2 currentPos;


    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        spawnTime = Time.time;

        // 크기 업데이트
        transform.localScale = Vector3.one * baseProjectileSize;
    }

    public void Initialize(
      float damage,
      Vector2 direction,
      float speed,
      Vector2 targetPos,
      float radius,
      float duration,
      float tickInterval,
      string effectPoolTag,
      float size)
    {
        base.Initialize(damage, direction, speed);

        this.targetPosition = targetPos;
        this.attackRadius = radius;
        this.groundEffectDuration = duration;
        this.damageTickInterval = tickInterval;
        this.groundEffectPoolTag = effectPoolTag;
        this.airTime = Vector2.Distance(transform.position, targetPosition) / speed;

        // 크기 설정
        transform.localScale = Vector3.one * size;
        angleZ = 0f;
    }


    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 투사체 자체는 대미지를 주지 않도록 override
    }

    protected override void Update()
    {
        angleZ = (angleZ + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        float progress = (Time.time - spawnTime) / airTime;
        if (progress >= 1f)
        {
            CreateGroundEffect();
            ReturnToPool();
            return;
        }

        float progressPI = progress * Mathf.PI;
        currentHeight = Mathf.Sin(progressPI) * MAX_HEIGHT;

        currentPos.x = Mathf.Lerp(startPosition.x, targetPosition.x, progress);
        currentPos.y = Mathf.Lerp(startPosition.y, targetPosition.y, progress);

        transform.position = new Vector3(currentPos.x, currentPos.y + currentHeight, 0);
    }


    private void CreateGroundEffect()
    {
        if (string.IsNullOrEmpty(groundEffectPoolTag)) return;

        GameObject groundEffect = ObjectPool.Instance.SpawnFromPool(
            groundEffectPoolTag,
            targetPosition,
            Quaternion.identity
        );

        if (groundEffect != null && groundEffect.TryGetComponent(out GrinderGroundEffect effect))
        {
            effect.SetPoolTag(groundEffectPoolTag);
            effect.Initialize(damage, attackRadius, groundEffectDuration, damageTickInterval);
        }
    }
}