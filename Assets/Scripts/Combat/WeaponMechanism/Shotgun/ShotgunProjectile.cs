using UnityEngine;

public class ShotgunProjectile : BaseProjectile
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamageAndEffects(enemy);
                // 샷건 투사체는 관통하지 않으므로 즉시 풀로 반환
                ReturnToPool();
            }
        }
    }
}
