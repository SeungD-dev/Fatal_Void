using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Scriptable Objects/EnemySpawnDatabase")]
public class EnemySpawnDatabase : ScriptableObject
{
    [Header("Enemy Spawn Settings")]
    [Tooltip("적 타입별 스폰 설정")]
    public EnemySpawnSettings[] enemySettings;

    [Header("Balance Settings")]
    [Tooltip("비율 체크 주기 (이 수만큼 적이 스폰될 때마다 비율 검사)")]
    public int ratioCheckInterval = 10;

    private int totalSpawnCount = 0;

    public void ResetSpawnCounts()
    {
        totalSpawnCount = 0;
        foreach (var setting in enemySettings)
        {
            setting.spawnCount = 0;
        }
    }

    public EnemyData GetRandomEnemy(float gameTime)
    {
        float gameTimeMinutes = gameTime / 60f;
        totalSpawnCount++;

        // 비율 체크 주기에 도달했는지 확인
        bool shouldCheckRatio = (totalSpawnCount % ratioCheckInterval == 0);

        if (shouldCheckRatio)
        {
            return GetEnemyWithRatioCheck(gameTimeMinutes);
        }
        else
        {
            return GetEnemyByWeight(gameTimeMinutes);
        }
    }

    private EnemyData GetEnemyWithRatioCheck(float gameTimeMinutes)
    {
        List<EnemySpawnSettings> availableEnemies = new List<EnemySpawnSettings>();

        foreach (var setting in enemySettings)
        {
            float currentRatio = (totalSpawnCount == 0) ? 0f :
                               (setting.spawnCount * 100f / totalSpawnCount);

            // 최소 비율을 만족하지 못한 적들 우선 선택
            if (currentRatio < setting.minSpawnRatio)
            {
                availableEnemies.Add(setting);
                continue;
            }

            // 최대 비율을 초과하지 않은 적들 선택
            if (currentRatio < setting.maxSpawnRatio)
            {
                float weight = setting.GetSpawnWeight(gameTimeMinutes);
                if (weight > 0)
                {
                    availableEnemies.Add(setting);
                }
            }
        }

        if (availableEnemies.Count == 0)
        {
            Debug.LogWarning("No available enemies within ratio constraints");
            return GetEnemyByWeight(gameTimeMinutes); // 폴백: 일반 가중치 선택
        }

        // 선택된 적들 중에서 가중치 기반 선택
        float totalWeight = 0f;
        foreach (var enemy in availableEnemies)
        {
            totalWeight += enemy.GetSpawnWeight(gameTimeMinutes);
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in availableEnemies)
        {
            currentWeight += enemy.GetSpawnWeight(gameTimeMinutes);
            if (randomWeight <= currentWeight)
            {
                enemy.spawnCount++;
                return enemy.enemyData;
            }
        }

        availableEnemies[0].spawnCount++;
        return availableEnemies[0].enemyData;
    }

    private EnemyData GetEnemyByWeight(float gameTimeMinutes)
    {
        float totalWeight = 0f;
        foreach (var setting in enemySettings)
        {
            float currentRatio = (totalSpawnCount == 0) ? 0f :
                               (setting.spawnCount * 100f / totalSpawnCount);

            if (currentRatio < setting.maxSpawnRatio)
            {
                totalWeight += setting.GetSpawnWeight(gameTimeMinutes);
            }
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning($"No available enemies at time: {gameTimeMinutes:F1} minutes");
            return null;
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var setting in enemySettings)
        {
            float currentRatio = (totalSpawnCount == 0) ? 0f :
                               (setting.spawnCount * 100f / totalSpawnCount);

            if (currentRatio < setting.maxSpawnRatio)
            {
                currentWeight += setting.GetSpawnWeight(gameTimeMinutes);
                if (randomWeight <= currentWeight)
                {
                    setting.spawnCount++;
                    return setting.enemyData;
                }
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public void DebugSpawnRatios()
    {
        if (totalSpawnCount == 0)
        {
            Debug.Log("No spawns yet");
            return;
        }

        foreach (var setting in enemySettings)
        {
            float ratio = setting.spawnCount * 100f / totalSpawnCount;
            Debug.Log($"Enemy: {setting.enemyData.name}\n" +
                     $"Spawn Count: {setting.spawnCount}\n" +
                     $"Current Ratio: {ratio:F1}%\n" +
                     $"Min Ratio: {setting.minSpawnRatio}%\n" +
                     $"Max Ratio: {setting.maxSpawnRatio}%");
        }
    }
#endif
}

// 에디터 확장
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
