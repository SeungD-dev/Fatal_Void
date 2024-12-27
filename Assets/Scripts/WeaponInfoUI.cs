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

    [Header("Grid References")]
    [SerializeField] private ItemGrid mainItemGrid;

    private PlayerStats playerStats;
    private WeaponData selectedWeapon;
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

        // Grid 이벤트 리스너 등록
        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged += RefreshUpgradeUI;
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
        if (mainItemGrid == null) Debug.LogError($"Missing reference: {nameof(mainItemGrid)} in {gameObject.name}");
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
    public void RefreshUpgradeUI()
    {
        if (selectedWeapon != null)
        {
            CheckUpgradePossibility();
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
        if (!isInitialized || selectedWeapon == null || mainItemGrid == null || upgradeButton == null)
        {
            Debug.LogWarning($"CheckUpgradePossibility failed: initialized={isInitialized}, selectedWeapon={selectedWeapon != null}, mainItemGrid={mainItemGrid != null}, upgradeButton={upgradeButton != null}");
            return;
        }

        upgradeableWeapons = new List<InventoryItem>();

        // 현재 선택된 무기의 정보 저장
        WeaponType targetType = selectedWeapon.weaponType;
        int targetTier = selectedWeapon.currentTier;

        // Grid의 모든 아이템을 검사하면서 같은 종류, 같은 티어의 무기만 수집
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item != null && item.weaponData != null &&
                    item.weaponData.weaponType == targetType &&
                    item.weaponData.currentTier == targetTier &&
                    !upgradeableWeapons.Contains(item))  // 중복 체크
                {
                    upgradeableWeapons.Add(item);
                }
            }
        }

        bool canUpgrade = upgradeableWeapons.Count >= 2 && targetTier < 4;
        upgradeButton.gameObject.SetActive(canUpgrade);

        if (canUpgrade)
        {
            upgradeButtonText.text = $"Upgrade to Tier {targetTier + 1}";
        }
    }
    private void OnUpgradeButtonClick()
    {
        if (!isInitialized || upgradeableWeapons == null || upgradeableWeapons.Count < 2 || selectedWeapon == null || mainItemGrid == null)
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

        // 기존 무기들의 위치를 저장 (첫 번째 무기의 위치를 사용)
        Vector2Int upgradePosition = new Vector2Int(
            upgradeableWeapons[0].onGridPositionX,
            upgradeableWeapons[0].onGridPositionY
        );

        // 기존 무기들을 Grid에서 완전히 제거
        foreach (var weapon in upgradeableWeapons.Take(2))
        {
            if (weapon != null)
            {
                // Grid에서 참조 제거
                mainItemGrid.PickUpItem(weapon.onGridPositionX, weapon.onGridPositionY);
                // GameObject 파괴
                Destroy(weapon.gameObject);
            }
        }

        // 새 무기 생성 및 배치 전에 리스트 초기화
        upgradeableWeapons.Clear();
        selectedWeapon = null;  // 선택된 무기 정보도 초기화

        // 새 무기 생성 및 배치
        if (inventoryController != null)
        {
            inventoryController.CreateUpgradedItem(nextTierWeapon, upgradePosition);
        }

        // UI 상태 초기화
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
        if (mainItemGrid != null)
        {
            mainItemGrid.OnGridChanged -= RefreshUpgradeUI;
        }
    }
}