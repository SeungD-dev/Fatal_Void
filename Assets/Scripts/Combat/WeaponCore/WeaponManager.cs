using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private List<WeaponMechanism> activeWeapons = new List<WeaponMechanism>();

    private void Update()
    {
        // 모든 활성화된 무기 메커니즘 업데이트
        for (int i = activeWeapons.Count - 1; i >= 0; i--)
        {
            activeWeapons[i].UpdateMechanism();
        }
    }

    public void EquipWeapon(WeaponData weaponData)
    {
        WeaponMechanism mechanism = null;

        // 무기 타입에 따라 직접 인스턴스 생성
        switch (weaponData.weaponType)
        {
            case WeaponType.Sword:
                mechanism = new LongSwordMechanism();
                break;
            case WeaponType.Bow:
                mechanism = new BowMechanism();
                break;
        }

        if (mechanism != null)
        {
            mechanism.Initialize(weaponData, transform);
            activeWeapons.Add(mechanism);
        }
    }

    public void UnequipWeapon(WeaponData weaponData)
    {
        activeWeapons.RemoveAll(weapon => weapon.GetType().Name == weaponData.weaponType.ToString() + "Mechanism");
    }
}


