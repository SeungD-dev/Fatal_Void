using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[Serializable]
public class TierProbability
{
    [System.Serializable]
    public class WaveRangeProbability
    {
        [Tooltip("이 확률이 적용되는 최소 웨이브 (이상)")]
        public int minWave;
        [Tooltip("이 확률이 적용되는 최대 웨이브 (이하)")]
        public int maxWave;

        [Header("Tier Probabilities")]
        [Range(0, 100)]
        public float tier1Probability = 70f;
        [Range(0, 100)]
        public float tier2Probability = 20f;
        [Range(0, 100)]
        public float tier3Probability = 8f;
        [Range(0, 100)]
        public float tier4Probability = 2f;

        public bool IsInRange(int wave)
        {
            return wave >= minWave && wave <= maxWave;
        }
    }

    [Header("웨이브 범위별 확률")]
    public List<WaveRangeProbability> waveRanges = new List<WaveRangeProbability>()
    {
        new WaveRangeProbability { minWave = 1, maxWave = 3,
            tier1Probability = 85, tier2Probability = 15, tier3Probability = 0, tier4Probability = 0 },
        new WaveRangeProbability { minWave = 4, maxWave = 6,
            tier1Probability = 70, tier2Probability = 25, tier3Probability = 5, tier4Probability = 0 },
        new WaveRangeProbability { minWave = 7, maxWave = 9,
            tier1Probability = 55, tier2Probability = 30, tier3Probability = 10, tier4Probability = 5 },
        new WaveRangeProbability { minWave = 10, maxWave = 15,
            tier1Probability = 40, tier2Probability = 35, tier3Probability = 15, tier4Probability = 10 },
        new WaveRangeProbability { minWave = 16, maxWave = 99,
            tier1Probability = 30, tier2Probability = 40, tier3Probability = 20, tier4Probability = 10 }
    };

    [Tooltip("무기 타입별 티어 확률 보정값")]
    [SerializeField]
    private SerializableDictionary<WeaponType, float[]> weaponTypeModifiers =
        new SerializableDictionary<WeaponType, float[]>();

    public float[] GetTierProbabilities(int currentWave, WeaponType weaponType = WeaponType.Buster)
    {
        // 해당 웨이브에 맞는 기본 확률 가져오기
        float[] baseProbs = GetBaseProbabilitiesForWave(currentWave);

        // 무기 타입 보정 적용
        if (weaponType != WeaponType.Buster && weaponTypeModifiers.ContainsKey(weaponType))
        {
            float[] modifiers = weaponTypeModifiers[weaponType];
            for (int i = 0; i < 4; i++)
            {
                baseProbs[i] *= modifiers[i];
            }
        }

        // 확률 정규화
        NormalizeProbabilities(baseProbs);

        return baseProbs;
    }

    private float[] GetBaseProbabilitiesForWave(int wave)
    {
        var range = waveRanges.Find(r => r.IsInRange(wave)) ?? waveRanges[0];

        return new float[]
        {
            range.tier1Probability,
            range.tier2Probability,
            range.tier3Probability,
            range.tier4Probability
        };
    }

    private void NormalizeProbabilities(float[] probs)
    {
        float total = probs.Sum();
        if (total > 0)
        {
            for (int i = 0; i < probs.Length; i++)
            {
                probs[i] = (probs[i] / total) * 100f;
            }
        }
        else
        {
            probs[0] = 100f;
            Debug.LogWarning("유효하지 않은 확률 분포가 감지되었습니다. 기본값을 사용합니다.");
        }
    }

    public WeaponTier GetRandomTier(int currentWave, WeaponType weaponType = WeaponType.Buster)
    {
        float[] probs = GetTierProbabilities(currentWave, weaponType);
        float random = UnityEngine.Random.value * 100f;
        float cumulative = 0f;

        for (int i = 0; i < probs.Length; i++)
        {
            cumulative += probs[i];
            if (random <= cumulative)
            {
                return (WeaponTier)(i + 1);
            }
        }

        return WeaponTier.Tier1;
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        // 웨이브 범위 정렬
        waveRanges.Sort((a, b) => a.minWave.CompareTo(b.minWave));

        // 웨이브 범위 유효성 검사
        for (int i = 0; i < waveRanges.Count; i++)
        {
            ValidateWaveRange(waveRanges[i]);
        }

        // 무기 타입 모디파이어 초기화
        InitializeWeaponTypeModifiers();
    }

    private void ValidateWaveRange(WaveRangeProbability range)
    {
        range.minWave = Mathf.Max(1, range.minWave);
        range.maxWave = Mathf.Max(range.minWave, range.maxWave);

        float total = range.tier1Probability + range.tier2Probability +
                     range.tier3Probability + range.tier4Probability;

        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            float scale = 100f / total;
            range.tier1Probability *= scale;
            range.tier2Probability *= scale;
            range.tier3Probability *= scale;
            range.tier4Probability *= scale;
        }
    }

    private void InitializeWeaponTypeModifiers()
    {
        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            if (!weaponTypeModifiers.ContainsKey(type))
            {
                weaponTypeModifiers[type] = new float[] { 1f, 1f, 1f, 1f };
            }
        }
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

    private List<WeaponData> allWeapons = new List<WeaponData>();
    public List<WeaponData> weapons => allWeapons;

    private void OnEnable()
    {
        InitializeWeapons();
    }
   
    private void InitializeWeapons()
    {
        allWeapons.Clear();

        if (baseWeapons == null || baseWeapons.Count == 0)
        {
            Debug.LogWarning("WeaponDatabase: baseWeapons가 비어있습니다!");
            return;
        }

        foreach (var baseWeapon in baseWeapons)
        {
            if (baseWeapon == null) continue;

            allWeapons.Add(baseWeapon);

            for (int tier = 2; tier <= 4; tier++)
            {
                WeaponData nextTierWeapon = Instantiate(baseWeapon);
                nextTierWeapon.currentTier = tier;
                nextTierWeapon.name = $"{baseWeapon.name} Tier {tier}";
                allWeapons.Add(nextTierWeapon);
            }
        }

        foreach (var tier in Enum.GetValues(typeof(WeaponTier)))
        {
            int count = allWeapons.Count(w => w.currentTier == (int)tier);
        }
    }

    public WeaponData GetRandomWeapon(int playerLevel, WeaponType preferredType = WeaponType.Buster)
    {
        WeaponTier targetTier = tierProbability.GetRandomTier(playerLevel, preferredType);
        var tieredWeapons = allWeapons.Where(w => w.currentTier == (int)targetTier).ToList();

        if (preferredType != WeaponType.Buster)
        {
            var typeWeapons = tieredWeapons.Where(w => w.weaponType == preferredType).ToList();
            if (typeWeapons.Any())
            {
                tieredWeapons = typeWeapons;
            }
        }

        return tieredWeapons.Count > 0
            ? tieredWeapons[UnityEngine.Random.Range(0, tieredWeapons.Count)]
            : null;
    }
}