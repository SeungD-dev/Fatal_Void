using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Inventory/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private List<WeaponData> weapons;

    [Header("Rarity Settings")]
    [SerializeField] private SerializableDictionary<WeaponRarity, Color> rarityColors;
    [SerializeField] private SerializableDictionary<WeaponRarity, float> rarityDropMultipliers;

    // 기본 무기 조회
    public WeaponData GetWeaponById(string id)
    {
        return weapons.Find(w => w.id == id);
    }

    // 특정 레어리티의 무기 목록 조회
    public List<WeaponData> GetWeaponsByRarity(WeaponRarity rarity)
    {
        return weapons.FindAll(w => w.rarity == rarity);
    }

    // 레어리티별 색상 조회
    public Color GetRarityColor(WeaponRarity rarity)
    {
        return rarityColors[rarity];
    }

    // 레어리티별 드롭 배율 조회
    public float GetRarityDropMultiplier(WeaponRarity rarity)
    {
        return rarityDropMultipliers[rarity];
    }

    // 전체 무기 목록 조회
    public List<WeaponData> GetAllWeapons()
    {
        return new List<WeaponData>(weapons);
    }
}
