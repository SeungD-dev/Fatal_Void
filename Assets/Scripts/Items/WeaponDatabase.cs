using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class TierProbability
{
    [Header("Base Probabilities")]
    [Range(0, 100)]
    public float tier1Probability = 75f;
    [Range(0, 100)]
    public float tier2Probability = 15f;
    [Range(0, 100)]
    public float tier3Probability = 9f;
    [Range(0, 100)]
    public float tier4Probability = 1f;

    [Header("Level Scaling (per level)")]
    [Range(-5, 0)]
    public float tier1Decrease = -2f;  // 레벨당 감소율
    [Range(-2, 2)]
    public float tier2Scaling = 1f;    // 레벨당 변화율
    [Range(-2, 2)]
    public float tier3Scaling = 0.8f;  // 레벨당 변화율
    [Range(-1, 1)]
    public float tier4Scaling = 0.2f;  // 레벨당 변화율

    [Header("Min/Max Probabilities")]
    [Range(0, 100)] public float tier1MinProb = 20f;  // 1티어 최소 확률
    [Range(0, 100)] public float tier2MinProb = 10f;  // 2티어 최소 확률
    [Range(0, 100)] public float tier3MinProb = 5f;   // 3티어 최소 확률
    [Range(0, 100)] public float tier4MinProb = 1f;   // 4티어 최소 확률

    [Range(0, 100)] public float tier1MaxProb = 75f;  // 1티어 최대 확률
    [Range(0, 100)] public float tier2MaxProb = 40f;  // 2티어 최대 확률
    [Range(0, 100)] public float tier3MaxProb = 30f;  // 3티어 최대 확률
    [Range(0, 100)] public float tier4MaxProb = 15f;  // 4티어 최대 확률

    public float[] GetTierProbabilities(int playerLevel)
    {
        float[] probs = new float[4];

        // 레벨에 따른 기본 확률 계산
        probs[0] = tier1Probability + (tier1Decrease * (playerLevel - 1));
        probs[1] = tier2Probability + (tier2Scaling * (playerLevel - 1));
        probs[2] = tier3Probability + (tier3Scaling * (playerLevel - 1));
        probs[3] = tier4Probability + (tier4Scaling * (playerLevel - 1));

        // 최소/최대 확률 제한
        probs[0] = Mathf.Clamp(probs[0], tier1MinProb, tier1MaxProb);
        probs[1] = Mathf.Clamp(probs[1], tier2MinProb, tier2MaxProb);
        probs[2] = Mathf.Clamp(probs[2], tier3MinProb, tier3MaxProb);
        probs[3] = Mathf.Clamp(probs[3], tier4MinProb, tier4MaxProb);

        // 총합 계산
        float total = probs[0] + probs[1] + probs[2] + probs[3];

        // 100%로 정규화
        for (int i = 0; i < 4; i++)
        {
            probs[i] = (probs[i] / total) * 100f;
        }

        return probs;
    }

    public float GetTierProbability(int tier, int playerLevel)
    {
        float[] probs = GetTierProbabilities(playerLevel);
        return tier switch
        {
            1 => probs[0],
            2 => probs[1],
            3 => probs[2],
            4 => probs[3],
            _ => 0f
        };
    }

#if UNITY_EDITOR
    // 유효성 검사를 위한 OnValidate
    private void OnValidate()
    {
        // 초기 확률의 합이 100이 되도록 보장
        float total = tier1Probability + tier2Probability + tier3Probability + tier4Probability;
        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            float scale = 100f / total;
            tier1Probability *= scale;
            tier2Probability *= scale;
            tier3Probability *= scale;
            tier4Probability *= scale;
        }

        // 최소/최대 확률 제한 유효성 검사
        tier1MinProb = Mathf.Min(tier1MinProb, tier1MaxProb);
        tier2MinProb = Mathf.Min(tier2MinProb, tier2MaxProb);
        tier3MinProb = Mathf.Min(tier3MinProb, tier3MaxProb);
        tier4MinProb = Mathf.Min(tier4MinProb, tier4MaxProb);
    }
#endif
}
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Inventory/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    [Header("Base Weapons")]
    [Tooltip("티어 1 기본 무기들을 이곳에 등록하세요")]
    [SerializeField] private List<WeaponData> baseWeapons = new List<WeaponData>();

    [Header("Tier Properties")]
    public TierProbability tierProbability;

    // 모든 티어의 무기를 포함하는 리스트
    private List<WeaponData> allWeapons = new List<WeaponData>();

    // public getter
    public List<WeaponData> weapons => allWeapons;

    private void OnEnable()
    {
        InitializeWeapons();
    }

    private void OnValidate()
    {
        // baseWeapons에 있는 무기들이 모두 티어 1인지 확인
        foreach (var weapon in baseWeapons)
        {
            if (weapon != null && weapon.currentTier != 1)
            {
                Debug.LogWarning($"Warning: {weapon.name}의 티어가 1이 아닙니다. baseWeapons에는 티어 1 무기만 등록해야 합니다.");
            }
        }

        // 게임 실행 중이 아닐 때만 무기 초기화 실행
        if (!Application.isPlaying)
        {
            InitializeWeapons();
        }
    }

    private void InitializeWeapons()
    {
        allWeapons.Clear();

        // 유효성 검사
        if (baseWeapons == null || baseWeapons.Count == 0)
        {
            Debug.LogWarning("WeaponDatabase: baseWeapons가 비어있습니다!");
            return;
        }

        // 먼저 기본 무기들 추가
        foreach (var baseWeapon in baseWeapons)
        {
            if (baseWeapon == null) continue;

            allWeapons.Add(baseWeapon);

            // 티어 2-4 무기 생성
            WeaponData currentWeapon = baseWeapon;
            for (int tier = 2; tier <= 4; tier++)
            {
                WeaponData nextTierWeapon = Instantiate(baseWeapon);
                nextTierWeapon.currentTier = tier;
                nextTierWeapon.name = $"{baseWeapon.name} Tier {tier}";
                allWeapons.Add(nextTierWeapon);
            }
        }

        // 디버그 로깅
        Debug.Log($"WeaponDatabase initialized with {allWeapons.Count} total weapons");
        for (int tier = 1; tier <= 4; tier++)
        {
            int count = allWeapons.Count(w => w.currentTier == tier);
            Debug.Log($"Tier {tier} weapons: {count}");
        }
    }
}