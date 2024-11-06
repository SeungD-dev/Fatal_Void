using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Inventory/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponData> weapons = new List<WeaponData>();

    public WeaponData GetWeaponByName(string name)
    {
        return weapons.Find(w => w.weaponName == name);
    }

}
