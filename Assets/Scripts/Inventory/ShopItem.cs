using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image rarityBackgroundImage;
    [SerializeField] private TextMeshProUGUI dpsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button sellButton;

    private ShopController shopUI;
    private WeaponData weaponData;
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GameManager.Instance.PlayerStats;
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        }
    }

    public void Initialize(WeaponData weapon, ShopController shop)
    {
        weaponData = weapon;
        shopUI = shop;
        UpdateUI();
        UpdatePurchaseButtonState();
    }

    private void UpdateUI()
    {
        if (weaponData == null) return;

        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        descriptionText.text = weaponData.weaponDescription;
     
        priceText.text = $"{weaponData.price} Coins"; // 가격 표시
        Color tierColor = weaponData.GetTierColor();
        weaponImage.color = tierColor;  // 무기 아이콘에 색상 적용
    }

    private void UpdatePurchaseButtonState()
    {
        if (purchaseButton != null && playerStats != null)
        {
            bool canAfford = playerStats.CoinCount >= weaponData.price;
            purchaseButton.interactable = canAfford;

            // 선택적: 버튼 색상 변경
            Color buttonColor = canAfford ? Color.white : Color.gray;
            purchaseButton.GetComponent<Image>().color = buttonColor;
        }
    }

    private void OnPurchaseButtonClicked()
    {
        if (playerStats.CoinCount >= weaponData.price)
        {
            playerStats.SpendCoins(weaponData.price); // PlayerStats에 추가 필요
            shopUI.PurchaseWeapon(weaponData);
        }
    }

    // 코인 수가 변경될 때마다 버튼 상태 업데이트
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

    public WeaponData GetWeaponData()
    {
        return weaponData;
    }
}