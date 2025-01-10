using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 무기 정보 및 업그레이드 UI를 관리하는 클래스
/// </summary>
public class WeaponInfoUI : MonoBehaviour
{
    #region SerializeFields
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;
    //[SerializeField] private TextMeshProUGUI weaponStatsText;

    [Header("Upgrade System")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Scene References")]
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private ItemGrid mainItemGrid;
    #endregion

    #region Private Fields
    private PlayerStats playerStats;
    private WeaponData selectedWeapon;
    private List<InventoryItem> upgradeableWeapons;
    private bool isInitialized;
    #endregion

    #region Unity Methods
    private void Start()
    {
        ValidateReferences();
        InitializeUI();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    private void ValidateReferences()
    {
        if (weaponLevelText == null) Debug.LogError($"Missing reference: {nameof(weaponLevelText)} in {gameObject.name}");
        if (weaponNameText == null) Debug.LogError($"Missing reference: {nameof(weaponNameText)} in {gameObject.name}");
        if (weaponDescriptionText == null) Debug.LogError($"Missing reference: {nameof(weaponDescriptionText)} in {gameObject.name}");
        if (upgradeButton == null) Debug.LogError($"Missing reference: {nameof(upgradeButton)} in {gameObject.name}");
        if (upgradeButtonText == null) Debug.LogError($"Missing reference: {nameof(upgradeButtonText)} in {gameObject.name}");
        if (weaponDatabase == null) Debug.LogError($"Missing reference: {nameof(weaponDatabase)} in {gameObject.name}");
        if (mainItemGrid == null) Debug.LogError($"Missing reference: {nameof(mainItemGrid)} in {gameObject.name}");
        if (inventoryController == null) Debug.LogError($"Missing reference: {nameof(inventoryController)} in {gameObject.name}");
    }

    private void InitializeUI()
    {
        upgradeableWeapons = new List<InventoryItem>();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
            upgradeButton.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
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

    private void SubscribeToEvents()
    {
        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged += RefreshUpgradeUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        if (GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged -= RefreshUpgradeUI;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 무기 정보 UI를 업데이트합니다.
    /// </summary>
    /// <param name="weaponData">표시할 무기 데이터</param>
    public void UpdateWeaponInfo(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        selectedWeapon = weaponData;
        gameObject.SetActive(true);

        UpdateBasicInfo(weaponData);

        if (isInitialized && playerStats != null)
        {
            //UpdateDetailedStats(weaponData);
            CheckUpgradePossibility();
        }
    }

    /// <summary>
    /// 업그레이드 UI를 새로고침합니다.
    /// </summary>
    public void RefreshUpgradeUI()
    {
        if (selectedWeapon != null)
        {
            CheckUpgradePossibility();
        }
    }
    #endregion

    #region Private Methods
    private void OnGameStateChanged(GameState newState)
    {
        if (!isInitialized && GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }

    private void UpdateBasicInfo(WeaponData weaponData)
    {
        weaponLevelText.text = $"Tier {weaponData.currentTier}";
        weaponNameText.text = weaponData.weaponName;
        weaponDescriptionText.text = weaponData.weaponDescription;
    }

    //private void UpdateDetailedStats(WeaponData weaponData)
    //{
    //    float dps = weaponData.CalculateTheoreticalDPS(playerStats);
    //    float damage = weaponData.CalculateFinalDamage(playerStats);
    //    float attacksPerSecond = weaponData.CalculateAttacksPerSecond(playerStats);
    //    float knockbackPower = weaponData.CalculateFinalKnockback(playerStats);
    //    float range = weaponData.CalculateFinalRange(playerStats);
    //    float projectileSize = weaponData.CalculateFinalProjectileSize(playerStats);
    //    var penetrationInfo = weaponData.GetPenetrationInfo();

    //    string penetrationText = penetrationInfo.canPenetrate ?
    //        $"관통: {(penetrationInfo.maxCount == 0 ? "무제한" : penetrationInfo.maxCount.ToString())}회" :
    //        "관통: 불가";

    //    weaponStatsText.text = $"DPS: {dps:F1}\n" +
    //                          $"DMG: {damage:F1}\n" +
    //                          $"ASPD: {attacksPerSecond:F2}/s\n" +
    //                          $"Range: {range:F1}\n" +
    //                          $"Size: {projectileSize:F1}x\n" +
    //                          $"KnockBack: {knockbackPower:F1}x\n" +
    //                          penetrationText;
    //}

    private void CheckUpgradePossibility()
    {
        if (!ValidateUpgradeRequirements())
        {
            upgradeButton.gameObject.SetActive(false);
            return;
        }

        upgradeableWeapons.Clear();

        WeaponType targetType = selectedWeapon.weaponType;
        int targetTier = selectedWeapon.currentTier;
        EquipmentType targetEquipmentType = selectedWeapon.equipmentType;

        // 더 엄격한 업그레이드 가능 조건 검사
        SearchUpgradeableWeapons(targetType, targetTier, targetEquipmentType);

        // 동일한 무기 종류, 같은 티어, 최소 2개 필요
        bool canUpgrade = CanPerformUpgrade(targetType, targetTier, targetEquipmentType);

        UpdateUpgradeButtonState(canUpgrade);
    }

    private bool CanPerformUpgrade(WeaponType targetType, int targetTier, EquipmentType targetEquipmentType)
    {
        // 같은 종류, 같은 티어의 무기 개수 카운트
        int matchingWeaponCount = upgradeableWeapons.Count(weapon =>
            weapon.GetWeaponData().weaponType == targetType &&
            weapon.GetWeaponData().currentTier == targetTier &&
            (targetType != WeaponType.Equipment ||
             weapon.GetWeaponData().equipmentType == targetEquipmentType)
        );

        return matchingWeaponCount >= 2 && targetTier < 4;
    }

    private bool ValidateUpgradeRequirements()
    {
        if (!isInitialized || selectedWeapon == null || mainItemGrid == null || upgradeButton == null)
        {
            Debug.LogWarning($"CheckUpgradePossibility failed: initialized={isInitialized}, " +
                           $"selectedWeapon={selectedWeapon != null}, " +
                           $"mainItemGrid={mainItemGrid != null}, " +
                           $"upgradeButton={upgradeButton != null}");
            return false;
        }
        return true;
    }

    private void SearchUpgradeableWeapons(WeaponType targetType, int targetTier, EquipmentType targetEquipmentType)
    {
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item == null) continue;

                WeaponData itemWeapon = item.GetWeaponData();
                if (itemWeapon == null) continue;

                if (IsWeaponMatchingCriteria(itemWeapon, targetType, targetTier, targetEquipmentType))
                {
                    upgradeableWeapons.Add(item);
                }
            }
        }
    }

    private bool IsWeaponMatchingCriteria(WeaponData weaponData, WeaponType targetType, int targetTier, EquipmentType targetEquipmentType)
    {
        if (weaponData.currentTier != targetTier) return false;

        if (targetType == WeaponType.Equipment)
        {
            return weaponData.weaponType == targetType &&
                   weaponData.equipmentType == targetEquipmentType;
        }

        return weaponData.weaponType == targetType;
    }

    private void UpdateUpgradeButtonState(bool canUpgrade)
    {
        upgradeButton.gameObject.SetActive(canUpgrade);

        if (canUpgrade)
        {
            upgradeButtonText.text = $"Upgrade to Tier {selectedWeapon.currentTier + 1}";
        }
    }

    private void OnUpgradeButtonClick()
    {
        if (!ValidateUpgradeOperation())
        {
            Debug.LogWarning("Cannot upgrade: missing requirements");
            return;
        }

        WeaponData nextTierWeapon = GetNextTierWeapon();
        if (nextTierWeapon == null)
        {
            Debug.LogWarning("Failed to create next tier weapon");
            return;
        }

        ProcessWeaponUpgrade(nextTierWeapon);
    }

    private bool ValidateUpgradeOperation()
    {
        return isInitialized &&
               upgradeableWeapons != null &&
               upgradeableWeapons.Count >= 2 &&
               selectedWeapon != null &&
               mainItemGrid != null;
    }

    private WeaponData GetNextTierWeapon()
    {
        if (selectedWeapon == null || selectedWeapon.currentTier >= 4) return null;
        return selectedWeapon.CreateNextTierWeapon();
    }

    private void ProcessWeaponUpgrade(WeaponData nextTierWeapon)
    {
        Vector2Int upgradePosition = GetUpgradePosition();
        RemoveUpgradeMaterials();
        CreateUpgradedWeapon(nextTierWeapon, upgradePosition);
        CleanupUpgradeState();
    }

    private Vector2Int GetUpgradePosition()
    {
        var firstWeapon = upgradeableWeapons[0];
        return new Vector2Int(
            firstWeapon.GridPosition.x,
            firstWeapon.GridPosition.y
        );
    }

    private void RemoveUpgradeMaterials()
    {
        if (selectedWeapon.weaponType == WeaponType.Equipment)
        {
            RemoveEquipmentEffects();
        }

        foreach (var weapon in upgradeableWeapons.Take(2))
        {
            if (weapon != null)
            {
                mainItemGrid.RemoveItem(weapon.GridPosition);
                Destroy(weapon.gameObject);
            }
        }
    }

    private void RemoveEquipmentEffects()
    {
        var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            foreach (var weapon in upgradeableWeapons.Take(2))
            {
                WeaponData weaponData = weapon.GetWeaponData();
                if (weaponData != null)
                {
                    weaponManager.UnequipWeapon(weaponData);
                }
            }
        }
    }

    private void CreateUpgradedWeapon(WeaponData nextTierWeapon, Vector2Int position)
    {
        inventoryController?.CreateUpgradedItem(nextTierWeapon, position);
    }

    private void CleanupUpgradeState()
    {
        upgradeableWeapons.Clear();
        selectedWeapon = null;
        upgradeButton.gameObject.SetActive(false);
    }
    #endregion
}