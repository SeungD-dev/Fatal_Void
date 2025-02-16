using System.Collections;
using UnityEngine;

public class BeamSaberMechanism : WeaponMechanism
{
    private bool isSecondAttackReady;
    private const float SECOND_ATTACK_DELAY = 0.25f;
    private LayerMask enemyLayer;
    private MonoBehaviour ownerComponent;
    private float attackCooldown = 0f;
    private bool isComboAttack = false;
    private int comboCount = 0;
    private const int MAX_COMBO = 2;

    private GameObject ownerGameObject;
    private IEnumerator currentComboCoroutine;

    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        enemyLayer = LayerMask.GetMask("Enemy");
        ownerComponent = player.GetComponent<MonoBehaviour>();

        if (ownerComponent != null)
        {
            ownerGameObject = ownerComponent.gameObject;
            StopComboIfActive();
        }
        else
        {
            Debug.LogError("Failed to get MonoBehaviour component from player!");
        }
    }
    private void StopComboIfActive()
    {
        if (isComboAttack && currentComboCoroutine != null)
        {
            ownerComponent.StopCoroutine(currentComboCoroutine);
            currentComboCoroutine = null;
            isComboAttack = false;
            comboCount = 0;
        }
    }

    public override void UpdateMechanism()
    {
        if (!ownerGameObject.activeInHierarchy) return;

        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
            return;
        }

        if (Time.time >= lastAttackTime + currentAttackDelay)
        {
            Attack(null);
            lastAttackTime = Time.time;

            if (weaponData.currentTier >= 3 && !isComboAttack)
            {
                isComboAttack = true;
                comboCount = 1;
                currentComboCoroutine = ComboAttackSequence();
                ownerComponent.StartCoroutine(currentComboCoroutine);
            }
            else if (weaponData.currentTier < 3)
            {
                attackCooldown = currentAttackDelay * 0.8f;
            }
        }
    }

    private IEnumerator ComboAttackSequence()
    {
        WaitForSeconds waitDelay = new WaitForSeconds(SECOND_ATTACK_DELAY);

        while (comboCount < MAX_COMBO)
        {
            yield return waitDelay;
            if (!ownerGameObject.activeInHierarchy) break;

            SpawnCircularAttack();
            comboCount++;
        }

        attackCooldown = currentAttackDelay * 0.8f;
        isComboAttack = false;
        comboCount = 0;
        currentComboCoroutine = null;
    }

    protected override void Attack(Transform target)
    {
        if (ownerGameObject.activeInHierarchy)
        {
            SoundManager.Instance.PlaySound("Slash_sfx", 1f, false);
            SpawnCircularAttack();
        }
        else
        {
            isComboAttack = false;
            comboCount = 0;
        }
    }


    private void SpawnCircularAttack()
    {
        if (ObjectPool.Instance == null || !ownerGameObject.activeInHierarchy) return;

        GameObject projectileObj = ObjectPool.Instance.SpawnFromPool(poolTag, playerTransform.position, Quaternion.identity);
        if (projectileObj == null) return;

        if (projectileObj.TryGetComponent(out BeamSaberProjectile projectile))
        {
            projectile.SetPoolTag(poolTag);

            float finalDamage = weaponData.CalculateFinalDamage(playerStats);
            if (isComboAttack && comboCount > 1)
            {
                finalDamage *= 1.2f;
            }

            projectile.Initialize(
                finalDamage,
                Vector2.zero,
                0f,
                weaponData.CalculateFinalKnockback(playerStats),
                weaponData.CalculateFinalRange(playerStats),
                weaponData.CalculateFinalProjectileSize(playerStats)
            );

            projectile.SetupCircularAttack(
                weaponData.CalculateFinalRange(playerStats),
                enemyLayer,
                playerTransform
            );
        }
    }
    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        // 필요한 경우 추가적인 스탯 업데이트 로직
    }
}