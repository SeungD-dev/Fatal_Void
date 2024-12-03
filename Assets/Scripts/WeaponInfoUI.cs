using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI weaponLevelText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponRarityImage;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponDPSText;
    [SerializeField] private TextMeshProUGUI weaponDescriptionText;

    public void UpdateWeaponInfo(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        weaponLevelText.text = $"Lv.{weaponData.weaponLevel}";
        weaponNameText.text = weaponData.weaponName;
        weaponImage.sprite = weaponData.weaponIcon;
        weaponDPSText.text = $"DPS : {weaponData.weaponDamage}";
        weaponDescriptionText.text = weaponData.weaponDescription;

        Color rarityColor = GetRarityColor(weaponData.rarity);
        weaponRarityImage.color = new Color(rarityColor.r,rarityColor.g,rarityColor.b , 0.3f);
    }

    private Color GetRarityColor(WeaponRarity rarity)
    {
        return rarity switch
        {
            WeaponRarity.common => new Color(0.8f, 0.8f, 0.8f),
            WeaponRarity.uncommon => new Color(0.3f, 0.3f, 0.3f),
            _ => Color.white
        };
    }
}
