using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

/// <summary>
/// X-티어 무기 업그레이드 시스템을 관리하는 클래스
/// 4티어 무기를 X-티어로 업그레이드하는 시스템을 제어합니다.
/// </summary>
public class EnhancedWeaponManager : MonoBehaviour
{
    [Header("Requirements")]
    [SerializeField] private int requiredPlayerLevel = 10;
    [SerializeField] private int levelCost = 10;

    [Header("References")]
    [SerializeField] private EnhancedWeaponUI enhancedWeaponUI;
    [SerializeField] private ScreenTransitionEffect transitionEffect;
    [SerializeField] private ShopController shopController;
    [SerializeField] private WeaponDatabase weaponDatabase;

    // 내부 상태 관리
    private bool isEnhancedWeaponUIActive = false;
    private bool hasShownEnhancedUIThisWave = false;

    // 캐싱된 참조
    private PlayerStats playerStats;
    private ItemGrid inventoryGrid;
    private WeaponManager weaponManager;
    private InventoryController inventoryController;

    // 업그레이드 가능한 무기 목록
    private readonly List<WeaponData> upgradableWeapons = new List<WeaponData>();

    // X-티어 무기 매핑 (4티어 → X-티어)
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
        // 필요한 이벤트 구독
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// 외부 참조 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // 없으면 찾기
        if (enhancedWeaponUI == null)
            enhancedWeaponUI = FindFirstObjectByType<EnhancedWeaponUI>();

        if (transitionEffect == null)
            transitionEffect = FindFirstObjectByType<ScreenTransitionEffect>();

        if (shopController == null)
            shopController = FindFirstObjectByType<ShopController>();

        if (weaponDatabase == null)
            weaponDatabase = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");

        // GameManager로부터 중요 참조 가져오기
        if (GameManager.Instance != null)
        {
            playerStats = GameManager.Instance.PlayerStats;
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            weaponManager = player.GetComponent<WeaponManager>();
        }

