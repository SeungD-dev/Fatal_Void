using UnityEngine;

[System.Serializable]
public class EnemySpawnWeight
{
    public EnemyData enemyData;
    public AnimationCurve spawnWeightOverTime;
    [Tooltip("최소 게임 시간(초)")]
    public float minGameTime = 0f;
    [Tooltip("최대 게임 시간(초)")]
    public float maxGameTime = float.MaxValue;
}

