using UnityEngine;
//사용안함
[System.Serializable]
public class EnemySpawnSettings
{
    public EnemyData enemyData;

    [Header("Spawn Probability Settings")]
    [Tooltip("시간에 따른 등장 확률 커브 (X: 시간(분), Y: 0~1 확률)")]
    public AnimationCurve spawnProbabilityCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(15, 1)
    );

    [Tooltip("최대 등장 확률 (%)")]
    [Range(0f, 100f)]
    public float maxSpawnWeight = 100f;

    [Header("Ratio Control")]
    [Tooltip("전체 스폰 중 이 적의 최소 비율 (%)")]
    [Range(0f, 100f)]
    public float minSpawnRatio = 0f;

    [Tooltip("전체 스폰 중 이 적의 최대 비율 (%)")]
    [Range(0f, 100f)]
    public float maxSpawnRatio = 100f;

    // 현재 스폰 수 추적
    [System.NonSerialized]
    public int spawnCount = 0;

    public float GetSpawnWeight(float gameTimeMinutes)
    {
        return spawnProbabilityCurve.Evaluate(gameTimeMinutes) * maxSpawnWeight;
    }
}