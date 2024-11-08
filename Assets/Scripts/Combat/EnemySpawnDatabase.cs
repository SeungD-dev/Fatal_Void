using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpawnDatabase", menuName = "Scriptable Objects/EnemySpawnDatabase")]
public class EnemySpawnDatabase : ScriptableObject
{
    [Header("Enemy Spawn Settings")]
    public EnemySpawnWeight[] enemySpawnWeights;

    public EnemyData GetRandomEnemy(float gameTime)
    {
        float totalWeight = 0f;

        // 현재 시간에 스폰 가능한 적들의 가중치 합계 계산
        foreach (var spawnWeight in enemySpawnWeights)
        {
            if (gameTime >= spawnWeight.minGameTime && gameTime <= spawnWeight.maxGameTime)
            {
                totalWeight += spawnWeight.spawnWeightOverTime.Evaluate(gameTime);
            }
        }

        if (totalWeight <= 0f)
            return null;

        // 랜덤 가중치 선택
        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // 선택된 가중치에 해당하는 적 반환
        foreach (var spawnWeight in enemySpawnWeights)
        {
            if (gameTime >= spawnWeight.minGameTime && gameTime <= spawnWeight.maxGameTime)
            {
                currentWeight += spawnWeight.spawnWeightOverTime.Evaluate(gameTime);
                if (randomWeight <= currentWeight)
                {
                    return spawnWeight.enemyData;
                }
            }
        }

        return null;
    }
}
