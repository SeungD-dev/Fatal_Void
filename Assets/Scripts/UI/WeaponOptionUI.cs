using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponOptionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private Button myPurchaseButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI weaponTierText; // 티어 텍스트 추가

    private ShopController shopUI;
    private PlayerStats playerStats;
    private WeaponData weaponData;
    private bool isPurchased = false;

    public WeaponData WeaponData => weaponData;

    private void Start()
    {
        playerStats = GameManager.Instance.PlayerStats;
    }

    public void Initialize(WeaponData weapon, ShopController shop)
    {
        weaponData = weapon;
        shopUI = shop;
        isPurchased = false;
        SetupUI();
        SetupButtons();
        UpdatePurchaseButtonState();
    }

    private void SetupUI()
    {
        if (weaponData == null) return;

        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        descriptionText.text = weaponData.weaponDescription;

        Color tierColor = weaponData.GetTierColor();
        weaponImage.color = tierColor;
        weaponImage.sprite = weaponData.weaponIcon;
        weaponImage.preserveAspect = true;

        priceText.text = weaponData.price == 0 ? "FREE" : $"{weaponData.price} Coins";

        // 티어 텍스트 설정
        if (weaponTierText != null)
        {
            weaponTierText.text = $"Tier {weaponData.currentTier}";
            weaponTierText.color = tierColor; // 티어 색상 적용
        }
    }

    private void SetupButtons()
    {
        if (myPurchaseButton != null)
        {
            myPurchaseButton.onClick.RemoveAllListeners();
            myPurchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }

    private void UpdatePurchaseButtonState()
    {
        if (myPurchaseButton != null && playerStats != null && weaponData != null)
        {
            bool canAfford = (weaponData.price == 0 || playerStats.CoinCount >= weaponData.price) && !isPurchased;
            myPurchaseButton.interactable = canAfford;
            Color buttonColor = isPurchased ? Color.gray : (canAfford ? Color.white : Color.gray);
            myPurchaseButton.GetComponent<Image>().color = buttonColor;
        }
    }

    private void OnPurchaseClicked()
    {
        SoundManager.Instance.PlaySound("Button_sfx",1f,false);
        if (weaponData != null && shopUI != null && playerStats != null && !isPurchased)
        {
            if (weaponData.price == 0 || playerStats.CoinCount >= weaponData.price)
            {
                if (weaponData.price > 0)
                {
                    playerStats.SpendCoins(weaponData.price);
                }
                isPurchased = true;
                UpdatePurchaseButtonState();
                shopUI.PurchaseWeapon(weaponData);
            }
        }
    }

    public void SetPurchased(bool purchased)
    {
        isPurchased = purchased;
        UpdatePurchaseButtonState();
    }

    public void UpdateUI()
    {
        SetupUI();
        UpdatePurchaseButtonState();
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinChanged += OnCoinCountChanged;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnCoinChanged -= OnCoinCountChanged;
        }
    }

    private void OnCoinCountChanged(int newCoinCount)
    {
        UpdatePurchaseButtonState();
    }
}