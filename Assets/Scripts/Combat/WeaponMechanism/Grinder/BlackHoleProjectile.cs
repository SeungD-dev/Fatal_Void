using UnityEngine;

/// <summary>
/// Black Hole (Grinder X-Tier) 전용 투사체
/// GrinderProjectile을 상속하여 BlackHoleGroundEffect를 생성
/// </summary>
public class BlackHoleProjectile : GrinderProjectile
{
    // Black Hole 전용 속성들
    private float blackHoleScaleMultiplier;
    private float blackHoleDuration;
    private float blackHoleDamageInterval;

    /// <summary>
    /// BlackHole 전용 Initialize 메서드
    /// </summary>
    public void Initialize(
        float damage,
        Vector2 direction,
        float speed,
        Vector2 targetPos,
        float radius,
        float duration,
        float tickInterval,
        string effectPoolTag,
        float size,
        float scaleMultiplier,
        float blackHoleDuration,
        float blackHoleDamageInterval)
    {
        // 기본 GrinderProjectile Initialize 호출
        base.Initialize(damage, direction, speed, targetPos, radius, duration, tickInterval, effectPoolTag, size);

        // BlackHole 전용 속성 설정
        this.blackHoleScaleMultiplier = scaleMultiplier;
        this.blackHoleDuration = blackHoleDuration;
        this.blackHoleDamageInterval = blackHoleDamageInterval;
    }

    /// <summary>
    /// BlackHoleGroundEffect 생성 (기본 CreateGroundEffect 오버라이드)
    /// </summary>
    protected override void CreateGroundEffect()
    {
        if (string.IsNullOrEmpty(groundEffectPoolTag)) return;

        GameObject groundEffect = ObjectPool.Instance.SpawnFromPool(
            groundEffectPoolTag,
            targetPosition,
            Quaternion.identity
        );

        if (groundEffect != null && groundEffect.TryGetComponent(out BlackHoleGroundEffect effect))
        {
            effect.SetPoolTag(groundEffectPoolTag);
            effect.Initialize(
                damage, 
                attackRadius, 
                blackHoleScaleMultiplier, 
                blackHoleDuration, 
                blackHoleDamageInterval
            );
        }
        else if (groundEffect != null && groundEffect.TryGetComponent(out GrinderGroundEffect grinderEffect))
        {
            // BlackHoleGroundEffect가 없으면 일반 GrinderGroundEffect를 대체 사용
            grinderEffect.SetPoolTag(groundEffectPoolTag);
            grinderEffect.Initialize(damage, attackRadius, groundEffectDuration, damageTickInterval);
        }
    }
}