using UnityEngine;

public class ShotgunProjectile : BulletProjectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageAndEffects(enemy);
                SpawnDestroyVFX();  // 소멸 효과 추가
                ReturnToPool();
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // 최대 사거리 도달 시 소멸 효과 추가
        if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
        {
            SpawnDestroyVFX();
            ReturnToPool();
        }
    }
}
