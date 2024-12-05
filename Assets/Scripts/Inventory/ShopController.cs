using UnityEngine;
using System.Collections.Generic;

public class ShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;

    private bool isFirstShop = true;
    private PlayerStats playerStats;

    public void InitializeShop()
    {
        // PlayerStats 참조 가져오기
        if (playerStats == null)
        {
            playerStats = GameManager.Instance.PlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats reference is null in ShopController!");
                return;
            }
        }

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
                // 원본 무기 데이터를 복제하여 레벨에 맞게 조정
                WeaponData scaledWeapon = ScaleWeaponToPlayerLevel(randomWeapons[i]);

                // 첫 상점이면 무기 가격을 0으로 설정
                if (isFirstShop)
                {
                    scaledWeapon.price = 0;
                }

                weaponOptions[i].Initialize(scaledWeapon, this);
            }
        }

        isFirstShop = false;
    }

    private WeaponData ScaleWeaponToPlayerLevel(WeaponData originalWeapon)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is null when trying to scale weapon!");
            return originalWeapon;
        }

        // 원본 데이터를 수정하지 않기 위해 복제
        WeaponData scaledWeapon = ScriptableObject.Instantiate(originalWeapon);

        // 무기 레벨을 플레이어 레벨로 설정
        scaledWeapon.weaponLevel = playerStats.Level;

        // 기본 공격력에 레벨 배수 추가
        float baseDamage = originalWeapon.weaponDamage;
        float levelBonus = scaledWeapon.levelMultiplier * (playerStats.Level - 1);
        float powerBonus = 1 + (playerStats.Power / 100f); // Power 스탯에 따른 데미지 증가

        scaledWeapon.weaponDamage = Mathf.RoundToInt((baseDamage + levelBonus) * powerBonus);

        // 공격 속도(쿨다운) 조정
        float cooldownReduction = 1f - (playerStats.CooldownReduce / 100f);
        scaledWeapon.attackDelay *= cooldownReduction;

        // 가격 조정
        int basePrice = originalWeapon.price;
        scaledWeapon.price = basePrice + Mathf.RoundToInt(basePrice * (scaledWeapon.weaponLevel - 1));

        return scaledWeapon;
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
            shopUI.SetActive(false);
            inventoryUI.SetActive(true);
            inventoryController.OnPurchaseItem(weaponData);
        }
    }

    public void RefreshShop()
    {
        InitializeShop();
    }
}