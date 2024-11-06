using Unity.Android.Gradle.Manifest;
using UnityEngine;

public abstract class WeaponMechanism
{
    protected WeaponData weaponData;
    protected float lastAttackTime;
    protected Transform playerTransform;

    public virtual void Initialize(WeaponData data, Transform player)
    {
        weaponData = data;
        lastAttackTime = 0f;
        playerTransform = player;
    }

    public virtual void UpdateMechanism()
    {
        if (Time.time >= lastAttackTime + weaponData.attackDelay)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    protected abstract void Attack();
}