        // 인벤토리 컨트롤러 찾기
        inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryGrid = inventoryController.GetComponentInChildren<ItemGrid>();
        }
    }

    /// <summary>
    /// 이벤트 구독 설정
    /// </summary>
    private void SubscribeToEvents()
    {
        // InventoryController의 OnProgressButtonClicked 이벤트에 구독
        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked += CheckForEnhancedWeapons;
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked -= CheckForEnhancedWeapons;
        }
    }

    /// <summary>
    /// 웨이브 완료 후 다음 단계로 진행하기 전에 체크
    /// </summary>
    private void CheckForEnhancedWeapons()
    {
        // X-티어 업그레이드를 이미 이번 웨이브에서 보여줬다면 스킵
        if (hasShownEnhancedUIThisWave) return;

        // 인벤토리에 4티어 무기가 있는지 확인
        CheckForUpgradableWeapons();

        // 업그레이드 가능한 무기가 있고, 플레이어 레벨이 충분하면 UI 표시
        if (upgradableWeapons.Count > 0 && CanPlayerUpgrade())
        {
            ShowEnhancedWeaponUI();
            hasShownEnhancedUIThisWave = true; // 이번 웨이브에서 표시했음을 기록
        }
        else
        {
            // 조건을 만족하지 않으면 상점으로 바로 진행
            ContinueToShop();
        }
    }

    /// <summary>
    /// 인벤토리에서 4티어 무기를 찾아 업그레이드 가능한 무기 목록 갱신
    /// </summary>
    private void CheckForUpgradableWeapons()
    {
        upgradableWeapons.Clear();

        if (inventoryGrid == null || !inventoryGrid.IsInitialized)
        {
            Debug.LogWarning("인벤토리 그리드를 찾을 수 없거나 초기화되지 않았습니다.");
            return;
        }

        // 그리드 내의 모든 아이템 확인
        for (int x = 0; x < inventoryGrid.Width; x++)
        {
            for (int y = 0; y < inventoryGrid.Height; y++)
            {
                InventoryItem item = inventoryGrid.GetItem(x, y);
                if (item != null)
                {
                    WeaponData weaponData = item.GetWeaponData();
                    if (weaponData != null && weaponData.currentTier == 4 && !weaponData.weaponType.Equals(WeaponType.Equipment))
                    {
                        // X-티어 무기 맵에 있는 무기 타입만 추가
                        if (xTierWeaponNames.ContainsKey(weaponData.weaponType))
                        {
                            upgradableWeapons.Add(weaponData);
                        }
                    }
                }
            }
        }

        // 디버그 로깅
        Debug.Log($"업그레이드 가능한 무기 {upgradableWeapons.Count}개 찾음");
    }

    /// <summary>
    /// 플레이어가 업그레이드 가능한 조건을 갖추었는지 확인
    /// </summary>
    private bool CanPlayerUpgrade()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("플레이어 스탯 참조를 찾을 수 없습니다.");
            return false;
        }

        return playerStats.Level >= requiredPlayerLevel;
    }

    /// <summary>
    /// 향상된 무기 UI 표시
    /// </summary>
    private void ShowEnhancedWeaponUI()
    {
        if (enhancedWeaponUI == null)
        {
            Debug.LogError("EnhancedWeaponUI 참조가 없습니다.");
            ContinueToShop();
            return;
        }

        // UI에 업그레이드 가능한 무기 전달 및 표시
        enhancedWeaponUI.SetWeaponsData(upgradableWeapons);
        enhancedWeaponUI.SetPlayerLevel(playerStats.Level);
        enhancedWeaponUI.SetLevelCost(levelCost);
        enhancedWeaponUI.gameObject.SetActive(true);
        isEnhancedWeaponUIActive = true;

        // 게임 일시 정지 상태 설정
        GameManager.Instance.SetGameState(GameState.Paused);
    }

    /// <summary>
    /// X-티어 무기로 업그레이드
    /// </summary>
    public void UpgradeToXTier(WeaponData weaponData)
    {
        if (weaponData == null || !CanPlayerUpgrade())
        {
            Debug.LogWarning("업그레이드 조건이 충족되지 않았습니다.");
            return;
        }

        // 레벨 차감
        playerStats.SubtractLevels(levelCost);

        // 기존 무기 제거
        RemoveOriginalWeapon(weaponData);

        // X-티어 무기 생성
        CreateXTierWeapon(weaponData);

        // 업그레이드 UI 닫기
        CloseEnhancedWeaponUI();

        // 상점으로 진행
        StartCoroutine(DelayedContinueToShop());
    }

    /// <summary>
    /// 원래 무기 제거
    /// </summary>
    private void RemoveOriginalWeapon(WeaponData weaponData)
    {
        if (inventoryGrid == null || weaponData == null) return;

        for (int x = 0; x < inventoryGrid.Width; x++)
        {
            for (int y = 0; y < inventoryGrid.Height; y++)
            {
                InventoryItem item = inventoryGrid.GetItem(x, y);
                if (item != null && item.GetWeaponData() == weaponData)
                {
                    // 먼저 WeaponManager에서 장착 해제
                    if (weaponManager != null)
                    {
                        weaponManager.UnequipWeapon(weaponData);
                    }

                    // 그리드에서 아이템 제거
                    inventoryGrid.RemoveItem(new Vector2Int(x, y));
                    Destroy(item.gameObject);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// X-티어 무기 생성 및 인벤토리에 배치
    /// </summary>
    private void CreateXTierWeapon(WeaponData originalWeapon)
    {
        if (inventoryController == null || originalWeapon == null) return;

        // 원본 무기의 위치 찾기
        Vector2Int? originalPosition = FindWeaponPosition(originalWeapon);
        Vector2Int position = originalPosition ?? new Vector2Int(0, 0); // 기본 위치

        // X-티어 무기 데이터 생성
        WeaponData xTierWeapon = CreateXTierWeaponData(originalWeapon);

        if (xTierWeapon != null)
        {
            // 인벤토리 컨트롤러를 통해 업그레이드된 아이템 생성
            inventoryController.CreateUpgradedItem(xTierWeapon, position);
        }
    }

    /// <summary>
    /// 무기의 그리드 위치 찾기
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
    /// X-티어 무기 데이터 생성
    /// </summary>
    private WeaponData CreateXTierWeaponData(WeaponData originalWeapon)
    {
        if (originalWeapon == null) return null;

        // 원본 무기 복제
        WeaponData xTierWeapon = Instantiate(originalWeapon);

        // X-티어 무기 이름 설정
        if (xTierWeaponNames.TryGetValue(originalWeapon.weaponType, out string xTierName))
        {
            xTierWeapon.weaponName = xTierName;
        }
        else
        {
            xTierWeapon.weaponName = $"X-{originalWeapon.weaponName}";
        }

        // X-티어 무기 설명은 이미 WeaponData에 설정되어 있음
        // weaponDescription 필드를 수정하지 않음 (원본 그대로 사용)

        // 티어 5로 설정 (X-티어)
        xTierWeapon.currentTier = 5;

        return xTierWeapon;
    }

    /// <summary>
    /// X-티어 무기 설명 설정 - 이 메서드는 WeaponData에 이미 설정된 설명을 사용합니다.
    /// </summary>
    private void UpdateXTierDescription(WeaponData weaponData)
    {
        // 기존 무기 설명 사용 - 실제 설명은 WeaponData 내에서 관리
        // 필요한 경우 여기서 설명을 약간 수정할 수 있음
    }

    /// <summary>
    /// 향상된 무기 UI 닫기
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
    /// 프레임 대기 후 상점으로 진행
    /// </summary>
    private IEnumerator DelayedContinueToShop()
    {
        // 1프레임 대기
        yield return null;

        ContinueToShop();
    }

    /// <summary>
    /// 상점으로 진행
    /// </summary>
    private void ContinueToShop()
    {
        if (shopController != null)
        {
            // 트랜지션 효과가 있으면 사용
            if (transitionEffect != null)
            {
                transitionEffect.reverseEffect = false; // 안에서 밖으로 효과
                transitionEffect.gameObject.SetActive(true);
                transitionEffect.PlayTransition(() => {
                    shopController.OpenShop();
                });
            }
            else
            {
                // 트랜지션 없이 바로 상점 열기
                shopController.OpenShop();
            }
        }
        else
        {
            Debug.LogWarning("ShopController 참조를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 새 웨이브 시작 시 상태 초기화
    /// </summary>
    public void ResetWaveState()
    {
        hasShownEnhancedUIThisWave = false;
    }
}