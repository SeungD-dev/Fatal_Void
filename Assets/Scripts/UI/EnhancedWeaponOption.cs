using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 향상된 무기 옵션 UI 요소를 관리하는 클래스
/// 개별 X-티어 무기 업그레이드 옵션을 표시합니다.
/// </summary>
public class EnhancedWeaponOption : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI originalNameText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;
    [SerializeField] private TextMeshProUGUI statUpgradesText;
    [SerializeField] private Button selectButton;

    // X-티어 무기 이름 매핑 (무기 타입 -> X-티어 이름)
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

    // 참조 및 데이터
    private WeaponData weaponData;
    private EnhancedWeaponUI parentUI;

    private void Awake()
    {
        // 버튼 이벤트 설정
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    /// <summary>
    /// 무기 데이터 초기화
    /// </summary>
    public void Initialize(WeaponData data, EnhancedWeaponUI ui)
    {
        weaponData = data;
        parentUI = ui;

        UpdateUI();
    }

    /// <summary>
    /// UI 정보 갱신
    /// </summary>
    private void UpdateUI()
    {
        if (weaponData == null) return;

        // 무기 아이콘 설정
        if (weaponIcon != null)
        {
            weaponIcon.sprite = weaponData.weaponIcon;
            weaponIcon.color = Color.red; // X-티어 색상 (빨간색)
        }

        // 원래 무기 이름
        if (originalNameText != null)
        {
            originalNameText.text = $"From: {weaponData.weaponName}";
        }

        // X-티어 무기 이름
        if (weaponNameText != null)
        {
            string xTierName = GetXTierName(weaponData.weaponType);
            weaponNameText.text = xTierName;
        }

        // 무기 설명
        if (weaponDescriptionText != null)
        {
            string description = GetEnhancedDescription(weaponData);
            weaponDescriptionText.text = description;
        }
    }

    /// <summary>
    /// 선택 버튼 클릭 이벤트 처리
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (parentUI != null && weaponData != null)
        {
            // 사운드 효과 재생
            SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);

            // 부모 UI에 선택 알림
            parentUI.OnWeaponSelected(weaponData);
        }
    }

    /// <summary>
    /// 무기 타입에 따른 X-티어 이름 반환
    /// </summary>
    private string GetXTierName(WeaponType weaponType)
    {
        if (xTierWeaponNames.TryGetValue(weaponType, out string name))
        {
            return name;
        }

        // 기본 이름 반환
        return $"X-{weaponData.weaponName}";
    }

    /// <summary>
    /// X-티어 무기 설명 표시
    /// </summary>
    private string GetEnhancedDescription(WeaponData weaponData)
    {
        // WeaponData에서 직접 가져오기
        return weaponData.weaponDescription;
    }
}