using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private Button myPurchaseButton;    // 각 WeaponOption의 구매 버튼
    [SerializeField] private Button mySellButton;        // 각 WeaponOption의 판매 버튼

    private WeaponData weaponData;
    private ShopController shopUI;

    public void Initialize(WeaponData weapon, ShopController shop)
    {
        weaponData = weapon;
        shopUI = shop;
        SetupUI();
        SetupButtons();
    }

    private void SetupUI()
    {
        if (weaponData == null) return;

        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        descriptionText.text = weaponData.weaponDescription;
        dpsText.text = $"DPS: {weaponData.weaponDamage}";
        weaponLevelText.text = $"Lv.{weaponData.weaponLevel}";

        Color rarityColor = GetRarityColor(weaponData.rarity);
        rarityBackgroundImage.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
    }

    private void SetupButtons()
    {
        // 각 WeaponOption의 구매 버튼에 리스너 추가
        if (myPurchaseButton != null)
        {
            myPurchaseButton.onClick.RemoveAllListeners();
            myPurchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        // 각 WeaponOption의 판매 버튼에 리스너 추가
        if (mySellButton != null)
        {
            mySellButton.onClick.RemoveAllListeners();
            mySellButton.onClick.AddListener(OnSellClicked);
        }
    }

    private void OnPurchaseClicked()
    {
        if (weaponData != null && shopUI != null)
        {
            Debug.Log($"Purchasing weapon: {weaponData.weaponName}");
            shopUI.PurchaseWeapon(weaponData);
        }
    }

    private void OnSellClicked()
    {
        if (weaponData != null && shopUI != null)
        {
            Debug.Log($"Selling weapon: {weaponData.weaponName}");
            shopUI.SellWeapon(weaponData);
        }
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
