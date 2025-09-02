using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// X-Ƽ�� ���� ���׷��̵� UI�� �����ϴ� Ŭ����
/// ���̺� Ŭ���� �� 4Ƽ�� ���⸦ X-Ƽ��� ���׷��̵��ϴ� UI�� �����մϴ�.
/// </summary>
public class EnhancedWeaponUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelInfoText;
    [SerializeField] private EnhancedWeaponOption[] weaponOptions; // �̸� ������ ���� �ɼ��� ���

    [Header("Enhanced Weapon Panel")]
    [SerializeField] private EnhancedWeaponManager enhancedWeaponManager;

    // ���� ����
    private List<WeaponData> availableWeapons = new List<WeaponData>();
    private int playerLevel;
    private int levelCost;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    /// <summary>
    /// �ʿ��� ������Ʈ ���� �ʱ�ȭ
    /// </summary>
    private void InitializeComponents()
    {
        if (enhancedWeaponManager == null)
        {
            enhancedWeaponManager = FindAnyObjectByType<EnhancedWeaponManager>();
        }

        // �⺻ �ؽ�Ʈ ����
        if (titleText != null)
        {
            titleText.text = "X-TIER WEAPON UPGRADE";
        }

        // ���� �ɼ��� �⺻ ��Ȱ��ȭ ����
        HideAllWeaponOptions();
    }

    /// <summary>
    /// ���׷��̵� ������ ���� ������ ����
    /// </summary>
    public void SetWeaponsData(List<WeaponData> weapons)
    {
        availableWeapons = new List<WeaponData>(weapons);
    }

    /// <summary>
    /// ���� �÷��̾� ���� ����
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        playerLevel = level;
    }

    /// <summary>
    /// ���� ��� ����
    /// </summary>
    public void SetLevelCost(int cost)
    {
        levelCost = cost;
    }

    /// <summary>
    /// UI ����
    /// </summary>
    private void UpdateUI()
    {
        // ���� ���� ����
        if (levelInfoText != null)
        {
            levelInfoText.text = $"Your Level: {playerLevel} / Cost: {levelCost} Levels";
        }

        // ���� �ɼ� ����
        UpdateWeaponOptions();
    }

    /// <summary>
    /// ���� �ɼ� UI ����
    /// </summary>
    private void UpdateWeaponOptions()
    {
        // ��� �ɼ� ��Ȱ��ȭ
        HideAllWeaponOptions();

        // ������ ���� ������ ���� ǥ��
        for (int i = 0; i < availableWeapons.Count && i < weaponOptions.Length; i++)
        {
            if (availableWeapons[i] != null && weaponOptions[i] != null)
            {
                weaponOptions[i].gameObject.SetActive(true);
                weaponOptions[i].Initialize(availableWeapons[i], this);
            }
        }
    }

    /// <summary>
    /// ��� ���� �ɼ� ����
    /// </summary>
    private void HideAllWeaponOptions()
    {
        if (weaponOptions == null) return;

        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ���� ���� ó��
    /// </summary>
    public void OnWeaponSelected(WeaponData weaponData)
    {
        if (weaponData == null || enhancedWeaponManager == null) return;

        // ������ ���⸦ X-Ƽ��� ���׷��̵�
        enhancedWeaponManager.UpgradeToXTier(weaponData);
    }
  
    private void OnDestroy()
    {
        HideAllWeaponOptions();
    }
}