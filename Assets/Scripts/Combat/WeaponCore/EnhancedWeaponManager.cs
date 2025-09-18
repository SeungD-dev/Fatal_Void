using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

/// <summary>
/// X-Ƽ�� ���� ���׷��̵� �ý����� �����ϴ� Ŭ����
/// 4Ƽ�� ���⸦ X-Ƽ��� ���׷��̵��ϴ� �ý����� �����մϴ�.
/// </summary>
public class EnhancedWeaponManager : MonoBehaviour
{
    [Header("Requirements")]
    [SerializeField] private int requiredPlayerLevel = 20;
    [SerializeField] private int levelCost = 20;

    [Header("References")]
    [SerializeField] private EnhancedWeaponUI enhancedWeaponUI;
    [SerializeField] private ScreenTransitionEffect transitionEffect;
    [SerializeField] private ShopController shopController;
    [SerializeField] private WeaponDatabase weaponDatabase;

    // ���� ���� ����
    private bool isEnhancedWeaponUIActive = false;
    private bool hasShownEnhancedUIThisWave = false;

    // ĳ�̵� ����
    private PlayerStats playerStats;
    private ItemGrid inventoryGrid;
    private WeaponManager weaponManager;
    private InventoryController inventoryController;
    private WaveManager waveManager;
    private PhysicsInventoryManager physicsInventoryManager;

    // ���׷��̵� ������ ���� ���
    private readonly List<WeaponData> upgradableWeapons = new List<WeaponData>();

    // X-Ƽ�� ���� ���� (4Ƽ�� �� X-Ƽ��)
    private readonly Dictionary<WeaponType, string> xTierWeaponNames = new Dictionary<WeaponType, string>()
    {
        { WeaponType.Buster, "Exterminator" },
        { WeaponType.Machinegun, "Ultrain" },
        { WeaponType.Blade, "Plasma Sword" },
        { WeaponType.Cutter, "Cyclone Edge" },
        { WeaponType.Sawblade, "Infinity Disc" },
        { WeaponType.BeamSaber, "Phantom Saber" },
        { WeaponType.Shotgun, "HellFire" },
        { WeaponType.Grinder, "Black Hole" },
        { WeaponType.ForceFieldGenerator, "Time Turner" }
    };

    private void Awake()
    {
        InitializeReferences();
    }

    private void Start()
    {
        // �ʿ��� �̺�Ʈ ����
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// �ܺ� ���� �ʱ�ȭ
    /// </summary>
    private void InitializeReferences()
    {
        // ������ ã��
        if (enhancedWeaponUI == null)
            enhancedWeaponUI = FindFirstObjectByType<EnhancedWeaponUI>();

        if (transitionEffect == null)
            transitionEffect = FindFirstObjectByType<ScreenTransitionEffect>();

        if (shopController == null)
            shopController = FindFirstObjectByType<ShopController>();

        if (weaponDatabase == null)
            weaponDatabase = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");

        // GameManager�κ��� �߿� ���� ��������
        if (GameManager.Instance != null)
        {
            playerStats = GameManager.Instance.PlayerStats;
        }

        // �÷��̾� ã��
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            weaponManager = player.GetComponent<WeaponManager>();
        }

        // �κ��丮 ��Ʈ�ѷ� ã��
        inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryGrid = inventoryController.GetComponentInChildren<ItemGrid>();
        }

        // ���̺� �Ŵ��� ã��
        waveManager = FindFirstObjectByType<WaveManager>();

