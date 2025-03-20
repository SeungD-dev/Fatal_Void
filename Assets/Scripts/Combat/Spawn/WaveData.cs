using UnityEngine;
using System;
using System.Collections.Generic;
using static WaveData;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "WaveData", menuName = "ScriptableObjects/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public enum SpawnFormation
    {
        Random,         // 완전히 랜덤한 위치에서 스폰
        EdgeRandom,     // 현재 기본 방식 - 가장자리 랜덤
        Surround,       // 플레이어 주변을 원형으로 포위
        Rectangle,      // 직사각형 형태로 포위
        Line,           // 직선 형태로 스폰
        Fixed           // 고정된 스폰 포인트 사용
    }

    [System.Serializable]
    public class SpawnSettings
    {
        
        public SpawnFormation formation = SpawnFormation.EdgeRandom;

        
        [Tooltip("원형 또는 사각형 포위 시 플레이어로부터의 거리")]
        public float surroundDistance = 10f;

        [Tooltip("원형 포위 시 각도 오프셋 (0-360)")]
        [Range(0f, 360f)]
        public float angleOffset = 0f;

        [Tooltip("직선 형태 스폰 시 시작 위치와 방향")]
        public Vector2 lineStart = new Vector2(-10f, 0f);
        public Vector2 lineEnd = new Vector2(10f, 0f);

        [Tooltip("스폰 포인트당 스폰되는 적 수 (0: 모든 적을 하나의 위치에 스폰)")]
        public int enemiesPerSpawnPoint = 1;

        [Tooltip("고정 스폰 포인트 사용 시 스폰 포인트 인덱스 (비워두면 랜덤 선택)")]
        public List<int> fixedSpawnPoints = new List<int>();
    }

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

        [Header("Spawn Formation")]
        public SpawnSettings spawnSettings = new SpawnSettings();

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
    private bool showSpawnPreview = false;
    private List<Vector2> previewPositions = new List<Vector2>();
    private Vector2 simulatedPlayerPosition = Vector2.zero;

    private readonly Color[] previewColors = new Color[]
    {
        Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaveData waveData = (WaveData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        previewWaveNumber = EditorGUILayout.IntField("Wave Number", previewWaveNumber);

        if (GUILayout.Button("Preview Wave"))
        {
            waveData.PreviewWave(previewWaveNumber);
        }
        EditorGUILayout.EndHorizontal();

        // 스폰 포메이션 미리보기 추가
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spawn Formation Preview", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        simulatedPlayerPosition = EditorGUILayout.Vector2Field("Player Position", simulatedPlayerPosition);
        showSpawnPreview = EditorGUILayout.Toggle("Show Preview", showSpawnPreview);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Preview Points"))
        {
            GeneratePreviewPoints(waveData);
        }

        if (showSpawnPreview && previewPositions.Count > 0)
        {
            DrawPreviewGrid();
        }
    }

    private void DrawPreviewGrid()
    {
        // 미리보기 그리드를 그리기 위한 간단한 레이아웃
        float gridSize = 200f;
        Rect gridRect = GUILayoutUtility.GetRect(gridSize, gridSize);

        // 미리보기 그리드 그리기
        Handles.BeginGUI();

        // 배경
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

        // 격자
        Handles.color = new Color(0.3f, 0.3f, 0.3f);
        float cellSize = 20f;
        for (float x = 0; x <= gridSize; x += cellSize)
        {
            Handles.DrawLine(
                new Vector3(gridRect.x + x, gridRect.y),
                new Vector3(gridRect.x + x, gridRect.y + gridSize)
            );
        }
        for (float y = 0; y <= gridSize; y += cellSize)
        {
            Handles.DrawLine(
                new Vector3(gridRect.x, gridRect.y + y),
                new Vector3(gridRect.x + gridSize, gridRect.y + y)
            );
        }

        // 중앙(플레이어 위치) 표시
        Vector2 center = new Vector2(gridRect.x + gridSize / 2, gridRect.y + gridSize / 2);
        float playerSize = 10f;
        Handles.color = Color.white;
        Handles.DrawSolidDisc(center, Vector3.forward, playerSize / 2);

        // 스폰 포인트 그리기
        float scale = gridSize / 30f; // 30x30 단위를 그리드에 맞게 스케일링

        for (int i = 0; i < previewPositions.Count; i++)
        {
            Vector2 pos = previewPositions[i];
            Vector2 screenPos = center + new Vector2(pos.x * scale, -pos.y * scale); // y축 반전

            Color pointColor = previewColors[i % previewColors.Length];
            Handles.color = pointColor;

            // 스폰 포인트 원
            Handles.DrawSolidDisc(screenPos, Vector3.forward, 5f);

            // 번호 표시
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;

            Handles.Label(screenPos, (i + 1).ToString(), style);
        }

        Handles.EndGUI();

        // 범례 표시
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Scale: 1 unit = " + (1 / scale).ToString("F1") + " game units");
        EditorGUILayout.EndHorizontal();
    }

    private void GeneratePreviewPoints(WaveData waveData)
    {
        previewPositions.Clear();

        // 선택한 웨이브 찾기
        WaveData.Wave wave = waveData.GetWave(previewWaveNumber);
        if (wave == null)
        {
            Debug.LogWarning($"Wave {previewWaveNumber} not found!");
            return;
        }

        // 스폰 설정 가져오기
        SpawnSettings settings = wave.spawnSettings;
        int count = wave.spawnAmount;

        // 포메이션에 따라 미리보기 포인트 생성
        switch (settings.formation)
        {
            case SpawnFormation.Surround:
                GenerateSurroundPreviewPoints(count, settings);
                break;
            case SpawnFormation.Rectangle:
                GenerateRectanglePreviewPoints(count, settings);
                break;
            case SpawnFormation.Line:
                GenerateLinePreviewPoints(count, settings);
                break;
            case SpawnFormation.Random:
                GenerateRandomPreviewPoints(count);
                break;
            case SpawnFormation.EdgeRandom:
                GenerateEdgeRandomPreviewPoints(count);
                break;
            case SpawnFormation.Fixed:
                // 고정 스폰 포인트는 여기서 미리보기 생성하지 않음
                break;
        }
    }

    private void GenerateSurroundPreviewPoints(int count, SpawnSettings settings)
    {
        float radius = settings.surroundDistance;
        float angleOffset = settings.angleOffset;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep + angleOffset;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            previewPositions.Add(position);
        }
    }

    private void GenerateRectanglePreviewPoints(int count, SpawnSettings settings)
    {
        float distance = settings.surroundDistance;

        // 사각형의 네 변에 적들을 균등하게 배치
        int enemiesPerSide = Mathf.CeilToInt(count / 4f);
        int remainingEnemies = count;

        // 상단 변
        int topCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < topCount; i++)
        {
            float t = (topCount == 1) ? 0.5f : (float)i / (topCount - 1);
            float xPos = -distance + distance * 2 * t;
            float yPos = distance;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= topCount;

        // 우측 변
        int rightCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < rightCount; i++)
        {
            float t = (rightCount == 1) ? 0.5f : (float)i / (rightCount - 1);
            float xPos = distance;
            float yPos = distance - distance * 2 * t;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= rightCount;

        // 하단 변
        int bottomCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < bottomCount; i++)
        {
            float t = (bottomCount == 1) ? 0.5f : (float)i / (bottomCount - 1);
            float xPos = distance - distance * 2 * t;
            float yPos = -distance;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= bottomCount;

        // 좌측 변
        int leftCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < leftCount; i++)
        {
            float t = (leftCount == 1) ? 0.5f : (float)i / (leftCount - 1);
            float xPos = -distance;
            float yPos = -distance + distance * 2 * t;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
    }
    private void GenerateLinePreviewPoints(int count, SpawnSettings settings)
    {
        Vector2 start = settings.lineStart;
        Vector2 end = settings.lineEnd;

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            Vector2 position = Vector2.Lerp(start, end, t);
            previewPositions.Add(position);
        }
    }

    private void GenerateRandomPreviewPoints(int count)
    {
        // 랜덤 위치 대신 고르게 분포된 예시 점들 생성
        for (int i = 0; i < count; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(3f, 10f);
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            previewPositions.Add(position);
        }
    }

    private void GenerateEdgeRandomPreviewPoints(int count)
    {
        // 맵 가장자리를 시뮬레이션하는 예시 점들
        float mapSize = 15f;

        for (int i = 0; i < count; i++)
        {
            int side = i % 4;
            Vector2 position;

            switch (side)
            {
                case 0: // 상단
                    position = new Vector2(UnityEngine.Random.Range(-mapSize, mapSize), mapSize);
                    break;
                case 1: // 우측
                    position = new Vector2(mapSize, UnityEngine.Random.Range(-mapSize, mapSize));
                    break;
                case 2: // 하단
                    position = new Vector2(UnityEngine.Random.Range(-mapSize, mapSize), -mapSize);
                    break;
                case 3: // 좌측
                default:
                    position = new Vector2(-mapSize, UnityEngine.Random.Range(-mapSize, mapSize));
                    break;
            }

            previewPositions.Add(position);
        }
    }
}
#endif
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SpawnSettings))]
public class SpawnSettingsDrawer : PropertyDrawer
{
    private bool showSettings = true;
    private float propertyHeight = 0f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return propertyHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 계산에 필요한 변수들
        float currentHeight = 0f;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // 포메이션 드롭다운을 가져옴
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SpawnFormation formation = (SpawnFormation)formationProp.enumValueIndex;

