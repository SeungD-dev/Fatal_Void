using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// X-티어 무기 업그레이드 UI를 관리하는 클래스
/// 웨이브 클리어 후 4티어 무기를 X-티어로 업그레이드하는 UI를 제공합니다.
/// </summary>
public class EnhancedWeaponUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Transform weaponOptionContainer;
    [SerializeField] private GameObject weaponOptionPrefab;

    [Header("Enhanced Weapon Panel")]
    [SerializeField] private EnhancedWeaponManager enhancedWeaponManager;

    // 내부 상태
    private List<WeaponData> availableWeapons = new List<WeaponData>();
    private int playerLevel;
    private int levelCost;
    private List<GameObject> instantiatedOptions = new List<GameObject>();

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    /// <summary>
    /// 필요한 컴포넌트 참조 초기화
    /// </summary>
    private void InitializeComponents()
    {
        if (enhancedWeaponManager == null)
        {
            enhancedWeaponManager = FindAnyObjectByType<EnhancedWeaponManager>();
        }

        // 기본 텍스트 설정
        if (titleText != null)
        {
            titleText.text = "X-TIER WEAPON UPGRADE";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "Select one weapon to upgrade to X-Tier. The upgrade will cost player levels.";
        }
    }

    /// <summary>
    /// 업그레이드 가능한 무기 데이터 설정
    /// </summary>
    public void SetWeaponsData(List<WeaponData> weapons)
    {
        availableWeapons = new List<WeaponData>(weapons);
    }

    /// <summary>
    /// 현재 플레이어 레벨 설정
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        playerLevel = level;
    }

    /// <summary>
    /// 레벨 비용 설정
    /// </summary>
    public void SetLevelCost(int cost)
    {
        levelCost = cost;
    }

    /// <summary>
    /// UI 갱신
    /// </summary>
    private void UpdateUI()
    {
        // 레벨 정보 갱신
        if (levelInfoText != null)
        {
            levelInfoText.text = $"Your Level: {playerLevel} / Cost: {levelCost} Levels";
        }

        // 무기 옵션 생성
        CreateWeaponOptions();
    }

    /// <summary>
    /// 무기 옵션 UI 생성
    /// </summary>
    private void CreateWeaponOptions()
    {
        // 기존 옵션 정리
        ClearWeaponOptions();

        if (weaponOptionContainer == null || weaponOptionPrefab == null)
        {
            Debug.LogError("무기 옵션 컨테이너 또는 프리팹이 설정되지 않았습니다.");
            return;
        }

        // 각 무기마다 옵션 UI 생성
        foreach (var weaponData in availableWeapons)
        {
            if (weaponData == null) continue;

            GameObject optionObj = Instantiate(weaponOptionPrefab, weaponOptionContainer);
            instantiatedOptions.Add(optionObj);

            // EnhancedWeaponOption 컴포넌트 가져오기
            EnhancedWeaponOption option = optionObj.GetComponent<EnhancedWeaponOption>();
            if (option != null)
            {
                option.Initialize(weaponData, this);
            }
        }
    }

    /// <summary>
    /// 기존 무기 옵션 정리
    /// </summary>
    private void ClearWeaponOptions()
    {
        foreach (var option in instantiatedOptions)
        {
            if (option != null)
            {
                Destroy(option);
            }
        }

        instantiatedOptions.Clear();
    }

    /// <summary>
    /// 무기 선택 처리
    /// </summary>
    public void OnWeaponSelected(WeaponData weaponData)
    {
        if (weaponData == null || enhancedWeaponManager == null) return;

        // 선택한 무기를 X-티어로 업그레이드
        enhancedWeaponManager.UpgradeToXTier(weaponData);
    }
  
    private void OnDestroy()
    {
        ClearWeaponOptions();
    }
}