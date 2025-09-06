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
        Random,         // ������ ������ ��ġ���� ����
        EdgeRandom,     // ���� �⺻ ��� - �����ڸ� ����
        Surround,       // �÷��̾� �ֺ��� �������� ����
        Rectangle,      // ���簢�� ���·� ����
        Line,           // ���� ���·� ����
        Fixed           // ������ ���� ����Ʈ ���
    }

    [System.Serializable]
    public class SpawnSettings
    {
        
        public SpawnFormation formation = SpawnFormation.EdgeRandom;

        
        [Tooltip("���� �Ǵ� �簢�� ���� �� �÷��̾�κ����� �Ÿ�")]
        public float surroundDistance = 10f;

        [Tooltip("���� ���� �� ���� ������ (0-360)")]
        [Range(0f, 360f)]
        public float angleOffset = 0f;

        [Tooltip("���� ���� ���� �� ���� ��ġ�� ����")]
        public Vector2 lineStart = new Vector2(-10f, 0f);
        public Vector2 lineEnd = new Vector2(10f, 0f);

        [Tooltip("���� ����Ʈ�� �����Ǵ� �� �� (0: ��� ���� �ϳ��� ��ġ�� ����)")]
        public int enemiesPerSpawnPoint = 1;

        [Tooltip("���� ���� ����Ʈ ��� �� ���� ����Ʈ �ε��� (����θ� ���� ����)")]
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
        public bool isBossWave = false;

        [Header("Time Settings")]
        public float waveDuration = 60f; // ���̺� ���� �ð�(��)
        public float survivalDuration = 15f; // �߰� ���� �ð�(��)

        [Header("Spawn Settings")]
        public float spawnInterval = 1f; // ���� ����(��)
        public int spawnAmount = 3; // �� ���� �����Ǵ� �� ��

        [Header("Spawn Formation")]
        public SpawnSettings spawnSettings = new SpawnSettings();

        [Header("Enemies")]
        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Boss")]
        public EnemyData boss;

        [Header("Rewards")]
        public int coinReward = 10; // ���̺� Ŭ���� �� ���޵Ǵ� ����
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();

    // ������ ���̺� ��ȣ�� ������ ��ȯ
    public Wave GetWave(int waveNumber)
    {
        foreach (Wave wave in waves)
        {
            if (wave.waveNumber == waveNumber)
                return wave;
        }

        // ������ ������ ���̺� ��ȯ
        return waves.Count > 0 ? waves[waves.Count - 1] : null;
    }

    // ���� ���̺� ��ȣ ��������
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

        // �� �̻� ���̺갡 ������ -1 ��ȯ
        return -1;
    }

    // ���� �� ������ ��������
    public EnemyData GetRandomEnemy(Wave wave)
    {
        if (wave == null || wave.enemies.Count == 0)
            return null;

        // �� Ȯ�� ���
        float totalChance = 0f;
        foreach (var enemy in wave.enemies)
        {
            totalChance += enemy.spawnChance;
        }

        // ���� �� ����
        float random = UnityEngine.Random.Range(0, totalChance);
        float currentSum = 0f;

        // ���õ� �� ã��
        foreach (var enemy in wave.enemies)
        {
            currentSum += enemy.spawnChance;
            if (random <= currentSum)
                return enemy.enemyData;
        }

        // �⺻��
        return wave.enemies[0].enemyData;
    }

