using UnityEngine;
using System.Collections.Generic;

public class ShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions; // 3개의 WeaponOption 참조
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;

    public void InitializeShop()
    {
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);
        if (weaponOptions == null || weaponOptions.Length == 0)
        {
            Debug.LogError("No weapon options assigned to ShopUI!");
            return;
        }

        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }

    private List<WeaponData> GetRandomWeapons(int count)
    {
        List<WeaponData> allWeapons = new List<WeaponData>(weaponDatabase.weapons);
        List<WeaponData> randomWeapons = new List<WeaponData>();

        while (randomWeapons.Count < count && allWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, allWeapons.Count);
            randomWeapons.Add(allWeapons[randomIndex]);
            allWeapons.RemoveAt(randomIndex);
        }

        return randomWeapons;
    }

    public void PurchaseWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            // 상점 UI 닫기
            shopUI.SetActive(false);

            // 인벤토리 UI 열기
            inventoryUI.SetActive(true);

            // 선택한 무기를 인벤토리에 추가
            inventoryController.OnPurchaseItem(weaponData);
        }
    }

    public void SellWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            Debug.Log($"Selling weapon: {weaponData.weaponName}");
            // 판매 로직 구현
        }
    }

    public void RefreshShop()
    {
        InitializeShop();
    }
}
