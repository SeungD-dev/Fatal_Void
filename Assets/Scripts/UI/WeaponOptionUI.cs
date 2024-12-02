using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponOptionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image rarityBackgroundImage;
    [SerializeField] private TextMeshProUGUI dpsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private Button myPurchaseButton;
    [SerializeField] private TextMeshProUGUI priceText; // 가격 텍스트 추가

    private WeaponData weaponData;
    private ShopController shopUI;
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GameManager.Instance.PlayerStats;
    }

    public void Initialize(WeaponData weapon, ShopController shop)
    {
        weaponData = weapon;
        shopUI = shop;
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
        dpsText.text = $"DPS: {weaponData.weaponDamage}";
        weaponLevelText.text = $"Lv.{weaponData.weaponLevel}";

        // 가격이 0이면 "FREE" 표시
        priceText.text = weaponData.price == 0 ? "FREE" : $"{weaponData.price} Coins";

        Color rarityColor = GetRarityColor(weaponData.rarity);
        rarityBackgroundImage.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
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
            // 무료 아이템이거나 구매 가능한 경우 버튼 활성화
            bool canAfford = weaponData.price == 0 || playerStats.CoinCount >= weaponData.price;
            myPurchaseButton.interactable = canAfford;

            Color buttonColor = canAfford ? Color.white : Color.gray;
            myPurchaseButton.GetComponent<Image>().color = buttonColor;
        }
    }

    private void OnPurchaseClicked()
    {
        if (weaponData != null && shopUI != null && playerStats != null)
        {
            // 무료 아이템이거나 충분한 코인이 있는 경우에만 구매 가능
            if (weaponData.price == 0 || playerStats.CoinCount >= weaponData.price)
            {
                if (weaponData.price > 0)
                {
                    playerStats.SpendCoins(weaponData.price);
                }
                shopUI.PurchaseWeapon(weaponData);
            }
        }
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

    private Color GetRarityColor(WeaponRarity rarity)
    {
        return rarity switch
        {
            WeaponRarity.common => new Color(0.8f, 0.8f, 0.8f),
            WeaponRarity.uncommon => new Color(0.3f, 0.8f, 0.3f),
            _ => Color.white
        };
    }
}
