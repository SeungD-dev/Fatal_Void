using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private ItemGrid mainItemGrid;  // Inspector에서 할당

    private Dictionary<WeaponData, WeaponMechanism> activeWeapons = new Dictionary<WeaponData, WeaponMechanism>();
    private Dictionary<WeaponType, GameObject> weaponPrefabs = new Dictionary<WeaponType, GameObject>();

    private void Awake()
    {
        if (mainItemGrid == null)
        {
            Debug.LogError("MainItemGrid is not assigned to WeaponManager!");
        }
    }

    private void Update()
    {
        // Grid에서 제거된 무기들 확인 및 정리
        var weaponsToRemove = new List<WeaponData>();

        foreach (var weaponPair in activeWeapons)
        {
            WeaponData weaponData = weaponPair.Key;
            WeaponMechanism mechanism = weaponPair.Value;

            if (!IsWeaponInGrid(weaponData))
            {
                weaponsToRemove.Add(weaponData);
                CleanupWeaponMechanism(mechanism);
            }
            else if (mechanism != null)
            {
                mechanism.UpdateMechanism();
            }
        }

        // 제거할 무기들 처리
        foreach (var weaponData in weaponsToRemove)
        {
            activeWeapons.Remove(weaponData);
            Debug.Log($"Removed unequipped weapon: {weaponData.weaponName}");
        }
    }

    private void CleanupWeaponMechanism(WeaponMechanism mechanism)
    {
        switch (mechanism)
        {
            case ForceFieldMechanism forceField:
                forceField.Cleanup();
                break;
            case BeamSaberMechanism beamSaber:
                // BeamSaber의 특별한 정리가 필요한 경우
                break;
            case GrinderMechanism grinder:
                // Grinder의 특별한 정리가 필요한 경우
                break;
                // 다른 특별한 정리가 필요한 무기들 추가
        }
    }

    private bool IsWeaponInGrid(WeaponData weaponData)
    {
        if (mainItemGrid == null || weaponData == null) return false;

        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item != null && item.weaponData == weaponData)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        // 이미 해당 무기가 활성화되어 있다면 무시
        if (activeWeapons.ContainsKey(weaponData))
        {
            Debug.Log($"Weapon already equipped: {weaponData.weaponName}");
            return;
        }

        WeaponMechanism mechanism = CreateWeaponMechanism(weaponData.weaponType);
        if (mechanism != null)
        {
            mechanism.Initialize(weaponData, transform);
            activeWeapons[weaponData] = mechanism;
            Debug.Log($"Weapon equipped: {weaponData.weaponName} (Total weapons: {activeWeapons.Count})");
        }
    }

    private WeaponMechanism CreateWeaponMechanism(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Buster => new BusterMechanism(),
            WeaponType.Machinegun => new MachinegunMechanism(),
            WeaponType.Blade => new BladeMechanism(),
            WeaponType.Cutter => new CutterMechanism(),
            WeaponType.Sawblade => new SawbladeMechanism(),
            WeaponType.BeamSaber => new BeamSaberMechanism(),
            WeaponType.Shotgun => new ShotgunMechanism(),
            WeaponType.Grinder => new GrinderMechanism(),
            WeaponType.ForceFieldGenerator => new ForceFieldMechanism(),
            WeaponType.Equipment => null, // Equipment는 별도 처리
            _ => null
        };
    }

    public void UnequipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        if (activeWeapons.TryGetValue(weaponData, out WeaponMechanism mechanism))
        {
            CleanupWeaponMechanism(mechanism);
            activeWeapons.Remove(weaponData);
            Debug.Log($"Weapon unequipped: {weaponData.weaponName}");
        }
    }

    public void ClearAllWeapons()
    {
        foreach (var mechanism in activeWeapons.Values)
        {
            if (mechanism != null)
            {
                CleanupWeaponMechanism(mechanism);
            }
        }
        activeWeapons.Clear();
        Debug.Log("All weapons cleared");
    }

    private void OnDestroy()
    {
        ClearAllWeapons();
    }
}