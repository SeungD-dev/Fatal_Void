using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "WaveData", menuName = "ScriptableObjects/WaveData")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public class WaveEnemy
    {
        public EnemyData enemyData;
        [Range(0, 100)]
        public float spawnChance = 100f;
    }

    [Serializable]
    public class Wave
    {
        [Header("Wave Settings")]
        public int waveNumber;

        [Header("Time Settings")]
        public float waveDuration = 60f; // 웨이브 지속 시간(초)
        public float survivalDuration = 15f; // 추가 생존 시간(초)

        [Header("Spawn Settings")]
        public float spawnInterval = 1f; // 스폰 간격(초)
        public int spawnAmount = 3; // 한 번에 스폰되는 적 수

        [Header("Enemies")]
        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Rewards")]
        public int coinReward = 10; // 웨이브 클리어 시 지급되는 코인
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();

    // 지정된 웨이브 번호의 데이터 반환
    public Wave GetWave(int waveNumber)
    {
        foreach (Wave wave in waves)
        {
            if (wave.waveNumber == waveNumber)
                return wave;
        }

        // 없으면 마지막 웨이브 반환
        return waves.Count > 0 ? waves[waves.Count - 1] : null;
    }

    // 다음 웨이브 번호 가져오기
    public int GetNextWaveNumber(int currentWaveNumber)
    {
        int nextWaveNumber = currentWaveNumber + 1;

        foreach (Wave wave in waves)
        {
            if (wave.waveNumber == nextWaveNumber)
            {
                return nextWaveNumber;
            }
        }

        // 더 이상 웨이브가 없으면 -1 반환
        return -1;
    }

    // 랜덤 적 데이터 가져오기
    public EnemyData GetRandomEnemy(Wave wave)
    {
        if (wave == null || wave.enemies.Count == 0)
            return null;

        // 총 확률 계산
        float totalChance = 0f;
        foreach (var enemy in wave.enemies)
        {
            totalChance += enemy.spawnChance;
        }

        // 랜덤 값 생성
        float random = UnityEngine.Random.Range(0, totalChance);
        float currentSum = 0f;

        // 선택된 적 찾기
        foreach (var enemy in wave.enemies)
        {
            currentSum += enemy.spawnChance;
            if (random <= currentSum)
                return enemy.enemyData;
        }

        // 기본값
        return wave.enemies[0].enemyData;
    }

#if UNITY_EDITOR
    // 에디터 미리보기 유틸리티
    public void PreviewWave(int waveNumber)
    {
        Wave wave = GetWave(waveNumber);
        if (wave == null)
        {
            Debug.LogWarning($"Wave {waveNumber} not found!");
            return;
        }

        Debug.Log($"Wave {waveNumber} Preview:" +
                  $"\nDuration: {wave.waveDuration}s + {wave.survivalDuration}s survival" +
                  $"\nSpawn Interval: {wave.spawnInterval}s" +
                  $"\nSpawn Amount: {wave.spawnAmount} enemies per spawn" +
                  $"\nEnemies: {wave.enemies.Count} types");

        // 적 스폰 확률 분석
        float totalChance = 0;
        foreach (var enemy in wave.enemies)
        {
            totalChance += enemy.spawnChance;
        }

        foreach (var enemy in wave.enemies)
        {
            float actualChance = enemy.spawnChance / totalChance * 100;
            Debug.Log($"- {enemy.enemyData.enemyName}: {actualChance:F1}% chance");
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private int previewWaveNumber = 1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaveData waveData = (WaveData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

        previewWaveNumber = EditorGUILayout.IntField("Wave Number", previewWaveNumber);

        if (GUILayout.Button("Preview Wave"))
        {
            waveData.PreviewWave(previewWaveNumber);
        }
    }
}
#endif