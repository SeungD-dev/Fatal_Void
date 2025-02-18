using UnityEngine;

public class ForceFieldMechanism : WeaponMechanism
{
    private ForceFieldProjectile currentForceField;
    private Vector3 spawnPosition = Vector3.zero;
    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        CreateForceField();
    }


    public void Cleanup()
    {
        if (currentForceField != null && currentForceField.gameObject != null)
        {
            // Destroy 대신 풀링 시스템 사용
            ObjectPool.Instance?.ReturnToPool("ForceFieldProjectile", currentForceField.gameObject);
            currentForceField = null;
        }
    }
    private void CreateForceField()
    {
        if (weaponData.projectilePrefab == null)
        {
            Debug.LogError($"Force Field prefab is missing for weapon: {weaponData.weaponName}");
            return;
        }

        spawnPosition.x = playerTransform.position.x;
        spawnPosition.y = playerTransform.position.y;

        GameObject forceFieldObj = ObjectPool.Instance.SpawnFromPool(
            "ForceFieldProjectile",
            spawnPosition,
            Quaternion.identity
        );

        if (forceFieldObj != null && forceFieldObj.TryGetComponent(out ForceFieldProjectile forceField))
        {
            currentForceField = forceField;
            UpdateForceFieldStats();
        }
    }

    private void UpdateForceFieldStats()
    {
        if (currentForceField == null) return;

        currentForceField.Initialize(
            weaponData.CalculateFinalDamage(playerStats),
            Vector2.zero,
            0f,
            weaponData.CalculateFinalKnockback(playerStats),
            1f,
            1f
        );

        currentForceField.SetupForceField(
            weaponData.CurrentTierStats.forceFieldTickInterval,
            playerTransform,
            weaponData.CurrentTierStats.forceFieldRadius
        );
    }
    public override void UpdateMechanism()
    {
        // 포스필드는 Update에서 자체적으로 동작
    }

    // Attack은 사용하지 않음
    protected override void Attack(Transform target) { }


    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        UpdateForceFieldStats();
    }
}