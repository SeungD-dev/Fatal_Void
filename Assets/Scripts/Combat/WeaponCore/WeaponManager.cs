using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private List<WeaponMechanism> activeWeapons = new List<WeaponMechanism>();

    private void Update()
    {
        // 각 무기 독립적으로 업데이트
        for (int i = activeWeapons.Count - 1; i >= 0; i--)
        {
            if (activeWeapons[i] != null)
            {
                activeWeapons[i].UpdateMechanism();
            }
        }
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        WeaponMechanism mechanism = null;

        // 무기 데이터에 따라 적절한 메커니즘 생성
        switch (weaponData.weaponType)
        {
            case WeaponType.Buster:
                mechanism = new BusterMechanism();
                break;
            case WeaponType.Machinegun:
                mechanism = new MachinegunMechanism();
                break;
            case WeaponType.Blade:
                mechanism = new BladeMechanism();
                break;
            case WeaponType.Cutter:
                mechanism = new CutterMechanism();
                break;
            case WeaponType.Sawblade:
                mechanism = new SawbladeMechanism();
                break;
            case WeaponType.BeamSaber:
                mechanism = new BeamSaberMechanism();
                break;
                // 다른 무기 타입들도 각각 추가
        }

        if (mechanism != null)
        {
            mechanism.Initialize(weaponData, transform);
            activeWeapons.Add(mechanism);
            Debug.Log($"Weapon equipped: {weaponData.weaponName} (Total weapons: {activeWeapons.Count})");
        }
    }

    public void UnequipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        // 특정 무기 데이터와 일치하는 메커니즘을 찾아서 제거
        for (int i = activeWeapons.Count - 1; i >= 0; i--)
        {
            if (activeWeapons[i].GetWeaponData() == weaponData)
            {
                activeWeapons.RemoveAt(i);
                Debug.Log($"Weapon unequipped: {weaponData.weaponName}");
                break;
            }
        }
    }

    // 현재 장착된 무기 개수 확인 (디버깅용)
    public int GetActiveWeaponCount()
    {
        return activeWeapons.Count;
    }

    // 모든 무기 제거 (필요한 경우 사용)
    public void ClearAllWeapons()
    {
        activeWeapons.Clear();
        Debug.Log("All weapons cleared");
    }
}