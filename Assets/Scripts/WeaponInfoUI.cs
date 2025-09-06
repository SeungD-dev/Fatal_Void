using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ���� ���� �� ���׷��̵� UI�� �����ϴ� Ŭ����
/// </summary>
public class WeaponInfoUI : MonoBehaviour
{
    #region SerializeFields
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;
    [SerializeField] private Button sellButton; // �Ǹ� ��ư �߰�
    [SerializeField] private TextMeshProUGUI sellPriceText; // �Ǹ� ���� �ؽ�Ʈ �߰�


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

        // �ʱ�ȭ�� �����ϸ� ��� �ʱ�ȭ
        if (GameManager.Instance != null && GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }

    private void OnDestroy() => UnsubscribeFromEvents();
    #endregion

    #region Public Methods
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
            CheckUpgradePossibility();
            UpdateSellButton();
        }
    }
    private void UpdateSellButton()
    {
        if (sellButton != null && selectedWeapon != null)
        {
            bool canSell = CanSellCurrentItem();
            sellButton.gameObject.SetActive(true);
            sellButton.interactable = canSell;

            if (sellPriceText != null)
            {
                sellPriceText.text = $"Sell: {selectedWeapon.SellPrice}";
            }
        }
    }
    private bool CanSellCurrentItem()
    {
        // �⺻ ����: �κ��丮�� 2�� �̻��� �������� ���� ���� �Ǹ� ����
        int itemCount = CountItemsInGrid();

        // ����: ���õ� �������� ���� �������� ��쿡�� �׻� �Ǹ� ����
        if (selectedWeapon != null)
        {
            Vector2Int? itemPosition = FindSelectedItemPosition();
            if (!itemPosition.HasValue) // �׸��� ��ġ�� ������ ���� �������� ���ɼ� ����
            {
                return true;
            }
        }

        return itemCount > 1; // �⺻ ����: �׸��忡 �������� 2�� �̻��� ���� �Ǹ� ����
    }
    private int CountItemsInGrid()
    {
        int count = 0;
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                if (mainItemGrid.GetItem(x, y) != null)
                {
                    count++;
                }
            }
        }
        return count;
    }
    private void SellPhysicsItem()
    {
        SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
        playerStats.AddCoins(selectedWeapon.SellPrice);

        // �������� Equipment�� ��� ȿ�� ����
        if (selectedWeapon.weaponType == WeaponType.Equipment)
        {
            var weaponManager = GameObject.FindWithTag("Player")?.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                weaponManager.UnequipWeapon(selectedWeapon);
            }
        }

        // PhysicsInventoryManager ���� �������� (ĳ�̵� ���� ���)
        PhysicsInventoryManager physicsManager = FindAnyObjectByType<PhysicsInventoryManager>();
        if (physicsManager != null)
        {
            // ���� �巡�� ���� ������ �Ǵ� ���õ� ���� �����Ϳ� ��ġ�ϴ� ������ ã��
            PhysicsInventoryItem physicsItem = physicsManager.GetDraggedPhysicsItem();

            if (physicsItem != null)
            {
                physicsManager.RemovePhysicsItem(physicsItem);
            }
            else
            {
                // ���õ� ���� �����Ϳ� ��ġ�ϴ� ������ ã��
                var physicsItems = physicsManager.GetAllPhysicsItems();
                foreach (var item in physicsItems)
                {
                    if (item != null && item.GetWeaponData() == selectedWeapon)
                    {
                        physicsManager.RemovePhysicsItem(item);
                        break;
                    }
                }
            }
        }

        // UI ���� �ʱ�ȭ
        selectedWeapon = null;
        gameObject.SetActive(false);
    }

    private void OnSellButtonClick()
    {
        if (!CanSellCurrentItem() || selectedWeapon == null) return;

        // ���� ���õ� �������� ��ġ ã��
        Vector2Int? itemPosition = FindSelectedItemPosition();
        
        //������Ģ ������ �Ǹ�
        if (!itemPosition.HasValue)
        {
            SellPhysicsItem();
            return;
        }

        // �Ǹ� ó��
        SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        playerStats.AddCoins(selectedWeapon.SellPrice);

        // �������� Equipment�� ��� ȿ�� ����
        if (selectedWeapon.weaponType == WeaponType.Equipment)
        {
            var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
            weaponManager?.UnequipWeapon(selectedWeapon);
        }

        // �׸��忡�� ������ ����
        InventoryItem item = mainItemGrid.RemoveItem(itemPosition.Value);
        if (item != null)
        {
            Destroy(item.gameObject);
        }

        // UI ���� �ʱ�ȭ
        selectedWeapon = null;
        gameObject.SetActive(false);
    }

    private Vector2Int? FindSelectedItemPosition()
    {
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item != null && item.WeaponData == selectedWeapon)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return null;
    }
    public void RefreshUpgradeUI()
    {
        if (selectedWeapon != null)
        {
            CheckUpgradePossibility();
        }
        else
        {
            // ���õ� ���Ⱑ ���� ���� ���׷��̵� ��ư �����
            upgradeButton.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Private Methods - Initialization
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
        upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
        sellButton.onClick.AddListener(OnSellButtonClick);
        upgradeButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);
        gameObject.SetActive(true);
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
    #endregion

    #region Private Methods - Event Handling
    private void SubscribeToEvents()
    {
        if (mainItemGrid != null && !mainItemGrid.IsInitialized)
        {
            mainItemGrid.ForceInitialize();
        }

        if (mainItemGrid != null)
        {
            // �̹� �����Ǿ� ���� ������ Ȯ�� (�ߺ� ���� ����)
            mainItemGrid.OnGridChanged -= RefreshUpgradeUI;
            mainItemGrid.OnGridChanged += RefreshUpgradeUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }
    private void UnsubscribeFromEvents()
    {
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
        if (sellButton != null) sellButton.onClick.RemoveListener(OnSellButtonClick);
        if (GameManager.Instance != null) GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        if (mainItemGrid != null) mainItemGrid.OnGridChanged -= RefreshUpgradeUI;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (!isInitialized && GameManager.Instance.IsInitialized)
        {
            InitializeReferences();
        }
    }
    #endregion

    #region Private Methods - UI Updates
    private void UpdateBasicInfo(WeaponData weaponData)
    {
        weaponLevelText.text = $"Tier {weaponData.currentTier}";
        weaponNameText.text = weaponData.weaponName;
        weaponDescriptionText.text = weaponData.weaponDescription;
    }

    private void CheckUpgradePossibility()
    {
        if (!ValidateUpgradeRequirements()) return;

        upgradeableWeapons.Clear();
        WeaponType targetType = selectedWeapon.weaponType;
        int targetTier = selectedWeapon.currentTier;

        SearchUpgradeableWeapons(targetType, targetTier, selectedWeapon.equipmentType);
        bool canUpgrade = upgradeableWeapons.Count >= 2 && targetTier < 4;

        upgradeButton.gameObject.SetActive(canUpgrade);
        if (canUpgrade)
        {
            upgradeButtonText.text = $"Upgrade to Tier {targetTier + 1}";
        }
    }
    #endregion

    #region Private Methods - Upgrade Logic
    private void SearchUpgradeableWeapons(WeaponType targetType, int targetTier, EquipmentType targetEquipmentType)
    {
        for (int x = 0; x < mainItemGrid.Width; x++)
        {
            for (int y = 0; y < mainItemGrid.Height; y++)
            {
                InventoryItem item = mainItemGrid.GetItem(x, y);
                if (item == null || item.WeaponData == null) continue;

                bool isMatchingType = (targetType == WeaponType.Equipment) ?
                    item.WeaponData.weaponType == targetType && item.WeaponData.equipmentType == targetEquipmentType :
                    item.WeaponData.weaponType == targetType;

                if (isMatchingType && item.WeaponData.currentTier == targetTier && !upgradeableWeapons.Contains(item))
                {
                    upgradeableWeapons.Add(item);
                }
            }
        }
    }

    private void OnUpgradeButtonClick()
    {
        Debug.Log("Upgrade button clicked");

        if (!ValidateUpgradeOperation())
        {
            Debug.LogWarning("Cannot upgrade: missing requirements");
            return;
        }

        WeaponData nextTierWeapon = selectedWeapon.CreateNextTierWeapon();
        if (nextTierWeapon == null)
        {
            Debug.LogWarning("Failed to create next tier weapon");
            return;
        }

        Debug.Log($"Found {upgradeableWeapons.Count} upgradeable weapons");

        // ���׷��̵� ���� ���� ������� ��ġ ����
        Vector2Int upgradePosition = upgradeableWeapons[0].GridPosition;

        // ���� ���� ����
        RemoveUpgradeMaterials();

        // ���ο� ���� ����
        inventoryController?.CreateUpgradedItem(nextTierWeapon, upgradePosition);

        // ���� ����
        CleanupUpgradeState();
    }

    private void RemoveUpgradeMaterials()
    {
        Debug.Log($"Attempting to remove {upgradeableWeapons.Count} materials");

        if (selectedWeapon.weaponType == WeaponType.Equipment)
        {
            var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                foreach (var weapon in upgradeableWeapons.Take(2))
                {
                    Debug.Log($"Removing equipment effect from weapon at position {weapon.GridPosition}");
                    weaponManager.UnequipWeapon(weapon.GetWeaponData());
                }
            }
        }

        // ���׷��̵忡 ���� 2���� ���⸸ ó��
        var weaponsToRemove = upgradeableWeapons.Take(2).ToList();
        foreach (var weapon in weaponsToRemove)
        {
            if (weapon != null)
            {
                Debug.Log($"Removing weapon from grid at position {weapon.GridPosition}");
                mainItemGrid.RemoveItem(weapon.GridPosition);
                Debug.Log($"Destroying weapon GameObject");
                Destroy(weapon.gameObject);
            }
        }
    }

    #endregion

    #region Private Methods - Validation
    private bool ValidateUpgradeRequirements()
    {
        if (!isInitialized || selectedWeapon == null || mainItemGrid == null || upgradeButton == null)
        {
            upgradeButton.gameObject.SetActive(false);
            return false;
        }
        return true;
    }
    private void CleanupUpgradeState()
    {
        Debug.Log("Cleaning up upgrade state");
        upgradeableWeapons.Clear();
        selectedWeapon = null;
        upgradeButton.gameObject.SetActive(false);
    }
    private bool ValidateUpgradeOperation()
    {
        return isInitialized && upgradeableWeapons != null &&
               upgradeableWeapons.Count >= 2 && selectedWeapon != null &&
               mainItemGrid != null && selectedWeapon.currentTier < 4;
    }

    #endregion
}