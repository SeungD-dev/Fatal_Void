using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// X-Tier 무기 디버깅을 위한 패널 스크립트
/// 각 X-Tier 무기 버튼을 클릭하면 인벤토리에 해당 무기가 추가됨
/// </summary>
public class XTierWeaponDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private Button closeButton;

    [Header("X-Tier Weapon Buttons")]
    [SerializeField] private Button exterminatorButton;      // Exterminator (Buster X-Tier)
    [SerializeField] private Button ultrainButton;           // Ultrain (Machinegun X-Tier)
    [SerializeField] private Button plasmaSwordButton;       // Plasma Sword (Blade X-Tier)
    [SerializeField] private Button cycloneEdgeButton;       // Cyclone Edge (Cutter X-Tier)
    [SerializeField] private Button infinityDiscButton;      // Infinity Disc (Sawblade X-Tier)
    [SerializeField] private Button phantomSaberButton;      // Phantom Saber (BeamSaber X-Tier)
    [SerializeField] private Button hellFireButton;          // HellFire (Shotgun X-Tier)
    [SerializeField] private Button blackHoleButton;         // Black Hole (Grinder X-Tier)
    [SerializeField] private Button timeTurnerButton;        // Time Turner (ForceFieldGenerator X-Tier)

    private SoundManager soundManager;
    private Dictionary<WeaponType, WeaponData> xTierWeapons = new Dictionary<WeaponType, WeaponData>();

    private void Awake()
    {
        soundManager = SoundManager.Instance;
        FindReferences();
        LoadXTierWeapons();
        SetupButtonListeners();
    }

    /// <summary>
    /// 필요한 레퍼런스들을 자동으로 찾기
    /// </summary>
    private void FindReferences()
    {
        if (weaponDatabase == null)
        {
            weaponDatabase = Resources.FindObjectsOfTypeAll<WeaponDatabase>().FirstOrDefault();
            if (weaponDatabase == null)
            {
                Debug.LogError("XTierWeaponDebugPanel: WeaponDatabase를 찾을 수 없습니다!");
                return;
            }
        }

        if (inventoryController == null)
        {
            inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController == null)
            {
                Debug.LogError("XTierWeaponDebugPanel: InventoryController를 찾을 수 없습니다!");
                return;
            }
        }
    }

    /// <summary>
    /// X-Tier를 지원하는 모든 무기 목록 로드
    /// </summary>
    private void LoadXTierWeapons()
    {
        if (weaponDatabase == null)
        {
            Debug.LogError("WeaponDatabase가 없어 X-Tier 무기를 로드할 수 없습니다!");
            return;
        }

        xTierWeapons.Clear();

        var allWeapons = weaponDatabase.weapons;
        if (allWeapons == null || allWeapons.Count == 0)
        {
            Debug.LogWarning("WeaponDatabase에 무기가 없습니다!");
            return;
        }

        foreach (var weapon in allWeapons)
        {
            if (weapon != null && weapon.supportsXTier)
            {
                // X-Tier 버전 생성
                WeaponData xTierWeapon = Instantiate(weapon);
                xTierWeapon.currentTier = 5; // X-Tier 설정
                xTierWeapon.name = weapon.name + "_XTier_Debug";
                
                // 무기 타입별로 저장
                if (!xTierWeapons.ContainsKey(weapon.weaponType))
                {
                    xTierWeapons[weapon.weaponType] = xTierWeapon;
                }
            }
        }

        Debug.Log($"X-Tier 무기 {xTierWeapons.Count}개를 로드했습니다.");
    }

    /// <summary>
    /// 버튼 리스너들 설정
    /// </summary>
    private void SetupButtonListeners()
    {
        // 닫기 버튼
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // X-Tier 무기 버튼들
        if (exterminatorButton != null)
        {
            exterminatorButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Buster));
        }

        if (ultrainButton != null)
        {
            ultrainButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Machinegun));
        }

        if (plasmaSwordButton != null)
        {
            plasmaSwordButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Blade));
        }

        if (cycloneEdgeButton != null)
        {
            cycloneEdgeButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Cutter));
        }

        if (infinityDiscButton != null)
        {
            infinityDiscButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Sawblade));
        }

        if (phantomSaberButton != null)
        {
            phantomSaberButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.BeamSaber));
        }

        if (hellFireButton != null)
        {
            hellFireButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Shotgun));
        }

        if (blackHoleButton != null)
        {
            blackHoleButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Grinder));
        }

        if (timeTurnerButton != null)
        {
            timeTurnerButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.ForceFieldGenerator));
        }
    }

    /// <summary>
    /// 닫기 버튼 클릭 처리
    /// </summary>
    private void OnCloseButtonClicked()
    {
        PlayButtonSound();
        
        // OptionController의 CloseXTierDebugPanel 호출
        var optionController = FindObjectOfType<OptionController>();
        if (optionController != null)
        {
            optionController.CloseXTierDebugPanel();
        }
        else
        {
            // 백업: 직접 패널 비활성화
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// X-Tier 무기 버튼 클릭 처리
    /// </summary>
    /// <param name="weaponType">무기 타입</param>
    private void OnWeaponButtonClicked(WeaponType weaponType)
    {
        PlayButtonSound();

        if (!xTierWeapons.ContainsKey(weaponType))
        {
            Debug.LogError($"X-Tier 무기를 찾을 수 없습니다: {weaponType}");
            return;
        }

        if (inventoryController == null)
        {
            Debug.LogError("InventoryController가 없어 무기를 추가할 수 없습니다!");
            return;
        }

        try
        {
            WeaponData xTierWeapon = xTierWeapons[weaponType];
            
            // InventoryController의 AddWeaponForDebug 메서드를 통해 인벤토리에 추가
            inventoryController.AddWeaponForDebug(xTierWeapon);
            string displayName = !string.IsNullOrEmpty(xTierWeapon.xTierWeaponName) ? xTierWeapon.xTierWeaponName : xTierWeapon.weaponName;
            Debug.Log($"X-Tier 무기를 인벤토리에 추가했습니다: {displayName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"무기 추가 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    private void PlayButtonSound()
    {
        if (soundManager?.currentSoundBank != null)
        {
            soundManager.PlaySound("Button_sfx", 0f, false);
        }
    }

    /// <summary>
    /// 패널이 활성화될 때마다 X-Tier 무기 목록 새로고침
    /// </summary>
    private void OnEnable()
    {
        LoadXTierWeapons();
    }

    private void OnDestroy()
    {
        // 리스너 정리
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        if (exterminatorButton != null) exterminatorButton.onClick.RemoveAllListeners();
        if (ultrainButton != null) ultrainButton.onClick.RemoveAllListeners();
        if (plasmaSwordButton != null) plasmaSwordButton.onClick.RemoveAllListeners();
        if (cycloneEdgeButton != null) cycloneEdgeButton.onClick.RemoveAllListeners();
        if (infinityDiscButton != null) infinityDiscButton.onClick.RemoveAllListeners();
        if (phantomSaberButton != null) phantomSaberButton.onClick.RemoveAllListeners();
        if (hellFireButton != null) hellFireButton.onClick.RemoveAllListeners();
        if (blackHoleButton != null) blackHoleButton.onClick.RemoveAllListeners();
        if (timeTurnerButton != null) timeTurnerButton.onClick.RemoveAllListeners();
    }
}
#endif