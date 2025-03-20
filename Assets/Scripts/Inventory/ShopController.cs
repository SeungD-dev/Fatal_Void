using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private GameObject noticeUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private GameObject weaponPrefab;
    [Header("UI Texts")]
    [SerializeField] private TMPro.TextMeshProUGUI refreshCostText;
    [SerializeField] private TMPro.TextMeshProUGUI playerCoinsText;
    [SerializeField] private TMPro.TextMeshProUGUI noticeText;
    [SerializeField] private float noticeDisplayTime = 2f;
    [Header("Transition Effect")]
    [SerializeField] private ScreenTransitionEffect transitionEffect;
    public bool isFirstShop = true;
    private bool hasFirstPurchase = false;
    private bool isNoticeClosed = true;
    private PlayerStats playerStats;
    private HashSet<WeaponData> purchasedWeapons = new HashSet<WeaponData>();
    private Coroutine currentNoticeCoroutine;
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
        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.ForceInitialize();
        }
    }
    private void OnDisable()
    {
        // 컴포넌트가 비활성화될 때 실행 중인 코루틴 정리
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
            currentNoticeCoroutine = null;
        }

        // Notice UI가 활성화된 상태로 남아있지 않도록 보장
        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
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
        StopAllCoroutines();
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
        if (inventoryController != null)
        {
            // 상점 UI 비활성화
            shopUI.SetActive(false);

            // 인벤토리 컨트롤러를 통해 인벤토리 열기
            inventoryController.OpenInventory();
        }
        else
        {
            Debug.LogError("InventoryController reference is missing!");
        }
    }

    public void OpenShop()
    {
        if (hasFirstPurchase)
        {
            isFirstShop = false;
        }

        // 상점 UI 준비 (아직 표시 안 함)
        PrepareShop(false);

        if (transitionEffect != null)
        {
            // 안에서 바깥으로 효과 (reverseEffect = true)
            transitionEffect.reverseEffect = true;
            transitionEffect.PlayTransition(() => {
                ShowShopUI();
            });
        }
        else
        {
            ShowShopUI();
        }
    }

    private void PrepareShop(bool showUI)
    {
        playerControlUI.SetActive(false);
        playerStatsUI.SetActive(false);

        if (showUI)
        {
            shopUI.SetActive(true);
            inventoryUI.SetActive(false);
        }

        // 첫 상점이 아닐 경우 새로운 무기 옵션을 생성
        if (!isFirstShop)
        {
            InitializeNewWeapons();
        }
        // 첫 상점이면서 아직 구매하지 않은 경우 무료 무기 표시
        else if (isFirstShop && !hasFirstPurchase)
        {
            InitializeFreeWeapons();
        }
        // 첫 상점에서 이미 구매했지만 아직 첫 상점인 경우
        // 다음 웨이브로 진행하지 않고 상점으로 돌아온 경우에 해당
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
    private void ShowShopUI()
    {
        shopUI.SetActive(true);
        inventoryUI.SetActive(false);

        // 게임 상태가 일시정지 상태가 아니라면 일시정지로 변경
        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }
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
    public void OnPurchaseClicked(WeaponOptionUI weaponOption)
    {
        if (weaponOption == null || weaponOption.WeaponData == null || playerStats == null) return;

        WeaponData weaponData = weaponOption.WeaponData;

        // 인벤토리 공간 체크
        if (!HasEnoughSpaceForItem(weaponData))
        {
            ShowNotice("Not enough space in inventory!");
            return;
        }

        // 구매 진행
        if (weaponData.price == 0 || playerStats.SpendCoins(weaponData.price))
        {
            SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
            weaponOption.SetPurchased(true);
            PurchaseWeapon(weaponData);
        }
    }
    private bool HasEnoughSpaceForItem(WeaponData weaponData)
    {
        if (mainInventoryGrid != null && !mainInventoryGrid.IsInitialized)
        {
            mainInventoryGrid.ForceInitialize();
        }
        // weaponData 체크
        if (weaponData == null)
        {
         return false;
        }

        // mainInventoryGrid 체크
        if (mainInventoryGrid == null)
        {            
            return false;
        }

        // weaponPrefab 체크
        if (weaponPrefab == null)
        {
            return false;
        }

        try
        { 
            // 임시 InventoryItem 생성
            GameObject tempObj = Instantiate(weaponPrefab);
            if (tempObj == null)
            {
                Debug.LogError("Failed to instantiate weapon prefab!");
                return false;
            }

            InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
            if (tempItem == null)
            {
                Destroy(tempObj);
                return false;
            }
            tempItem.Initialize(weaponData);
            // 공간 체크
            Vector2Int? freePosition = mainInventoryGrid.FindSpaceForObject(tempItem);
            // 임시 오브젝트 제거
            Destroy(tempObj);

            return freePosition.HasValue;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in HasEnoughSpaceForItem: {e.Message}\nStackTrace: {e.StackTrace}");
            return false;
        }
    }
    private void ShowNotice(string message)
    {
        if (noticeUI == null || noticeText == null) return;

        // 이전에 실행 중인 코루틴이 있다면 중지
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
        }

        noticeText.text = message;
        noticeUI.SetActive(true);

        currentNoticeCoroutine = StartCoroutine(HideNoticeAfterDelay());
    }


    private IEnumerator HideNoticeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(noticeDisplayTime);

        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        currentNoticeCoroutine = null;
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
                WeaponData weaponCopy = ScriptableObject.Instantiate(randomWeapons[i]);
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