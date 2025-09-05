using UnityEngine;

public class ForceFieldMechanism : WeaponMechanism
{
    protected ForceFieldProjectile currentForceField;
    protected Vector3 spawnPosition = Vector3.zero;
    public override void Initialize(WeaponData data, Transform player)
    {
        base.Initialize(data, player);
        CreateForceField();
    }


    public void Cleanup()
    {
        if (currentForceField != null && currentForceField.gameObject != null)
        {
            // Destroy ��� Ǯ�� �ý��� ���
            ObjectPool.Instance?.ReturnToPool("ForceField_Projectile", currentForceField.gameObject);
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
            "ForceField_Projectile",
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
        // �����ʵ�� Update���� ��ü������ ����
    }

    // Attack�� ������� ����
    protected override void Attack(Transform target) { }


    public override void OnPlayerStatsChanged()
    {
        base.OnPlayerStatsChanged();
        UpdateForceFieldStats();
    }
}