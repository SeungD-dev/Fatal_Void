using UnityEngine;

public class BulletProjectile : BaseProjectile
{
    private const string DESTROY_VFX_TAG = "Bullet_DestroyVFX";

    protected void SpawnDestroyVFX()
    {
        GameObject vfx = ObjectPool.Instance.SpawnFromPool(DESTROY_VFX_TAG, transform.position, transform.rotation);
        if (vfx != null && vfx.TryGetComponent(out BulletDestroyVFX destroyVFX))
        {
            destroyVFX.SetPoolTag(DESTROY_VFX_TAG);
            Vector3 currentProjectileScale = transform.localScale;
            if (baseProjectileSize > 0)
            {
                currentProjectileScale *= baseProjectileSize;
            }
            destroyVFX.SetEffectScale(currentProjectileScale);
        }
    }
}
