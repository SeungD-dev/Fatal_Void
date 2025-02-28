using UnityEngine;

public class ShotgunProjectile : BulletProjectile
{
    // 투사체 상태 관리
    private int processingState = 0; // 0: 초기, 1: 활성화, 2: 충돌 중, 3: 반환 중

    protected override void OnEnable()
    {
        base.OnEnable();
        processingState = 1;  // 활성 상태로 설정
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 처리 중이면 무시
        if (processingState != 1) return;

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                processingState = 2;  // 충돌 처리 중
                ApplyDamageAndEffects(enemy);
                SpawnDestroyVFX();

                processingState = 3;  // 반환 중
                ReturnToPool();
            }
        }
    }

    protected override void Update()
    {
        // 활성 상태일 때만 업데이트 처리
        if (processingState != 1) return;

        // 이동 처리
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // 최대 사거리 확인
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            processingState = 3;  // 반환 중
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }

    protected override void ReturnToPool()
    {
        // 이미 반환 중인지 확인
        if (processingState == 3 && !string.IsNullOrEmpty(poolTag))
        {
            // 안전하게 풀에 반환
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            // 풀 태그가 없으면 비활성화
            gameObject.SetActive(false);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        processingState = 0;  // 초기 상태로 리셋
    }
}