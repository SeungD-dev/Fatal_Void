using UnityEngine;

public enum ItemType
{
    ExperienceSmall,
    ExperienceMedium,
    ExperienceLarge,
    Gold,
    HealthPotion,
    Magnet
}

[System.Serializable]
public class ExperienceDropInfo
{
    [Range(0f, 100f)]
    public float smallExpRate = 60f;
    [Range(0f, 100f)]
    public float mediumExpRate = 30f;
    [Range(0f, 100f)]
    public float largeExpRate = 10f;

    public GameObject smallExpPrefab;
    public GameObject mediumExpPrefab;
    public GameObject largeExpPrefab;

    public void ValidateRates()
    {
        float total = smallExpRate + mediumExpRate + largeExpRate;
        if (total != 100f)
        {
            Debug.LogWarning($"Experience drop rates do not sum to 100%. Current total: {total}%");
        }
    }
}

[System.Serializable]
public class GoldDropInfo
{
    public int minGoldAmount = 10;
    public int maxGoldAmount = 50;
    public GameObject goldPrefab;
    public void ValidateAmount()
    {
        if (minGoldAmount > maxGoldAmount)
        {
            Debug.LogWarning("Min gold amount is greater than max gold amount!");
        }
    }
}

[System.Serializable]
public class AdditionalDrop
{
    public ItemType itemType;
    public GameObject itemPrefab;
    [Range(0f, 100f)]
    public float dropRate;
    public bool isMagnetable = true;
}

[CreateAssetMenu(fileName = "EnemyDropTable", menuName = "Scriptable Objects/EnemyDropTable")]
public class EnemyDropTable : ScriptableObject
{
    [Header("Essential Drop Settings")]
    [Range(0f, 100f)]
    public float experienceDropRate = 50f; // 나머지는 자동으로 Gold
    public ExperienceDropInfo experienceInfo;
    public GoldDropInfo goldInfo;

    [Header("Additional Drop Settings")]
    public AdditionalDrop[] additionalDrops;

    private void OnValidate()
    {
        experienceInfo?.ValidateRates();
        goldInfo?.ValidateAmount();
        ValidateAdditionalDrops();
    }

    private void ValidateAdditionalDrops()
    {
        if (additionalDrops == null) return;

        foreach (var drop in additionalDrops)
        {
            if (drop.itemPrefab == null)
            {
                Debug.LogError($"Missing prefab for {drop.itemType} in drop table!");
            }
            if (drop.itemType == ItemType.ExperienceSmall ||
                drop.itemType == ItemType.ExperienceMedium ||
                drop.itemType == ItemType.ExperienceLarge ||
                drop.itemType == ItemType.Gold)
            {
                Debug.LogWarning($"{drop.itemType} should not be in additional drops as it's handled by essential drops!");
            }
        }
    }
}