        // 제목 표시
        Rect titleRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
        showSettings = EditorGUI.Foldout(titleRect, showSettings, label, true);
        currentHeight += lineHeight + spacing;

        if (showSettings)
        {
            // 포메이션 선택
            Rect formationRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.PropertyField(formationRect, formationProp, new GUIContent("Formation"));
            currentHeight += lineHeight + spacing;

            // 포메이션 설정 헤더
            Rect headerRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.LabelField(headerRect, "Formation Settings", EditorStyles.boldLabel);
            currentHeight += lineHeight + spacing;

            // 포메이션 별 관련 속성만 표시
            switch (formation)
            {
                case SpawnFormation.Surround:
                    // Surround 포메이션 속성
                    SerializedProperty surroundDistanceProp = property.FindPropertyRelative("surroundDistance");
                    SerializedProperty angleOffsetProp = property.FindPropertyRelative("angleOffset");

                    Rect distanceRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(distanceRect, surroundDistanceProp);
                    currentHeight += lineHeight + spacing;

                    Rect angleRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(angleRect, angleOffsetProp);
                    currentHeight += lineHeight + spacing;
                    break;

                case SpawnFormation.Rectangle:
                    // Rectangle 포메이션 속성
                    SerializedProperty rectDistanceProp = property.FindPropertyRelative("surroundDistance");

                    Rect rectDistRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(rectDistRect, rectDistanceProp, new GUIContent("Rectangle Size"));
                    currentHeight += lineHeight + spacing;
                    break;

                case SpawnFormation.Line:
                    // Line 포메이션 속성
                    SerializedProperty lineStartProp = property.FindPropertyRelative("lineStart");
                    SerializedProperty lineEndProp = property.FindPropertyRelative("lineEnd");

                    Rect startRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(startRect, lineStartProp);
                    currentHeight += lineHeight + spacing;

                    Rect endRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(endRect, lineEndProp);
                    currentHeight += lineHeight + spacing;
                    break;

                case SpawnFormation.Fixed:
                    // Fixed 포메이션 속성
                    SerializedProperty fixedPointsProp = property.FindPropertyRelative("fixedSpawnPoints");

                    Rect pointsRect = new Rect(position.x, position.y + currentHeight, position.width, EditorGUI.GetPropertyHeight(fixedPointsProp, true));
                    EditorGUI.PropertyField(pointsRect, fixedPointsProp, true);
                    currentHeight += EditorGUI.GetPropertyHeight(fixedPointsProp, true) + spacing;
                    break;
            }

            // 모든 포메이션에 공통으로 필요한 속성
            SerializedProperty enemiesPerPointProp = property.FindPropertyRelative("enemiesPerSpawnPoint");

            Rect enemiesPerPointRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.PropertyField(enemiesPerPointRect, enemiesPerPointProp);
            currentHeight += lineHeight + spacing;
        }

        // 전체 높이 설정
        propertyHeight = currentHeight;

        EditorGUI.EndProperty();
    }
}
#endif