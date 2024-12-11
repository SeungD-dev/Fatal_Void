using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class WeaponInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponRarityImage;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponDPSText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;

    [Header("Upgrade System")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Scene References")]
    [SerializeField] private InventoryController inventoryController;

    private PlayerStats playerStats;
    private WeaponData selectedWeapon;
    private ItemGrid currentGrid;
    private List<InventoryItem> upgradeableWeapons;
    private bool isInitialized = false;

    private void Start()
    {
        ValidateReferences();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
            upgradeButton.gameObject.SetActive(false);
        }

        if (GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }

        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void ValidateReferences()
    {
        if (weaponLevelText == null) Debug.LogError($"Missing reference: {nameof(weaponLevelText)} in {gameObject.name}");
        if (weaponNameText == null) Debug.LogError($"Missing reference: {nameof(weaponNameText)} in {gameObject.name}");
        if (weaponRarityImage == null) Debug.LogError($"Missing reference: {nameof(weaponRarityImage)} in {gameObject.name}");
        if (weaponImage == null) Debug.LogError($"Missing reference: {nameof(weaponImage)} in {gameObject.name}");
        if (weaponDPSText == null) Debug.LogError($"Missing reference: {nameof(weaponDPSText)} in {gameObject.name}");
        if (weaponDescriptionText == null) Debug.LogError($"Missing reference: {nameof(weaponDescriptionText)} in {gameObject.name}");
        if (upgradeButton == null) Debug.LogError($"Missing reference: {nameof(upgradeButton)} in {gameObject.name}");
        if (upgradeButtonText == null) Debug.LogError($"Missing reference: {nameof(upgradeButtonText)} in {gameObject.name}");
        if (weaponDatabase == null) Debug.LogError($"Missing reference: {nameof(weaponDatabase)} in {gameObject.name}");
        if (inventoryController == null) Debug.LogError($"Missing reference: {nameof(inventoryController)} in {gameObject.name}");
    }

    private void InitializeReferences()
    {
        if (isInitialized) return;

        playerStats = GameManager.Instance.PlayerStats;

        if (playerStats != null && inventoryController != null)
        {
            isInitialized = true;
            if (selectedWeapon != null)
            {
                UpdateWeaponInfo(selectedWeapon);
            }
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (!isInitialized && GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }

    public void UpdateWeaponInfo(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        selectedWeapon = weaponData;

        gameObject.SetActive(true);

        // 기본 정보 업데이트
        weaponLevelText.text = $"Tier {weaponData.currentTier}";
        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        weaponDescriptionText.text = weaponData.weaponDescription;

        Color rarityColor = GetRarityColor(weaponData.rarity);
        weaponRarityImage.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);

        if (isInitialized && playerStats != null)
        {
            UpdateDetailedStats(weaponData);
            CheckUpgradePossibility();
        }
    }

    private void UpdateDetailedStats(WeaponData weaponData)
    {
        float dps = weaponData.CalculateTheoreticalDPS(playerStats);
        float damage = weaponData.CalculateFinalDamage(playerStats);
        float attacksPerSecond = weaponData.CalculateAttacksPerSecond(playerStats);
        float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
        float range = weaponData.CalculateFinalRange(playerStats);
        float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
        var penetrationInfo = weaponData.GetPenetrationInfo();

        string penetrationText = penetrationInfo.canPenetrate ?
            $"관통: {(penetrationInfo.maxCount == 0 ? "무제한" : penetrationInfo.maxCount.ToString())}회" :
            "관통: 불가";

        string statText = $"DPS: {dps:F1}\n" +
                         $"DMG: {damage:F1}\n" +
                         $"ASPD: {attacksPerSecond:F2}/s\n" +
                         $"Range: {range:F1}\n" +
                         $"Size: {projectileSize:F1}x\n" +
                         $"KnockBack: {knockbackPower:F1}x\n" +
                         penetrationText;

        weaponDPSText.text = statText;
    }

    private void CheckUpgradePossibility()
    {
        if (!isInitialized || selectedWeapon == null || currentGrid == null || upgradeButton == null) return;

        upgradeableWeapons = new List<InventoryItem>();

        for (int x = 0; x < currentGrid.Width; x++)
        {
            for (int y = 0; y < currentGrid.Height; y++)
            {
                InventoryItem item = currentGrid.GetItem(x, y);
                if (item != null &&
                    item.weaponData.weaponType == selectedWeapon.weaponType &&
                    item.weaponData.currentTier == selectedWeapon.currentTier)
                {
                    upgradeableWeapons.Add(item);
                }
            }
        }

        bool canUpgrade = upgradeableWeapons.Count >= 2 && selectedWeapon.currentTier < 4;
        upgradeButton.gameObject.SetActive(canUpgrade);

        if (canUpgrade)
        {
            upgradeButtonText.text = $"Upgrade to Tier {selectedWeapon.currentTier + 1}";
        }
    }

    private void OnUpgradeButtonClick()
    {
        if (!isInitialized || upgradeableWeapons.Count < 2 || selectedWeapon == null) return;

        WeaponData nextTierWeapon = GetNextTierWeapon();
        if (nextTierWeapon == null) return;

        for (int i = 0; i < 2; i++)
        {
            if (upgradeableWeapons[i] != null)
            {
                Destroy(upgradeableWeapons[i].gameObject);
            }
        }

        if (inventoryController != null)
        {
            inventoryController.CreatePurchasedItem(nextTierWeapon);
        }

        upgradeButton.gameObject.SetActive(false);
    }

    private WeaponData GetNextTierWeapon()
    {
        if (selectedWeapon == null || selectedWeapon.currentTier >= 4) return null;
        return selectedWeapon.CreateNextTierWeapon();
    }

    private Color GetRarityColor(WeaponRarity rarity)
    {
        return rarity switch
        {
            WeaponRarity.common => new Color(0.8f, 0.8f, 0.8f),
            WeaponRarity.uncommon => new Color(0.3f, 0.3f, 0.3f),
            _ => Color.white
        };
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}