using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Scriptable Objects/EnemySpawnDatabase")]
public class EnemySpawnDatabase : ScriptableObject
{
    [Header("Enemy Spawn Settings")]
    [Tooltip("적 타입별 스폰 설정")]
    public EnemySpawnSettings[] enemySettings;

    [Header("Balance Settings")]
    [Tooltip("비율 체크 주기 (이 수만큼 적이 스폰될 때마다 비율 검사)")]
    public int ratioCheckInterval = 10;

    // 캐시된 계산 결과
    private float[] cachedWeights;
    private float[] cachedRatios;
    private float cachedTotalWeight;
    private float lastCacheTime = -1f;
    private const float CACHE_DURATION = 0.5f; // 0.5초마다 갱신

    private int totalSpawnCount;

    public void ResetSpawnCounts()
    {
        totalSpawnCount = 0;
        if (enemySettings != null)
        {
            for (int i = 0; i < enemySettings.Length; i++)
            {
                enemySettings[i].spawnCount = 0;
            }
        }
    }

    public EnemyData GetRandomEnemy(float gameTime)
    {
        if (enemySettings == null || enemySettings.Length == 0)
        {
            Debug.LogWarning("No enemy settings available");
            return null;
        }

        float gameTimeMinutes = gameTime / 60f;
        totalSpawnCount++;

        return (totalSpawnCount % ratioCheckInterval == 0)
            ? GetEnemyWithRatioCheck(gameTimeMinutes)
            : GetEnemyByWeight(gameTimeMinutes);
    }

    private EnemyData GetEnemyWithRatioCheck(float gameTimeMinutes)
    {
        InitializeCacheArrays();

        int availableCount = 0;
        float totalAvailableWeight = 0f;

        for (int i = 0; i < enemySettings.Length; i++)
        {
            var setting = enemySettings[i];
            float currentRatio = CalculateSpawnRatio(setting);

            if (currentRatio < setting.minSpawnRatio)
            {
                cachedWeights[availableCount] = setting.GetSpawnWeight(gameTimeMinutes);
                cachedRatios[availableCount] = i;
                totalAvailableWeight += cachedWeights[availableCount];
                availableCount++;
                continue;
            }

            if (currentRatio < setting.maxSpawnRatio)
            {
                float weight = setting.GetSpawnWeight(gameTimeMinutes);
                if (weight > 0)
                {
                    cachedWeights[availableCount] = weight;
                    cachedRatios[availableCount] = i;
                    totalAvailableWeight += weight;
                    availableCount++;
                }
            }
        }

        if (availableCount == 0)
        {
            Debug.LogWarning("No available enemies within ratio constraints");
            return GetEnemyByWeight(gameTimeMinutes);
        }

        float randomWeight = Random.Range(0f, totalAvailableWeight);
        float currentWeight = 0f;

        for (int i = 0; i < availableCount; i++)
        {
            currentWeight += cachedWeights[i];
            if (randomWeight <= currentWeight)
            {
                int settingIndex = (int)cachedRatios[i];
                enemySettings[settingIndex].spawnCount++;
                return enemySettings[settingIndex].enemyData;
            }
        }

        // 기본값 반환
        int defaultIndex = (int)cachedRatios[0];
        enemySettings[defaultIndex].spawnCount++;
        return enemySettings[defaultIndex].enemyData;
    }

    private EnemyData GetEnemyByWeight(float gameTimeMinutes)
    {
        // 캐시 갱신 필요 여부 확인
        if (Time.time - lastCacheTime >= CACHE_DURATION)
        {
            UpdateWeightCache(gameTimeMinutes);
            lastCacheTime = Time.time;
        }

        if (cachedTotalWeight <= 0f)
        {
            Debug.LogWarning($"No available enemies at time: {gameTimeMinutes:F1} minutes");
            return null;
        }

        float randomWeight = Random.Range(0f, cachedTotalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < enemySettings.Length; i++)
        {
            if (cachedWeights[i] <= 0) continue;

            currentWeight += cachedWeights[i];
            if (randomWeight <= currentWeight)
            {
                enemySettings[i].spawnCount++;
                return enemySettings[i].enemyData;
            }
        }

        return null;
    }

    private void UpdateWeightCache(float gameTimeMinutes)
    {
        InitializeCacheArrays();

        cachedTotalWeight = 0f;

        for (int i = 0; i < enemySettings.Length; i++)
        {
            var setting = enemySettings[i];
            float currentRatio = CalculateSpawnRatio(setting);

            if (currentRatio < setting.maxSpawnRatio)
            {
                cachedWeights[i] = setting.GetSpawnWeight(gameTimeMinutes);
                cachedTotalWeight += cachedWeights[i];
            }
            else
            {
                cachedWeights[i] = 0f;
            }
        }
    }

    private void InitializeCacheArrays()
    {
        if (cachedWeights == null || cachedWeights.Length != enemySettings.Length)
        {
            cachedWeights = new float[enemySettings.Length];
            cachedRatios = new float[enemySettings.Length];
        }
    }

    private float CalculateSpawnRatio(EnemySpawnSettings setting)
    {
        return totalSpawnCount == 0 ? 0f : (setting.spawnCount * 100f / totalSpawnCount);
    }

#if UNITY_EDITOR
    public void DebugSpawnRatios()
    {
        if (totalSpawnCount == 0)
        {
            Debug.Log("No spawns yet");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < enemySettings.Length; i++)
        {
            var setting = enemySettings[i];
            float ratio = CalculateSpawnRatio(setting);

            sb.AppendLine($"Enemy: {setting.enemyData.name}")
              .AppendLine($"Spawn Count: {setting.spawnCount}")
              .AppendLine($"Current Ratio: {ratio:F1}%")
              .AppendLine($"Min Ratio: {setting.minSpawnRatio}%")
              .AppendLine($"Max Ratio: {setting.maxSpawnRatio}%")
              .AppendLine();
        }

        Debug.Log(sb.ToString());
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnemySpawnDatabase))]
public class EnemySpawnDatabaseEditor : Editor
{
    private float debugTimeMinutes = 0f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnemySpawnDatabase database = (EnemySpawnDatabase)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        debugTimeMinutes = EditorGUILayout.Slider("Test Time (Minutes)", debugTimeMinutes, 0f, 15f);

        if (GUILayout.Button("Show Current Ratios"))
        {
            database.DebugSpawnRatios();
        }

        if (GUILayout.Button("Reset Spawn Counts"))
        {
            database.ResetSpawnCounts();
        }
    }
}
#endif