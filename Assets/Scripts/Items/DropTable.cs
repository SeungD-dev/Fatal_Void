using UnityEngine;

[CreateAssetMenu(fileName = "DropTable", menuName = "Scriptable Objects/DropTable")]
public class DropTable : ScriptableObject
{
    public DropInfo[] possibleDrops;

    public void ValidateDropRates()
    {
        float totalRate = 0f;
        foreach (var drop in possibleDrops)
        {
            totalRate += drop.dropRate;
        }

        if (totalRate > 100f)
        {
            Debug.LogWarning($"Total drop rate exceeds 100%: {totalRate}%");
        }
    }
}

public enum ItemType
{
    ExperienceSmall,
    ExperienceLarge,
    HealthPotion,
    Coin,
    Magnet
}

[System.Serializable]
public class DropInfo
{
    public ItemType itemType;
    public GameObject itemPrefab;
    [Range(0f, 100f)]
    public float dropRate;
    [Min(1)]
    public int minAmount = 1;
    [Min(1)]
    public int maxAmount = 1;
    [Tooltip("자석 효과의 영향을 받는지 여부")]
    public bool isMagnetable = true;
}
