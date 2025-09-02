using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ���� ���� �ɼ� UI ��Ҹ� �����ϴ� Ŭ����
/// ���� X-Ƽ�� ���� ���׷��̵� �ɼ��� ǥ���մϴ�.
/// </summary>
public class EnhancedWeaponOption : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;
    [SerializeField] private TextMeshProUGUI statUpgradesText;
    [SerializeField] private Button selectButton;

    // X-Ƽ�� ���� �̸� ���� (���� Ÿ�� -> X-Ƽ�� �̸�)
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

    // ���� �� ������
    private WeaponData weaponData;
    private EnhancedWeaponUI parentUI;

    private void Awake()
    {
        // ��ư �̺�Ʈ ����
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    /// <summary>
    /// ���� ������ �ʱ�ȭ
    /// </summary>
    public void Initialize(WeaponData data, EnhancedWeaponUI ui)
    {
        weaponData = data;
        parentUI = ui;

        UpdateUI();
    }

    /// <summary>
    /// UI ���� ����
    /// </summary>
    private void UpdateUI()
    {
        if (weaponData == null) return;

        // ���� ������ ����
        if (weaponIcon != null)
        {
            weaponIcon.sprite = weaponData.weaponIcon;
            weaponIcon.color = Color.red; // X-Ƽ�� ���� (������)
        }

        // X-Ƽ�� ���� �̸�
        if (weaponNameText != null)
        {
            string xTierName = GetXTierName(weaponData.weaponType);
            weaponNameText.text = xTierName;
        }

        // ���� ����
        if (weaponDescriptionText != null)
        {
            string description = GetEnhancedDescription(weaponData);
            weaponDescriptionText.text = description;
        }
    }

    /// <summary>
    /// ���� ��ư Ŭ�� �̺�Ʈ ó��
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (parentUI != null && weaponData != null)
        {
            // ���� ȿ�� ���
            SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);

            // �θ� UI�� ���� �˸�
            parentUI.OnWeaponSelected(weaponData);
        }
    }

    /// <summary>
    /// ���� Ÿ�Կ� ���� X-Ƽ�� �̸� ��ȯ
    /// </summary>
    private string GetXTierName(WeaponType weaponType)
    {
        if (xTierWeaponNames.TryGetValue(weaponType, out string name))
        {
            return name;
        }

        // �⺻ �̸� ��ȯ
        return $"X-{weaponData.weaponName}";
    }

    /// <summary>
    /// X-Ƽ�� ���� ���� ǥ��
    /// </summary>
    private string GetEnhancedDescription(WeaponData weaponData)
    {
        // X-Ƽ�� ���� ������ �����Ǿ� �ִ��� Ȯ��
        if (!string.IsNullOrEmpty(weaponData.xTierWeaponDescription))
        {
            return weaponData.xTierWeaponDescription;
        }
        
        // X-Ƽ�� ������ ���� ������ ���� ������ �⺻���� ���
        return $"Enhanced {weaponData.weaponDescription}\n\n[X-Tier Enhancement]\n• Greatly increased damage and performance\n• Special effects and abilities activated";
    }
}