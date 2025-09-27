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

                // 버튼 상태 복구 (이전에 비활성화된 버튼들을 다시 활성화)
                Button optionButton = weaponOptions[i].GetComponentInChildren<Button>();
                if (optionButton != null)
                {
                    optionButton.interactable = true;
                }
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

        // 실시간으로 인벤토리에서 최신 무기 데이터 참조 찾기 (참조 불일치 문제 해결)
        WeaponData currentWeaponData = enhancedWeaponManager.FindMatchingWeaponInInventory(weaponData);

        if (currentWeaponData == null)
        {
            Debug.LogWarning($"선택한 무기를 인벤토리에서 찾을 수 없습니다: {weaponData.weaponName}");
            // UI를 닫고 상점으로 이동
            CloseUIAndContinueToShop();
            return;
        }

        // ��� ���� �ɼ� ��Ȱ��ȭ
        DisableAllWeaponOptions();

        // 최신 참조로 업그레이드 진행
        enhancedWeaponManager.UpgradeToXTier(currentWeaponData);
    }

    /// <summary>
    /// ��� ���� �ɼ� ��Ȱ��ȭ
    /// </summary>
    private void DisableAllWeaponOptions()
    {
        if (weaponOptions == null) return;

        foreach (var option in weaponOptions)
        {
            if (option != null && option.gameObject.activeInHierarchy)
            {
                Button optionButton = option.GetComponentInChildren<Button>();
                if (optionButton != null)
                {
                    optionButton.interactable = false;
                }
            }
        }
    }
  
    /// <summary>
    /// UI를 닫고 상점으로 이동 (무기를 찾을 수 없는 경우)
    /// </summary>
    private void CloseUIAndContinueToShop()
    {
        if (enhancedWeaponManager != null)
        {
            enhancedWeaponManager.CloseEnhancedWeaponUI();
            // 직접 상점으로 이동하는 대신 EnhancedWeaponManager를 통해 처리
            enhancedWeaponManager.SendMessage("ContinueToShop", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnDestroy()
    {
        HideAllWeaponOptions();
    }
}