#if UNITY_EDITOR
    // ������ �̸����� ��ƿ��Ƽ
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

        // �� ���� Ȯ�� �м�
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
        WaveData waveData = (WaveData)target;

        EditorGUI.BeginChangeCheck();

        // Waves 배열 표시
        SerializedProperty wavesProp = serializedObject.FindProperty("waves");
        EditorGUILayout.PropertyField(wavesProp, new GUIContent("Waves"), true);

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        previewWaveNumber = EditorGUILayout.IntField("Wave Number", previewWaveNumber);

        if (GUILayout.Button("Preview Wave"))
        {
            waveData.PreviewWave(previewWaveNumber);
        }
        EditorGUILayout.EndHorizontal();

        // ���� �����̼� �̸����� �߰�
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
        // �̸����� �׸��带 �׸��� ���� ������ ���̾ƿ�
        float gridSize = 200f;
        Rect gridRect = GUILayoutUtility.GetRect(gridSize, gridSize);

        // �̸����� �׸��� �׸���
        Handles.BeginGUI();

        // ���
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));

        // ����
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

        // �߾�(�÷��̾� ��ġ) ǥ��
        Vector2 center = new Vector2(gridRect.x + gridSize / 2, gridRect.y + gridSize / 2);
        float playerSize = 10f;
        Handles.color = Color.white;
        Handles.DrawSolidDisc(center, Vector3.forward, playerSize / 2);

        // ���� ����Ʈ �׸���
        float scale = gridSize / 30f; // 30x30 ������ �׸��忡 �°� �����ϸ�

        for (int i = 0; i < previewPositions.Count; i++)
        {
            Vector2 pos = previewPositions[i];
            Vector2 screenPos = center + new Vector2(pos.x * scale, -pos.y * scale); // y�� ����

            Color pointColor = previewColors[i % previewColors.Length];
            Handles.color = pointColor;

            // ���� ����Ʈ ��
            Handles.DrawSolidDisc(screenPos, Vector3.forward, 5f);

            // ��ȣ ǥ��
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;

            Handles.Label(screenPos, (i + 1).ToString(), style);
        }

        Handles.EndGUI();

        // ���� ǥ��
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Scale: 1 unit = " + (1 / scale).ToString("F1") + " game units");
        EditorGUILayout.EndHorizontal();
    }

    private void GeneratePreviewPoints(WaveData waveData)
    {
        previewPositions.Clear();

        // ������ ���̺� ã��
        WaveData.Wave wave = waveData.GetWave(previewWaveNumber);
        if (wave == null)
        {
            Debug.LogWarning($"Wave {previewWaveNumber} not found!");
            return;
        }

        // ���� ���� ��������
        SpawnSettings settings = wave.spawnSettings;
        int count = wave.spawnAmount;

        // �����̼ǿ� ���� �̸����� ����Ʈ ����
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
                // ���� ���� ����Ʈ�� ���⼭ �̸����� �������� ����
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

        // �簢���� �� ���� ������ �յ��ϰ� ��ġ
        int enemiesPerSide = Mathf.CeilToInt(count / 4f);
        int remainingEnemies = count;

        // ��� ��
        int topCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < topCount; i++)
        {
            float t = (topCount == 1) ? 0.5f : (float)i / (topCount - 1);
            float xPos = -distance + distance * 2 * t;
            float yPos = distance;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= topCount;

        // ���� ��
        int rightCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < rightCount; i++)
        {
            float t = (rightCount == 1) ? 0.5f : (float)i / (rightCount - 1);
            float xPos = distance;
            float yPos = distance - distance * 2 * t;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= rightCount;

        // �ϴ� ��
        int bottomCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < bottomCount; i++)
        {
            float t = (bottomCount == 1) ? 0.5f : (float)i / (bottomCount - 1);
            float xPos = distance - distance * 2 * t;
            float yPos = -distance;
            previewPositions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= bottomCount;

        // ���� ��
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
        // ���� ��ġ ��� ������ ������ ���� ���� ����
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
        // �� �����ڸ��� �ùķ��̼��ϴ� ���� ����
        float mapSize = 15f;

        for (int i = 0; i < count; i++)
        {
            int side = i % 4;
            Vector2 position;

            switch (side)
            {
                case 0: // ���
                    position = new Vector2(UnityEngine.Random.Range(-mapSize, mapSize), mapSize);
                    break;
                case 1: // ����
                    position = new Vector2(mapSize, UnityEngine.Random.Range(-mapSize, mapSize));
                    break;
                case 2: // �ϴ�
                    position = new Vector2(UnityEngine.Random.Range(-mapSize, mapSize), -mapSize);
                    break;
                case 3: // ����
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

        // ��꿡 �ʿ��� ������
        float currentHeight = 0f;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // �����̼� ��Ӵٿ��� ������
        SerializedProperty formationProp = property.FindPropertyRelative("formation");
        SpawnFormation formation = (SpawnFormation)formationProp.enumValueIndex;

        // ���� ǥ��
        Rect titleRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
        showSettings = EditorGUI.Foldout(titleRect, showSettings, label, true);
        currentHeight += lineHeight + spacing;

        if (showSettings)
        {
            // �����̼� ����
            Rect formationRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.PropertyField(formationRect, formationProp, new GUIContent("Formation"));
            currentHeight += lineHeight + spacing;

            // �����̼� ���� ���
            Rect headerRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.LabelField(headerRect, "Formation Settings", EditorStyles.boldLabel);
            currentHeight += lineHeight + spacing;

            // �����̼� �� ���� �Ӽ��� ǥ��
            switch (formation)
            {
                case SpawnFormation.Surround:
                    // Surround �����̼� �Ӽ�
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
                    // Rectangle �����̼� �Ӽ�
                    SerializedProperty rectDistanceProp = property.FindPropertyRelative("surroundDistance");

                    Rect rectDistRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
                    EditorGUI.PropertyField(rectDistRect, rectDistanceProp, new GUIContent("Rectangle Size"));
                    currentHeight += lineHeight + spacing;
                    break;

                case SpawnFormation.Line:
                    // Line �����̼� �Ӽ�
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
                    // Fixed �����̼� �Ӽ�
                    SerializedProperty fixedPointsProp = property.FindPropertyRelative("fixedSpawnPoints");

                    Rect pointsRect = new Rect(position.x, position.y + currentHeight, position.width, EditorGUI.GetPropertyHeight(fixedPointsProp, true));
                    EditorGUI.PropertyField(pointsRect, fixedPointsProp, true);
                    currentHeight += EditorGUI.GetPropertyHeight(fixedPointsProp, true) + spacing;
                    break;
            }

            // ��� �����̼ǿ� �������� �ʿ��� �Ӽ�
            SerializedProperty enemiesPerPointProp = property.FindPropertyRelative("enemiesPerSpawnPoint");

            Rect enemiesPerPointRect = new Rect(position.x, position.y + currentHeight, position.width, lineHeight);
            EditorGUI.PropertyField(enemiesPerPointRect, enemiesPerPointProp);
            currentHeight += lineHeight + spacing;
        }

        // ��ü ���� ����
        propertyHeight = currentHeight;

        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(Wave))]
public class WavePropertyDrawer : PropertyDrawer
{
    private bool foldout = true;
    private float totalHeight = 0f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return totalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float currentY = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Wave number와 Boss Wave 체크박스를 한 줄에 표시
        Rect headerRect = new Rect(position.x, currentY, position.width, lineHeight);
        
        SerializedProperty waveNumberProp = property.FindPropertyRelative("waveNumber");
        SerializedProperty isBossWaveProp = property.FindPropertyRelative("isBossWave");
        
        float halfWidth = position.width * 0.5f;
        Rect waveNumRect = new Rect(position.x, currentY, halfWidth - 5f, lineHeight);
        Rect bossCheckRect = new Rect(position.x + halfWidth + 5f, currentY, halfWidth - 5f, lineHeight);
        
        EditorGUI.PropertyField(waveNumRect, waveNumberProp, new GUIContent("Wave Number"));
        EditorGUI.PropertyField(bossCheckRect, isBossWaveProp, new GUIContent("Boss Wave"));
        
        currentY += lineHeight + spacing;

        bool isBossWave = isBossWaveProp.boolValue;

        // Foldout
        Rect foldoutRect = new Rect(position.x, currentY, position.width, lineHeight);
        foldout = EditorGUI.Foldout(foldoutRect, foldout, isBossWave ? "Boss Wave Settings" : "Normal Wave Settings", true);
        currentY += lineHeight + spacing;

        if (foldout)
        {
            EditorGUI.indentLevel++;

            if (isBossWave)
            {
                // Boss Wave 전용 필드들
                SerializedProperty bossProp = property.FindPropertyRelative("boss");
                
                Rect bossRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.PropertyField(bossRect, bossProp, new GUIContent("Boss"));
                currentY += lineHeight + spacing;
            }
            else
            {
                // Normal Wave 전용 필드들
                EditorGUI.LabelField(new Rect(position.x, currentY, position.width, lineHeight), "Time Settings", EditorStyles.boldLabel);
                currentY += lineHeight + spacing;

                SerializedProperty waveDurationProp = property.FindPropertyRelative("waveDuration");
                SerializedProperty survivalDurationProp = property.FindPropertyRelative("survivalDuration");

                Rect waveDurRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.PropertyField(waveDurRect, waveDurationProp, new GUIContent("Wave Duration"));
                currentY += lineHeight + spacing;

                Rect survivalDurRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.PropertyField(survivalDurRect, survivalDurationProp, new GUIContent("Survival Duration"));
                currentY += lineHeight + spacing;

                EditorGUI.LabelField(new Rect(position.x, currentY, position.width, lineHeight), "Spawn Settings", EditorStyles.boldLabel);
                currentY += lineHeight + spacing;

                SerializedProperty spawnIntervalProp = property.FindPropertyRelative("spawnInterval");
                SerializedProperty spawnAmountProp = property.FindPropertyRelative("spawnAmount");

                Rect intervalRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.PropertyField(intervalRect, spawnIntervalProp, new GUIContent("Spawn Interval"));
                currentY += lineHeight + spacing;

                Rect amountRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.PropertyField(amountRect, spawnAmountProp, new GUIContent("Spawn Amount"));
                currentY += lineHeight + spacing;

                EditorGUI.LabelField(new Rect(position.x, currentY, position.width, lineHeight), "Spawn Formation", EditorStyles.boldLabel);
                currentY += lineHeight + spacing;

                SerializedProperty spawnSettingsProp = property.FindPropertyRelative("spawnSettings");
                float spawnSettingsHeight = EditorGUI.GetPropertyHeight(spawnSettingsProp, true);
                Rect spawnSettingsRect = new Rect(position.x, currentY, position.width, spawnSettingsHeight);
                EditorGUI.PropertyField(spawnSettingsRect, spawnSettingsProp, new GUIContent("Spawn Settings"), true);
                currentY += spawnSettingsHeight + spacing;

                EditorGUI.LabelField(new Rect(position.x, currentY, position.width, lineHeight), "Enemies", EditorStyles.boldLabel);
                currentY += lineHeight + spacing;

                SerializedProperty enemiesProp = property.FindPropertyRelative("enemies");
                float enemiesHeight = EditorGUI.GetPropertyHeight(enemiesProp, true);
                Rect enemiesRect = new Rect(position.x, currentY, position.width, enemiesHeight);
                EditorGUI.PropertyField(enemiesRect, enemiesProp, new GUIContent("Enemies"), true);
                currentY += enemiesHeight + spacing;
            }

            // 공통 Rewards 섹션
            EditorGUI.LabelField(new Rect(position.x, currentY, position.width, lineHeight), "Rewards", EditorStyles.boldLabel);
            currentY += lineHeight + spacing;

            SerializedProperty coinRewardProp = property.FindPropertyRelative("coinReward");
            Rect coinRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(coinRect, coinRewardProp, new GUIContent("Coin Reward"));
            currentY += lineHeight + spacing;

            EditorGUI.indentLevel--;
        }

        totalHeight = currentY - position.y;
        EditorGUI.EndProperty();
    }
}
#endif