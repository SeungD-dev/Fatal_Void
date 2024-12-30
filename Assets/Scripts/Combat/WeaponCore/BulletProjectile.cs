using UnityEngine;

public class BulletProjectile : BaseProjectile
{
    protected void SpawnDestroyVFX()
    {
        GameObject vfx = ObjectPool.Instance.SpawnFromPool("Bullet_DestroyVFX", transform.position, transform.rotation);
        if (vfx != null)
        {
            BulletDestroyVFX destroyVFX = vfx.GetComponent<BulletDestroyVFX>();
            if (destroyVFX != null)
            {
                destroyVFX.SetPoolTag("Bullet_DestroyVFX");

                // 현재 투사체의 실제 크기를 전달
                Vector3 currentProjectileScale = transform.localScale;

                // projectileSize가 있다면 그것도 고려 (BaseProjectile에 있다면)
                if (baseProjectileSize > 0)
                {
                    currentProjectileScale *= baseProjectileSize;
                }

                destroyVFX.SetEffectScale(currentProjectileScale);
            }
        }
    }
}
