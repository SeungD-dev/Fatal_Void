using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        // 스탯 스케일링
        float levelScaling = 1f + ((playerStats.Level - 1) * 0.05f);
        TierStats currentTierStats = scaledWeapon.CurrentTierStats;
        currentTierStats.damage *= levelScaling;
        currentTierStats.projectileSpeed *= levelScaling;
        currentTierStats.attackDelay /= (1f + ((playerStats.Level - 1) * 0.02f));

        // 첫 상점이면 무조건 가격을 0으로 설정
        if (isFirstShop)
        {
            scaledWeapon.price = 0;
        }
        else
        {
            // 일반 상점일 경우 레벨에 따른 가격 스케일링 적용
            float priceScaling = 1f + ((playerStats.Level - 1) * 0.1f);
            int basePrice = scaledWeapon.currentTier switch
            {
                1 => scaledWeapon.tier1Price,
                2 => scaledWeapon.tier2Price,
                3 => scaledWeapon.tier3Price,
                4 => scaledWeapon.tier4Price,
                _ => scaledWeapon.tier1Price
            };
            scaledWeapon.price = Mathf.RoundToInt(basePrice * priceScaling);
        }

        return scaledWeapon;
    }
    private List<WeaponData> GetRandomWeapons(int count)
    {
        if (weaponDatabase == null || playerStats == null)
        {
            Debug.LogError("WeaponDatabase or PlayerStats is missing!");
            return new List<WeaponData>();
        }

        List<WeaponData> randomWeapons = new List<WeaponData>();

        for (int i = 0; i < count; i++)
        {
            WeaponData weapon = GetRandomWeaponByTierProbability();
            if (weapon != null)
            {
                randomWeapons.Add(weapon);
            }
        }

        return randomWeapons;
    }

    private WeaponData GetRandomWeaponByTierProbability()
    {
        // 현재 레벨에서의 각 티어 확률을 한번에 가져오기
        float[] tierProbs = weaponDatabase.tierProbability.GetTierProbabilities(playerStats.Level);

        // 랜덤 값으로 티어 선택 (0-100 사이의 값)
        float random = Random.value * 100f;
        float cumulative = 0f;
        int selectedTier = 1;

        for (int i = 0; i < 4; i++)
        {
            cumulative += tierProbs[i];
            if (random <= cumulative)
            {
                selectedTier = i + 1;
                break;
            }
        }

        // 선택된 티어의 무기들 중에서 랜덤 선택
        List<WeaponData> tierWeapons = weaponDatabase.weapons
            .Where(w => w.currentTier == selectedTier)
            .ToList();

        if (tierWeapons.Count == 0)
        {
            Debug.LogWarning($"No weapons found for tier {selectedTier}");
            return null;
        }

        // 무기를 복제하여 반환 (가격 설정은 ScaleWeaponToPlayerLevel에서 처리)
        return ScriptableObject.Instantiate(tierWeapons[Random.Range(0, tierWeapons.Count)]);
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