        // ���� �κ��丮 �Ŵ��� ã��
        physicsInventoryManager = FindFirstObjectByType<PhysicsInventoryManager>();
    }

    /// <summary>
    /// �̺�Ʈ ���� ����
    /// </summary>
    private void SubscribeToEvents()
    {
        // WaveManager�� OnWaveCompleted �̺�Ʈ�� ����
        if (waveManager != null)
        {
            waveManager.OnWaveCompleted += CheckForEnhancedWeapons;
            waveManager.OnWaveStarted += ResetWaveState;
        }
    }

    /// <summary>
    /// �̺�Ʈ ���� ����
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveCompleted -= CheckForEnhancedWeapons;
            waveManager.OnWaveStarted -= ResetWaveState;
        }
    }

    /// <summary>
    /// ���̺� �Ϸ� �� ���� �ܰ�� �����ϱ� ���� üũ
    /// </summary>
    private void CheckForEnhancedWeapons()
    {
        // X-Ƽ�� ���׷��̵带 �̹� �̹� ���̺꿡�� ������ٸ� ��ŵ
        if (hasShownEnhancedUIThisWave) return;

        // �κ��丮�� 4Ƽ�� ���Ⱑ �ִ��� Ȯ��
        CheckForUpgradableWeapons();

        // ���׷��̵� ������ ���Ⱑ �ְ�, �÷��̾� ������ ����ϸ� UI ǥ��
        if (upgradableWeapons.Count > 0 && CanPlayerUpgrade())
        {
            ShowEnhancedWeaponUI();
            hasShownEnhancedUIThisWave = true; // �̹� ���̺꿡�� ǥ�������� ���
        }
        else
        {
            // ������ �������� ������ �������� �ٷ� ����
            ContinueToShop();
        }
    }

    /// <summary>
    /// �κ��丮 �׸����� ���� �κ��丮���� 4Ƽ�� ���⸦ ã�� ���׷��̵� ������ ���� ��� ����
    /// </summary>
    private void CheckForUpgradableWeapons()
    {
        upgradableWeapons.Clear();
        List<WeaponData> allTier4Weapons = new List<WeaponData>();

        // 1. �κ��丮 �׸��忡�� 4Ƽ�� ���� ã��
        if (inventoryGrid != null && inventoryGrid.IsInitialized)
        {
            for (int x = 0; x < inventoryGrid.Width; x++)
            {
                for (int y = 0; y < inventoryGrid.Height; y++)
                {
                    InventoryItem item = inventoryGrid.GetItem(x, y);
                    if (item != null)
                    {
                        WeaponData weaponData = item.GetWeaponData();
                        if (IsUpgradableWeapon(weaponData))
                        {
                            allTier4Weapons.Add(weaponData);
                        }
                    }
                }
            }
        }

        // 2. ���� �κ��丮�� 4Ƽ�� ���� ã��
        if (physicsInventoryManager != null)
        {
            var physicsItems = physicsInventoryManager.GetAllPhysicsItems();
            foreach (var physicsItem in physicsItems)
            {
                if (physicsItem != null)
                {
                    InventoryItem inventoryItem = physicsItem.GetComponent<InventoryItem>();
                    if (inventoryItem != null)
                    {
                        WeaponData weaponData = inventoryItem.GetWeaponData();
                        if (IsUpgradableWeapon(weaponData))
                        {
                            allTier4Weapons.Add(weaponData);
                        }
                    }
                }
            }
        }

        // 3. �ִ� 3���� ����, ���� ���� ���ۻ���
        if (allTier4Weapons.Count > 3)
        {
            // ���� ����
            for (int i = 0; i < allTier4Weapons.Count; i++)
            {
                int randomIndex = Random.Range(i, allTier4Weapons.Count);
                WeaponData temp = allTier4Weapons[i];
                allTier4Weapons[i] = allTier4Weapons[randomIndex];
                allTier4Weapons[randomIndex] = temp;
            }

            // óǪ 3�� ���� ����
            upgradableWeapons.AddRange(allTier4Weapons.Take(3));
        }
        else
        {
            upgradableWeapons.AddRange(allTier4Weapons);
        }

        // ����� �α�
        Debug.Log($"���׷��̵� ������ ���� {upgradableWeapons.Count}�� ã�� (��ü {allTier4Weapons.Count}�� ��)");
    }

    /// <summary>
    /// ���⸦ ���׷��̵� ������ ���߾����� Ȯ��
    /// </summary>
    private bool IsUpgradableWeapon(WeaponData weaponData)
    {
        return weaponData != null &&
               weaponData.currentTier == 4 &&
               weaponData.weaponType != WeaponType.Equipment &&
               weaponData.supportsXTier &&
               xTierWeaponNames.ContainsKey(weaponData.weaponType);
    }

    /// <summary>
    /// �÷��̾ ���׷��̵� ������ ������ ���߾����� Ȯ��
    /// </summary>
    private bool CanPlayerUpgrade()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("�÷��̾� ���� ������ ã�� �� �����ϴ�.");
            return false;
        }

        return playerStats.Level >= requiredPlayerLevel;
    }

    /// <summary>
    /// ���� ���� UI ǥ��
    /// </summary>
    private void ShowEnhancedWeaponUI()
    {
        if (enhancedWeaponUI == null)
        {
            Debug.LogError("EnhancedWeaponUI ������ �����ϴ�.");
            ContinueToShop();
            return;
        }

        // UI�� ���׷��̵� ������ ���� ���� �� ǥ��
        enhancedWeaponUI.SetWeaponsData(upgradableWeapons);
        enhancedWeaponUI.SetPlayerLevel(playerStats.Level);
        enhancedWeaponUI.SetLevelCost(levelCost);
        enhancedWeaponUI.gameObject.SetActive(true);
        isEnhancedWeaponUIActive = true;

        // ���� �Ͻ� ���� ���� ����
        GameManager.Instance.SetGameState(GameState.Paused);
    }

    /// <summary>
    /// X-Ƽ�� ����� ���׷��̵�
    /// </summary>
    public void UpgradeToXTier(WeaponData weaponData)
    {
        if (weaponData == null || !CanPlayerUpgrade())
        {
            Debug.LogWarning("���׷��̵� ������ �������� �ʾҽ��ϴ�.");
            return;
        }

        // ���� ����
        playerStats.SubtractLevels(levelCost);

        // ���� ���� ����
        RemoveOriginalWeapon(weaponData);

        // X-Ƽ�� ���� ����
        CreateXTierWeapon(weaponData);

        // ���׷��̵� UI �ݱ�
        CloseEnhancedWeaponUI();

        // �������� ����
        StartCoroutine(DelayedContinueToShop());
    }

    /// <summary>
    /// ���� ���� ���� (�κ��丮 �׸��� + ���� �κ��丮)
    /// </summary>
    private void RemoveOriginalWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;

        // WeaponManager���� ���� ���� ����
        if (weaponManager != null)
        {
            weaponManager.UnequipWeapon(weaponData);
        }

        // 1. �κ��丮 �׸��忡�� ã��
        if (inventoryGrid != null && inventoryGrid.IsInitialized)
        {
            for (int x = 0; x < inventoryGrid.Width; x++)
            {
                for (int y = 0; y < inventoryGrid.Height; y++)
                {
                    InventoryItem item = inventoryGrid.GetItem(x, y);
                    if (item != null && item.GetWeaponData() == weaponData)
                    {
                        inventoryGrid.RemoveItem(new Vector2Int(x, y));
                        Destroy(item.gameObject);
                        return;
                    }
                }
            }
        }

        // 2. ���� �κ��丮���� ã��
        if (physicsInventoryManager != null)
        {
            var physicsItems = physicsInventoryManager.GetAllPhysicsItems();
            foreach (var physicsItem in physicsItems)
            {
                if (physicsItem != null)
                {
                    InventoryItem inventoryItem = physicsItem.GetComponent<InventoryItem>();
                    if (inventoryItem != null && inventoryItem.GetWeaponData() == weaponData)
                    {
                        physicsInventoryManager.RemovePhysicsItem(physicsItem);
                        return;
                    }
                }
            }
        }

        Debug.LogWarning($"���׷��̵� ���� ���⸦ ã�� �� �����ϴ�: {weaponData.weaponName}");
    }

    /// <summary>
    /// X-Ƽ�� ���� ���� �� �κ��丮 �Ǵ� ���� ��ġ�� ��ġ
    /// </summary>
    private void CreateXTierWeapon(WeaponData originalWeapon)
    {
        if (originalWeapon == null) return;

        // X-Ƽ�� ���� ������ ����
        WeaponData xTierWeapon = CreateXTierWeaponData(originalWeapon);
        if (xTierWeapon == null) return;

        // ���� ������ ��ġ ã�� (�׸��� ���� ���)
        Vector2Int? originalPosition = FindWeaponPosition(originalWeapon);

        // �κ��丮�� ���� ��ġ �õ�
        if (inventoryController != null && originalPosition.HasValue)
        {
            // �׸��忡 ���� ��ġ�� �õ�
            inventoryController.CreateUpgradedItem(xTierWeapon, originalPosition.Value);
        }
        else if (physicsInventoryManager != null)
        {
            // �κ��丮�� ���� ���� ���� �κ��丮�� ����
            physicsInventoryManager.HandleFullInventory(xTierWeapon);
        }
        else
        {
            Debug.LogError("X-Tier ���⸦ ��ġ�� �� �����ϴ�. InventoryController �Ǵ� PhysicsInventoryManager�� �����ϴ�.");
        }
    }

    /// <summary>
    /// ������ �׸��� ��ġ ã��
    /// </summary>
    private Vector2Int? FindWeaponPosition(WeaponData weaponData)
    {
        if (inventoryGrid == null || weaponData == null) return null;

        for (int x = 0; x < inventoryGrid.Width; x++)
        {
            for (int y = 0; y < inventoryGrid.Height; y++)
            {
                InventoryItem item = inventoryGrid.GetItem(x, y);
                if (item != null && item.GetWeaponData() == weaponData)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// X-Ƽ�� ���� ������ ����
    /// </summary>
    private WeaponData CreateXTierWeaponData(WeaponData originalWeapon)
    {
        if (originalWeapon == null) return null;

        // ���� ���� ����
        WeaponData xTierWeapon = Instantiate(originalWeapon);

        // X-Ƽ�� ���� �̸� ����
        if (xTierWeaponNames.TryGetValue(originalWeapon.weaponType, out string xTierName))
        {
            xTierWeapon.weaponName = xTierName;
        }
        else
        {
            xTierWeapon.weaponName = $"X-{originalWeapon.weaponName}";
        }

        // X-Ƽ�� ���� ������ �̹� WeaponData�� �����Ǿ� ����
        // weaponDescription �ʵ带 �������� ���� (���� �״�� ���)

        // Ƽ�� 5�� ���� (X-Ƽ��)
        xTierWeapon.currentTier = 5;

        return xTierWeapon;
    }

    /// <summary>
    /// X-Ƽ�� ���� ���� ���� - �� �޼���� WeaponData�� �̹� ������ ������ ����մϴ�.
    /// </summary>
    private void UpdateXTierDescription(WeaponData weaponData)
    {
        // ���� ���� ���� ��� - ���� ������ WeaponData ������ ����
        // �ʿ��� ��� ���⼭ ������ �ణ ������ �� ����
    }

    /// <summary>
    /// ���� ���� UI �ݱ�
    /// </summary>
    public void CloseEnhancedWeaponUI()
    {
        if (enhancedWeaponUI != null)
        {
            enhancedWeaponUI.gameObject.SetActive(false);
        }

        isEnhancedWeaponUIActive = false;
    }

    /// <summary>
    /// ������ ��� �� �������� ����
    /// </summary>
    private IEnumerator DelayedContinueToShop()
    {
        // 1������ ���
        yield return null;

        ContinueToShop();
    }

    /// <summary>
    /// �������� ����
    /// </summary>
    private void ContinueToShop()
    {
        if (waveManager != null)
        {
            // WaveManager�� ����/��� ǥ�� �� ���� ���� ����
            waveManager.ShowBannerAndOpenShop();
        }
        else
        {
            Debug.LogWarning("WaveManager ������ ã�� �� �����ϴ�.");
        }
    }

    /// <summary>
    /// �� ���̺� ���� �� ���� �ʱ�ȭ
    /// </summary>
    public void ResetWaveState()
    {
        hasShownEnhancedUIThisWave = false;
    }
}