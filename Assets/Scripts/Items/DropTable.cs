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
    HealthPotion
}

[System.Serializable]
public class DropInfo
{
    public ItemType itemType;
    public GameObject itemPrefab;
    [Range(0f, 100f)]
    public float dropRate;  // 드롭 확률 (%)
    [Min(1)]
    public int minAmount = 1;  // 최소 드롭 수량
    [Min(1)]
    public int maxAmount = 1;  // 최대 드롭 수량
}

