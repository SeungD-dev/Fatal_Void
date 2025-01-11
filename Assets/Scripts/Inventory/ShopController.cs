using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("Refresh Settings")]
    [SerializeField] private int initialRefreshCost = 5;
    [SerializeField] private int refreshCostIncrease = 1;
    [SerializeField] private Button refreshButton;
    private int currentRefreshCost;

    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponOptionUI[] weaponOptions;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [Header("UI Texts")]
    [SerializeField] private TMPro.TextMeshProUGUI refreshCostText;
    [SerializeField] private TMPro.TextMeshProUGUI playerCoinsText;

    public bool isFirstShop = true;
    private bool hasFirstPurchase = false;
    private PlayerStats playerStats;
    private HashSet<WeaponData> purchasedWeapons = new HashSet<WeaponData>();

    private void Start()
    {
        // 리프레시 버튼 이벤트 설정
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshShop);
        }
        else
        {
            Debug.LogError("Refresh button reference is missing!");
        }
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshShop);
        }

        if (playerStats != null)
        {
            playerStats.OnCoinChanged -= UpdatePlayerCoinsText;
        }
    }

    private void UpdatePlayerCoinsText(int coins)
    {
        if (playerCoinsText != null)
        {
            playerCoinsText.text = $"{coins}";
        }
    }

    private void UpdateRefreshCostText()
    {
        if (refreshCostText != null)
        {
            refreshCostText.text = $"Reroll\n{currentRefreshCost}";
        }
    }

    // UI 전환 메서드
    public void OpenInventory()
    {
        SoundManager.Instance.PlaySound("Button_sfx", 1f, false);

        // 상점 상태 초기화
        if (isFirstShop && hasFirstPurchase)
        {
            isFirstShop = false;
        }

        // UI 전환
        shopUI.SetActive(false);

        if (inventoryController != null)
        {
            inventoryController.OpenInventory();
        }
    }

    public void OpenShop()
    {
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);
        inventoryUI.SetActive(false);

        // 첫 상점이 아닐 경우에만 새로운 무기 옵션을 생성
        if (!isFirstShop)
        {
            InitializeNewWeapons();
        }
        // 첫 상점이면서 아직 구매하지 않은 경우
        else if (!hasFirstPurchase)
        {
            InitializeFreeWeapons();
        }
        // 첫 상점에서 이미 구매한 경우
        else if (isFirstShop && hasFirstPurchase)
        {
            foreach (var option in weaponOptions)
            {
                if (option != null)
                {
                    option.SetPurchased(true);
                }
            }
        }

        UpdateRefreshCostText();
    }
    private void InitializeNewWeapons()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.ResetPurchaseState();
            }
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }

    private void InitializeFreeWeapons()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.ResetPurchaseState();
            }
        }

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                randomWeapons[i].price = 0;
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }
    public WeaponOptionUI[] GetWeaponOptions()
    {
        return weaponOptions;
    }
    public void InitializeShop()
    {
        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }

        // 상점이 새로 열릴 때마다 리프레시 비용 초기화
        currentRefreshCost = initialRefreshCost;
        UpdateRefreshCostText();

        // PlayerStats 참조 설정
        if (playerStats == null)
        {
            playerStats = GameManager.Instance.PlayerStats;
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats reference is null in ShopController!");
                return;
            }
            playerStats.OnCoinChanged += UpdatePlayerCoinsText;
        }

        // UI 초기화
        UpdatePlayerCoinsText(playerStats.CoinCount);
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);
        shopUI.SetActive(true);

        // 무기 옵션 초기화
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        // 모든 무기 옵션 초기화
        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                WeaponData weapon = randomWeapons[i];
                // 첫 상점이고 아직 구매하지 않았다면 무료로 설정
                if (isFirstShop && !hasFirstPurchase)
                {
                    weapon.price = 0;
                }

                // 무기 옵션 초기화 (항상 구매 가능한 상태로 시작)
                weaponOptions[i].ResetPurchaseState();
                weaponOptions[i].Initialize(weapon, this);
            }
        }
    }
    private List<WeaponData> GetRandomWeapons(int count)
    {
        if (weaponDatabase == null || playerStats == null)
        {
            Debug.LogError("WeaponDatabase or PlayerStats is missing!");
            return new List<WeaponData>();
        }

        List<WeaponData> randomWeapons = new List<WeaponData>();

        for (int i = 0; i < count; i++)
        {
            WeaponData weapon = GetRandomWeaponByTierProbability();
            if (weapon != null)
            {
                randomWeapons.Add(weapon);
            }
        }

        return randomWeapons;
    }

    private WeaponData GetRandomWeaponByTierProbability()
    {
        float[] tierProbs = weaponDatabase.tierProbability.GetTierProbabilities(playerStats.Level);
        float random = Random.value * 100f;
        float cumulative = 0f;
        int selectedTier = 1;

        for (int i = 0; i < 4; i++)
        {
            cumulative += tierProbs[i];
            if (random <= cumulative)
            {
                selectedTier = i + 1;
                break;
            }
        }

        List<WeaponData> tierWeapons = weaponDatabase.weapons
            .Where(w => w.currentTier == selectedTier)
            .Where(w => !isFirstShop || w.weaponType != WeaponType.Equipment)
            .ToList();

        if (tierWeapons.Count == 0)
        {
            Debug.LogWarning($"No weapons found for tier {selectedTier}");
            return null;
        }

        return ScriptableObject.Instantiate(tierWeapons[Random.Range(0, tierWeapons.Count)]);
    }
    private void GenerateNewWeaponOptions()
    {
        List<WeaponData> randomWeapons = GetRandomWeapons(weaponOptions.Length);

        for (int i = 0; i < weaponOptions.Length; i++)
        {
            if (i < randomWeapons.Count && weaponOptions[i] != null)
            {
                weaponOptions[i].ResetPurchaseState();
                weaponOptions[i].Initialize(randomWeapons[i], this);
            }
        }
    }
    public void PurchaseWeapon(WeaponData weaponData)
    {
        if (weaponData != null)
        {
            // 구매한 무기 목록에 추가
            purchasedWeapons.Add(weaponData);

            // 첫 상점에서의 첫 구매 처리
            if (isFirstShop && !hasFirstPurchase)
            {
                hasFirstPurchase = true;
                // 첫 상점에서만 나머지 무기들을 비활성화
                foreach (var option in weaponOptions)
                {
                    if (option.WeaponData != weaponData)
                    {
                        option.SetPurchased(true);
                    }
                }
            }

            shopUI.SetActive(false);
            inventoryUI.SetActive(true);
            inventoryController.OnPurchaseItem(weaponData);
        }
    }
    private void SetAllOptionsPurchased()
    {
        foreach (var option in weaponOptions)
        {
            if (option != null)
            {
                option.SetPurchased(true);
            }
        }
    }

    public void CloseShop()
    {
        shopUI.SetActive(false);
    }
    public void RefreshShop()
    {
        if (playerStats == null || !playerStats.SpendCoins(currentRefreshCost)) return;

        // 리프레시 비용 증가
        currentRefreshCost += refreshCostIncrease;
        UpdateRefreshCostText();

        // 새로운 무기 목록 생성
        GenerateNewWeaponOptions();

        // 효과음 재생
        SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
    }
}