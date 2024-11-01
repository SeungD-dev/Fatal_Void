using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ScriptableObject
{
    public int width = 1;
    public int height = 1;

    public Sprite weaponIcon;
}