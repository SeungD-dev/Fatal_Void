using UnityEngine;

public class BusterMechanism : WeaponMechanism
{
    protected override void Attack(Transform target)
    {
        FireProjectile(target);
    }
}
