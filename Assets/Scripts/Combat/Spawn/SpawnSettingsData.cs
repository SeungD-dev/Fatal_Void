using UnityEditor;
using UnityEngine;
//사용안함
[System.Serializable]
public class TimeBasedSpawnSettings
{
    public float gameTimeMinutes;  // 시작 시간(분)
    public int spawnAmount;        // 이 시간대의 스폰량
    public float spawnInterval;    // 이 시간대의 스폰 주기
}


[CreateAssetMenu(fileName = "SpawnSettings", menuName = "Game/SpawnSettings")]
public class SpawnSettingsData : ScriptableObject
{
    [Header("Spawn Settings Over Time")]
    [Tooltip("시간에 따른 스폰 설정")]
    public TimeBasedSpawnSettings[] timeSettings;

    [Header("Limits")]
    [Tooltip("최소 스폰 간격(초)")]
    public float minSpawnInterval = 1f;
    [Tooltip("최대 스폰 간격(초)")]
    public float maxSpawnInterval = 3f;
    [Tooltip("최소 스폰 수")]
    public int minSpawnAmount = 3;
    [Tooltip("최대 스폰 수")]
    public int maxSpawnAmount = 30;

    public (int spawnAmount, float spawnInterval) GetSettingsAtTime(float gameTime)
    {
        float gameTimeMinutes = gameTime / 60f;

        // 첫 번째 설정 이전
        if (gameTimeMinutes < timeSettings[0].gameTimeMinutes)
        {
            return (timeSettings[0].spawnAmount, timeSettings[0].spawnInterval);
        }

        // 마지막 설정 이후
        if (gameTimeMinutes >= timeSettings[timeSettings.Length - 1].gameTimeMinutes)
        {
            var lastSettings = timeSettings[timeSettings.Length - 1];
            return (lastSettings.spawnAmount, lastSettings.spawnInterval);
        }

        // 현재 시간에 해당하는 구간 찾기
        for (int i = 0; i < timeSettings.Length - 1; i++)
        {
            if (gameTimeMinutes >= timeSettings[i].gameTimeMinutes &&
                gameTimeMinutes < timeSettings[i + 1].gameTimeMinutes)
            {
                // 두 시간대 사이 보간
                float t = (gameTimeMinutes - timeSettings[i].gameTimeMinutes) /
                         (timeSettings[i + 1].gameTimeMinutes - timeSettings[i].gameTimeMinutes);

                int amount = Mathf.RoundToInt(Mathf.Lerp(
                    timeSettings[i].spawnAmount,
                    timeSettings[i + 1].spawnAmount,
                    t
                ));

                float interval = Mathf.Lerp(
                    timeSettings[i].spawnInterval,
                    timeSettings[i + 1].spawnInterval,
                    t
                );

                // 한계값 적용
                amount = Mathf.Clamp(amount, minSpawnAmount, maxSpawnAmount);
                interval = Mathf.Clamp(interval, minSpawnInterval, maxSpawnInterval);

                return (amount, interval);
            }
        }

        // 예상치 못한 경우 기본값 반환
        Debug.LogWarning("Unexpected state in GetSettingsAtTime");
        return (minSpawnAmount, maxSpawnInterval);
    }

    // 에디터에서 설정 유효성 검사
    private void OnValidate()
    {
        if (timeSettings == null || timeSettings.Length == 0) return;
        // 한계값 검사
        foreach (var setting in timeSettings)
        {
            setting.spawnAmount = Mathf.Clamp(setting.spawnAmount, minSpawnAmount, maxSpawnAmount);
            setting.spawnInterval = Mathf.Clamp(setting.spawnInterval, minSpawnInterval, maxSpawnInterval);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpawnSettingsData))]
public class SpawnSettingsDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnSettingsData spawnSettings = (SpawnSettingsData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Preview", EditorStyles.boldLabel);

        float testTime = EditorGUILayout.Slider("Test Time (Minutes)", 0f, 15f, 0f);
        var settings = spawnSettings.GetSettingsAtTime(testTime * 60f);

        EditorGUILayout.LabelField($"Spawn Amount: {settings.spawnAmount}");
        EditorGUILayout.LabelField($"Spawn Interval: {settings.spawnInterval:F2}s");
    }
}
#endif