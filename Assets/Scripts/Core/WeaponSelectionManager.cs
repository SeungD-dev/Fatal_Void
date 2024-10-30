using System.Collections.Generic;
using UnityEngine;

public class WeaponSelectionManager : MonoBehaviour
{
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private int selectionCount = 3;

    public List<WeaponData> SelectRandomWeapons()
    {
        List<WeaponData> availableWeapons = weaponDatabase.GetAllWeapons();
        List<WeaponData> selectedWeapons = new List<WeaponData>();

        foreach (var weapon in availableWeapons)
        {
            // 무기의 기본 드롭율에 레어리티 배율을 적용
            float finalDropRate = weapon.dropRate *
                weaponDatabase.GetRarityDropMultiplier(weapon.rarity);

            if (Random.Range(0, 100f) <= finalDropRate)
            {
                selectedWeapons.Add(weapon);
            }
        }

        // selectionCount(3개)보다 많이 선택된 경우, 무작위로 줄임
        while (selectedWeapons.Count > selectionCount && selectedWeapons.Count > 0)
        {
            selectedWeapons.RemoveAt(Random.Range(0, selectedWeapons.Count));
        }

        // selectionCount(3개)보다 적게 선택된 경우, 무작위로 채움
        while (selectedWeapons.Count < selectionCount && availableWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, availableWeapons.Count);
            if (!selectedWeapons.Contains(availableWeapons[randomIndex]))
            {
                selectedWeapons.Add(availableWeapons[randomIndex]);
            }
        }

        return selectedWeapons;
    }
}