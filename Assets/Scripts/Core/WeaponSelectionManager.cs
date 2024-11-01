using System.Collections.Generic;
using UnityEngine;

public class WeaponSelectionManager : MonoBehaviour
{
    //[SerializeField] private WeaponDatabase weaponDatabase;
    //[SerializeField] private int selectionCount = 3;

    //// 레어리티별 고정 확률
    //private readonly Dictionary<WeaponRarity, float> rarityDropRates = new Dictionary<WeaponRarity, float>()
    //{
    //    { WeaponRarity.common, 75f },
    //    { WeaponRarity.uncommon, 15f },
    //    { WeaponRarity.magic, 8f },
    //    { WeaponRarity.epic, 2f }
    //};

    //public List<WeaponData> SelectRandomWeapons()
    //{
    //    List<WeaponData> availableWeapons = weaponDatabase.GetAllWeapons();
    //    List<WeaponData> selectedWeapons = new List<WeaponData>();

    //    // selectionCount(3개)만큼 무기 선택
    //    while (selectedWeapons.Count < selectionCount)
    //    {
    //        // 확률에 따른 레어리티 결정
    //        float randomValue = Random.Range(0f, 100f);
    //        WeaponRarity selectedRarity = WeaponRarity.common;
    //        float cumulative = 0f;

    //        foreach (var rateEntry in rarityDropRates)
    //        {
    //            cumulative += rateEntry.Value;
    //            if (randomValue <= cumulative)
    //            {
    //                selectedRarity = rateEntry.Key;
    //                break;
    //            }
    //        }

    //        // 선택된 레어리티의 무기들 중에서 랜덤 선택
    //        List<WeaponData> weaponsOfRarity = weaponDatabase.GetWeaponsByRarity(selectedRarity);
    //        if (weaponsOfRarity.Count > 0)
    //        {
    //            WeaponData selectedWeapon;
    //            do
    //            {
    //                selectedWeapon = weaponsOfRarity[Random.Range(0, weaponsOfRarity.Count)];
    //            }
    //            while (selectedWeapons.Contains(selectedWeapon)); // 중복 방지

    //            selectedWeapons.Add(selectedWeapon);
    //        }
    //    }

    //    return selectedWeapons;
    //}

    //// 레어리티별 드롭률 수정을 위한 public 메서드 (필요한 경우)
    //public void UpdateDropRate(WeaponRarity rarity, float newRate)
    //{
    //    if (rarityDropRates.ContainsKey(rarity))
    //    {
    //        rarityDropRates[rarity] = newRate;
    //    }
    //}

    //// 현재 드롭률 확인을 위한 public 메서드 (필요한 경우)
    //public float GetDropRate(WeaponRarity rarity)
    //{
    //    return rarityDropRates.ContainsKey(rarity) ? rarityDropRates[rarity] : 0f;
    //}
}