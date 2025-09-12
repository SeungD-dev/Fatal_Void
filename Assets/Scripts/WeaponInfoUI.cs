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
    [SerializeField] private PhysicsInventoryManager physicsInventoryManager;
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

        // 장착된 무기 효과 제거 (모든 무기 타입에 대해)
        var weaponManager = GameObject.FindWithTag("Player")?.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.UnequipWeapon(selectedWeapon);
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

        // 장착된 무기 효과 제거 (모든 무기 타입에 대해)
        var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
        weaponManager?.UnequipWeapon(selectedWeapon);

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
        
        // PhysicsInventoryManager 자동 찾기
        if (physicsInventoryManager == null)
        {
            physicsInventoryManager = FindAnyObjectByType<PhysicsInventoryManager>();
        }
        
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
        // Tier 5 (X-Tier) 특별 처리
        if (weaponData.currentTier == 5)
        {
            weaponLevelText.text = "Tier X";
            
            // X-Tier 전용 이름이 설정되어 있으면 사용, 없으면 기본 이름 정리
            if (!string.IsNullOrEmpty(weaponData.xTierWeaponName))
            {
                weaponNameText.text = weaponData.xTierWeaponName;
            }
            else
            {
                weaponNameText.text = GetCleanWeaponName(weaponData.weaponName);
            }
            
            // X-Tier 전용 설명이 설정되어 있으면 사용, 없으면 기본 설명 사용
            if (!string.IsNullOrEmpty(weaponData.xTierWeaponDescription))
            {
                weaponDescriptionText.text = weaponData.xTierWeaponDescription;
            }
            else
            {
                weaponDescriptionText.text = weaponData.weaponDescription;
            }
        }
        else
        {
            // 일반 Tier 처리 (Tier 1-4)
            weaponLevelText.text = $"Tier {weaponData.currentTier}";
            weaponNameText.text = GetCleanWeaponName(weaponData.weaponName);
            weaponDescriptionText.text = weaponData.weaponDescription;
        }
    }

    private string GetCleanWeaponName(string weaponName)
    {
        // "Tier X" 패턴을 제거하여 깔끔한 무기 이름만 반환
        if (string.IsNullOrEmpty(weaponName))
            return weaponName;
            
        // "Tier X" 또는 " Tier X" 패턴을 찾아서 제거
        System.Text.RegularExpressions.Regex tierPattern = new System.Text.RegularExpressions.Regex(@"\s*Tier\s*\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        string cleanName = tierPattern.Replace(weaponName, "").Trim();
        
        // 결과가 비어있지 않다면 반환, 비어있다면 원본 반환
        return string.IsNullOrEmpty(cleanName) ? weaponName : cleanName;
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
        // 1. 인벤토리 격자에서 검색
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

        // 2. 물리 아이템에서 검색
        if (physicsInventoryManager != null)
        {
            var physicsItems = physicsInventoryManager.GetAllPhysicsItems();
            foreach (var physicsItem in physicsItems)
            {
                if (physicsItem == null) continue;
                
                InventoryItem inventoryItem = physicsItem.GetComponent<InventoryItem>();
                if (inventoryItem == null || inventoryItem.WeaponData == null) continue;

                bool isMatchingType = (targetType == WeaponType.Equipment) ?
                    inventoryItem.WeaponData.weaponType == targetType && inventoryItem.WeaponData.equipmentType == targetEquipmentType :
                    inventoryItem.WeaponData.weaponType == targetType;

                if (isMatchingType && inventoryItem.WeaponData.currentTier == targetTier && !upgradeableWeapons.Contains(inventoryItem))
                {
                    upgradeableWeapons.Add(inventoryItem);
                }
            }
        }
    }

    private void OnUpgradeButtonClick()
    {
        Debug.Log("Upgrade button clicked");

        // 업그레이드 실행 전 최신 상태로 다시 검색
        if (selectedWeapon != null)
        {
            upgradeableWeapons.Clear();
            WeaponType targetType = selectedWeapon.weaponType;
            int targetTier = selectedWeapon.currentTier;
            SearchUpgradeableWeapons(targetType, targetTier, selectedWeapon.equipmentType);
        }

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

        // 업그레이드된 무기 배치 위치 결정 (인벤토리 격자 아이템 우선)
        Vector2Int upgradePosition = Vector2Int.zero;
        bool hasValidPosition = false;
        
        foreach (var weapon in upgradeableWeapons)
        {
            PhysicsInventoryItem physicsItem = weapon.GetComponent<PhysicsInventoryItem>();
            if (physicsItem == null || !physicsItem.IsPhysicsActive)
            {
                // 인벤토리 격자 아이템인 경우 해당 위치 사용
                upgradePosition = weapon.GridPosition;
                hasValidPosition = true;
                break;
            }
        }
        
        // 모두 물리 아이템인 경우 기본 위치 사용
        if (!hasValidPosition)
        {
            upgradePosition = Vector2Int.zero;
        }

        // 기존 무기 제거
        RemoveUpgradeMaterials();

        // 새로운 무기 생성
        inventoryController?.CreateUpgradedItem(nextTierWeapon, upgradePosition);

        // ���� ����
        CleanupUpgradeState();
    }

    private void RemoveUpgradeMaterials()
    {
        Debug.Log($"Attempting to remove {upgradeableWeapons.Count} materials");

        // 장착된 무기들의 효과 제거 (모든 무기 타입에 대해)
        var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            foreach (var weapon in upgradeableWeapons.Take(2))
            {
                Debug.Log($"Removing weapon effect from weapon at position {weapon.GridPosition}");
                weaponManager.UnequipWeapon(weapon.GetWeaponData());
            }
        }

        // 업그레이드에 사용될 2개의 무기만 처리
        var weaponsToRemove = upgradeableWeapons.Take(2).ToList();
        foreach (var weapon in weaponsToRemove)
        {
            if (weapon != null)
            {
                // 물리 아이템인지 확인
                PhysicsInventoryItem physicsItem = weapon.GetComponent<PhysicsInventoryItem>();
                
                if (physicsItem != null && physicsItem.IsPhysicsActive)
                {
                    // 물리 아이템인 경우
                    Debug.Log($"Removing physics item: {weapon.name}");
                    if (physicsInventoryManager != null)
                    {
                        physicsInventoryManager.RemovePhysicsItem(physicsItem);
                    }
                    else
                    {
                        Destroy(weapon.gameObject);
                    }
                }
                else
                {
                    // 인벤토리 격자 아이템인 경우
                    Debug.Log($"Removing weapon from grid at position {weapon.GridPosition}");
                    mainItemGrid.RemoveItem(weapon.GridPosition);
                    Destroy(weapon.gameObject);
                }
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