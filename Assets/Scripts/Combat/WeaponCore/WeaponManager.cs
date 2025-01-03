using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private ItemGrid mainItemGrid;  // Inspector에서 할당

    private Dictionary<WeaponData, WeaponMechanism> activeWeapons = new Dictionary<WeaponData, WeaponMechanism>();
    private Dictionary<WeaponType, GameObject> weaponPrefabs = new Dictionary<WeaponType, GameObject>();
    private Dictionary<WeaponData, bool> activeEquipments = new Dictionary<WeaponData, bool>();
    private PlayerStats playerStats;

    private void Awake()
    {
        if (mainItemGrid == null)
        {
            Debug.LogError("MainItemGrid is not assigned to WeaponManager!");
        }

        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found on the same GameObject as WeaponManager!");
            return;
        }

        // PlayerStats의 모든 스탯 변경 이벤트에 리스너 등록
        playerStats.OnPowerChanged += UpdateAllWeaponsStats;
        playerStats.OnCooldownReduceChanged += UpdateAllWeaponsStats;
        playerStats.OnKnockbackChanged += UpdateAllWeaponsStats;
        playerStats.OnAreaOfEffectChanged += UpdateAllWeaponsStats;
    }
    private void ApplyEquipmentEffect(WeaponData equipmentData)
    {
        if (playerStats != null)
        {
            equipmentData.ApplyEquipmentEffect(playerStats);
        }
    }

    private void RemoveEquipmentEffect(WeaponData equipmentData)
    {
        if (playerStats != null)
        {
            equipmentData.RemoveEquipmentEffect(playerStats);
        }
    }

    private void Update()
    {
        var weaponsToRemove = new List<WeaponData>();
        var equipmentsToRemove = new List<WeaponData>();

        // 무기 체크
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

        // Equipment 체크
        foreach (var equipmentPair in activeEquipments)
        {
            WeaponData equipmentData = equipmentPair.Key;
            if (!IsWeaponInGrid(equipmentData))
            {
                equipmentsToRemove.Add(equipmentData);
                RemoveEquipmentEffect(equipmentData);
            }
        }

        // 제거할 무기와 장비 처리
        foreach (var weaponData in weaponsToRemove)
        {
            activeWeapons.Remove(weaponData);
        }

        foreach (var equipmentData in equipmentsToRemove)
        {
            activeEquipments.Remove(equipmentData);
        }
    }
    private void UpdateAllWeaponsStats()
    {
        // 모든 무기의 스탯 업데이트
        foreach (var weaponMechanism in activeWeapons.Values)
        {
            weaponMechanism.OnPlayerStatsChanged();
        }

        // Equipment 효과 재적용
        foreach (var equipmentPair in activeEquipments)
        {
            WeaponData equipmentData = equipmentPair.Key;
            RemoveEquipmentEffect(equipmentData);
            ApplyEquipmentEffect(equipmentData);
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

        // Equipment 타입인 경우
        if (weaponData.weaponType == WeaponType.Equipment)
        {
            if (!activeEquipments.ContainsKey(weaponData))
            {
                ApplyEquipmentEffect(weaponData);
                activeEquipments[weaponData] = true;
                UpdateAllWeaponsStats(); // Equipment 장착 시 모든 무기 스탯 업데이트
            }
            return;
        }

        // 이미 장착된 무기인 경우 스탯만 업데이트
        if (activeWeapons.ContainsKey(weaponData))
        {
            activeWeapons[weaponData].OnPlayerStatsChanged();
            return;
        }

        // 새로운 무기 장착
        WeaponMechanism mechanism = CreateWeaponMechanism(weaponData.weaponType);
        if (mechanism != null)
        {
            mechanism.Initialize(weaponData, transform);
            activeWeapons[weaponData] = mechanism;
        }
    }

    public void UnequipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        if (weaponData.weaponType == WeaponType.Equipment)
        {
            if (activeEquipments.ContainsKey(weaponData))
            {
                RemoveEquipmentEffect(weaponData);
                activeEquipments.Remove(weaponData);
                UpdateAllWeaponsStats(); // Equipment 제거 시 모든 무기 스탯 업데이트
            }
        }
        else if (activeWeapons.TryGetValue(weaponData, out WeaponMechanism mechanism))
        {
            CleanupWeaponMechanism(mechanism);
            activeWeapons.Remove(weaponData);
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

    public void ClearAllWeapons()
    {
        foreach (var mechanism in activeWeapons.Values)
        {
            CleanupWeaponMechanism(mechanism);
        }
        activeWeapons.Clear();

        foreach (var equipmentData in activeEquipments.Keys.ToList())
        {
            RemoveEquipmentEffect(equipmentData);
        }
        activeEquipments.Clear();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPowerChanged -= UpdateAllWeaponsStats;
            playerStats.OnCooldownReduceChanged -= UpdateAllWeaponsStats;
            playerStats.OnKnockbackChanged -= UpdateAllWeaponsStats;
            playerStats.OnAreaOfEffectChanged -= UpdateAllWeaponsStats;
        }
        ClearAllWeapons();
    }
}