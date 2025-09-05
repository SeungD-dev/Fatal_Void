using UnityEngine;

/// <summary>
/// HellFire (Shotgun X-Tier) 전용 투사체
/// 기본 BaseProjectile 기능을 사용
/// </summary>
public class HellFireProjectile : BaseProjectile
{

    public override void Initialize(
        float damage,
        Vector2 direction,
        float speed,
        float knockbackPower = 0f,
        float range = 10f,
        float projectileSize = 1f,
        bool canPenetrate = false,
        int maxPenetrations = 0,
        float damageDecay = 0.1f)
    {
        base.Initialize(damage, direction, speed, knockbackPower, range, projectileSize, 
                       canPenetrate, maxPenetrations, damageDecay);
    }

}