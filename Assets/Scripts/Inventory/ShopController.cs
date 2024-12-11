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

        // 티어 1로 초기화 (상점에서는 항상 1티어로 시작)
        scaledWeapon.currentTier = 1;

        // 플레이어 레벨에 따른 스탯 스케일링
        float levelScaling = 1f + ((playerStats.Level - 1) * 0.05f); // 레벨당 5% 증가

        // 현재 티어의 스탯 스케일링
        TierStats currentTierStats = scaledWeapon.CurrentTierStats;
        currentTierStats.damage *= levelScaling;
        currentTierStats.projectileSpeed *= levelScaling;
        currentTierStats.attackDelay /= (1f + ((playerStats.Level - 1) * 0.02f)); // 레벨당 2% 빨라짐

        // 가격 조정
        if (!isFirstShop)
        {
            int basePrice = originalWeapon.price;
            float priceScaling = 1f + ((playerStats.Level - 1) * 0.1f); // 레벨당 10% 증가
            scaledWeapon.price = Mathf.RoundToInt(basePrice * priceScaling);
        }
        else
        {
            scaledWeapon.price = 0;
        }

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