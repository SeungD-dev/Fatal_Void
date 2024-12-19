using System.Collections;
using UnityEngine;
public class BeamSaberMechanism : WeaponMechanism
{
    private bool isSecondAttackReady = false;
    private float secondAttackDelay = 0.25f;
    private LayerMask enemyLayer;
    private MonoBehaviour ownerComponent;
    private bool isAttacking = false;  // 현재 공격 중인지 확인하는 플래그

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
        ownerComponent = player.GetComponent<MonoBehaviour>();
    }

    protected override void Attack(Transform target)
    {
        if (isAttacking) return;  // 이미 공격 중이면 새로운 공격 무시
        isAttacking = true;
        SpawnCircularAttack();
        ownerComponent. StartCoroutine(ResetAttackState());

        if (weaponData.currentTier >= 3 && !isSecondAttackReady)
        {
            isSecondAttackReady = true;
            ownerComponent.StartCoroutine(PerformSecondAttack());
        }
    }

    private IEnumerator ResetAttackState()
    {
        // 애니메이션이 완전히 끝날 때까지 대기 (25프레임)
        yield return new WaitForSeconds(25f / 60f);
        isAttacking = false;
    }

    // UpdateMechanism도 오버라이드하여 적 감지 없이 공격하도록 수정
    public override void UpdateMechanism()
    {
        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Attack(null);  // null을 전달해도 공격이 실행됨
            lastAttackTime = Time.time;
        }
    }

    private void SpawnCircularAttack()
    {
        // 오브젝트 스폰
        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(
            poolTag,
            playerTransform.position,
            Quaternion.identity
        );

        // 오브젝트가 비활성화 상태라면 활성화
        if (!projectileObj.activeSelf)
        {
            Debug.LogWarning("BeamSaber projectile was inactive after spawn, activating...");
            projectileObj.SetActive(true);
        }

        BeamSaberProjectile projectile = projectileObj.GetComponent<BeamSaberProjectile>();
        if (projectile != null)
        {
            // 초기화 순서 변경
            projectile.SetPoolTag(poolTag);  // 풀 태그 먼저 설정
            projectile.SetupCircularAttack(  // 그 다음 공격 설정
                weaponData.CalculateFinalRange(playerStats),
                enemyLayer,
                playerTransform
            );

            // 마지막으로 나머지 값들 초기화
            projectile.Initialize(
                weaponData.CalculateFinalDamage(playerStats),
                Vector2.zero,
                0f,
                weaponData.CalculateFinalKnockback(playerStats),
                weaponData.CalculateFinalRange(playerStats),
                weaponData.CalculateFinalProjectileSize(playerStats)
            );
        }
        else
        {
            Debug.LogError("BeamSaberProjectile component not found!");
        }
    }
    private IEnumerator PerformSecondAttack()
    {
        yield return new WaitForSeconds(secondAttackDelay);
        SpawnCircularAttack();
        isSecondAttackReady = false;
    }
}