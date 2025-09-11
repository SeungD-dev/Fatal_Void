#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BulletHellSystem))]
public class BulletPatternEditor : Editor
{
    private BulletHellSystem bulletSystem;
    private int selectedPatternIndex = 0;
    private bool showPreviewInScene = true;
    private float previewScale = 1f;
    
    // 미리보기용 점들
    private List<Vector3> previewPoints = new List<Vector3>();
    private List<Vector3> previewDirections = new List<Vector3>();
    
    private void OnEnable()
    {
        bulletSystem = (BulletHellSystem)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("패턴 빌더", EditorStyles.boldLabel);
        
        // 패턴 선택
        SerializedProperty patternsProperty = serializedObject.FindProperty("patterns");
        
        if (patternsProperty.arraySize > 0)
        {
            string[] patternNames = new string[patternsProperty.arraySize + 1];
            patternNames[0] = "새 패턴 추가";
            
            for (int i = 0; i < patternsProperty.arraySize; i++)
            {
                var pattern = patternsProperty.GetArrayElementAtIndex(i);
                var nameProperty = pattern.FindPropertyRelative("patternName");
                patternNames[i + 1] = string.IsNullOrEmpty(nameProperty.stringValue) ? 
                    $"패턴 {i + 1}" : nameProperty.stringValue;
            }
            
            int newSelection = EditorGUILayout.Popup("패턴 선택", selectedPatternIndex, patternNames);
            
            if (newSelection == 0) // 새 패턴 추가
            {
                patternsProperty.arraySize++;
                selectedPatternIndex = patternsProperty.arraySize;
                var newPattern = patternsProperty.GetArrayElementAtIndex(patternsProperty.arraySize - 1);
                InitializeNewPattern(newPattern);
            }
            else
            {
                selectedPatternIndex = newSelection;
            }
            
            // 현재 선택된 패턴 편집
            if (selectedPatternIndex > 0 && selectedPatternIndex <= patternsProperty.arraySize)
            {
                EditorGUILayout.Space();
                DrawPatternEditor(patternsProperty.GetArrayElementAtIndex(selectedPatternIndex - 1));
            }
        }
        else
        {
            if (GUILayout.Button("첫 번째 패턴 추가", GUILayout.Height(30)))
            {
                patternsProperty.arraySize = 1;
                selectedPatternIndex = 1;
                var newPattern = patternsProperty.GetArrayElementAtIndex(0);
                InitializeNewPattern(newPattern);
            }
        }
        
        EditorGUILayout.Space(10);
        
        // 미리보기 설정
        EditorGUILayout.LabelField("미리보기 설정", EditorStyles.boldLabel);
        showPreviewInScene = EditorGUILayout.Toggle("Scene에서 미리보기", showPreviewInScene);
        previewScale = EditorGUILayout.Slider("미리보기 크기", previewScale, 0.1f, 3f);
        
        if (GUILayout.Button("패턴 미리보기 새로고침"))
        {
            RefreshPreview();
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            RefreshPreview();
        }
    }
    
    private void DrawPatternEditor(SerializedProperty patternProperty)
    {
        EditorGUILayout.BeginVertical("Box");
        
        // 패턴 이름
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("patternName"));
        
        EditorGUILayout.Space();
        
        // 패턴 타입 (큰 버튼으로 표시)
        var patternTypeProperty = patternProperty.FindPropertyRelative("patternType");
        EditorGUILayout.LabelField("패턴 타입", EditorStyles.boldLabel);
        
        PatternType currentType = (PatternType)patternTypeProperty.enumValueIndex;
        PatternType newType = DrawPatternTypeButtons(currentType);
        
        if (newType != currentType)
        {
            patternTypeProperty.enumValueIndex = (int)newType;
            RefreshPreview();
        }
        
        EditorGUILayout.Space();
        
        // 기본 설정
        EditorGUILayout.LabelField("발사 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("bulletCount"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("bulletSpeed"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("fireRate"));
        
        EditorGUILayout.Space();
        
        // 방향 설정
        EditorGUILayout.LabelField("방향 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("startAngle"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("angleSpread"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("rotationSpeed"));
        
        EditorGUILayout.Space();
        
        // 거리 설정
        EditorGUILayout.LabelField("위치 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("distance"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("distanceVariation"));
        
        EditorGUILayout.Space();
        
        // 특수 설정
        EditorGUILayout.LabelField("특수 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("speedType"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("speedMultiplier"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("lifetime"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("useGravity"));
        
        var useGravityProperty = patternProperty.FindPropertyRelative("useGravity");
        if (useGravityProperty.boolValue)
        {
            EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("gravityStrength"));
        }
        
        EditorGUILayout.Space();
        
        // 미리보기 설정
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("showPreview"));
        EditorGUILayout.PropertyField(patternProperty.FindPropertyRelative("previewColor"));
        
        // 패턴 삭제 버튼
        EditorGUILayout.Space();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("이 패턴 삭제"))
        {
            if (EditorUtility.DisplayDialog("패턴 삭제", "정말로 이 패턴을 삭제하시겠습니까?", "삭제", "취소"))
            {
                var patternsProperty = serializedObject.FindProperty("patterns");
                patternsProperty.DeleteArrayElementAtIndex(selectedPatternIndex - 1);
                selectedPatternIndex = Mathf.Max(1, selectedPatternIndex - 1);
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
    }
    
    private PatternType DrawPatternTypeButtons(PatternType currentType)
    {
        EditorGUILayout.BeginHorizontal();
        
        PatternType[] types = { PatternType.Circle, PatternType.Spiral, PatternType.Line, PatternType.Wave };
        string[] typeNames = { "원형", "나선형", "직선", "파형" };
        
        for (int i = 0; i < types.Length; i++)
        {
            bool isSelected = currentType == types[i];
            GUI.backgroundColor = isSelected ? Color.green : Color.white;
            
            if (GUILayout.Button(typeNames[i], GUILayout.Height(30)))
            {
                GUI.backgroundColor = Color.white;
                return types[i];
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        
        PatternType[] types2 = { PatternType.Burst, PatternType.Cross, PatternType.Random };
        string[] typeNames2 = { "폭발형", "십자형", "랜덤" };
        
        for (int i = 0; i < types2.Length; i++)
        {
            bool isSelected = currentType == types2[i];
            GUI.backgroundColor = isSelected ? Color.green : Color.white;
            
            if (GUILayout.Button(typeNames2[i], GUILayout.Height(30)))
            {
                GUI.backgroundColor = Color.white;
                return types2[i];
            }
        }
        
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
        
        return currentType;
    }
    
    private void InitializeNewPattern(SerializedProperty patternProperty)
    {
        patternProperty.FindPropertyRelative("patternName").stringValue = "새로운 패턴";
        patternProperty.FindPropertyRelative("patternType").enumValueIndex = 0;
        patternProperty.FindPropertyRelative("bulletCount").intValue = 8;
        patternProperty.FindPropertyRelative("bulletSpeed").floatValue = 5f;
        patternProperty.FindPropertyRelative("fireRate").floatValue = 1f;
        patternProperty.FindPropertyRelative("startAngle").floatValue = 0f;
        patternProperty.FindPropertyRelative("angleSpread").floatValue = 360f;
        patternProperty.FindPropertyRelative("rotationSpeed").floatValue = 0f;
        patternProperty.FindPropertyRelative("distance").floatValue = 5f;
        patternProperty.FindPropertyRelative("distanceVariation").floatValue = 0f;
        patternProperty.FindPropertyRelative("lifetime").floatValue = 10f;
        patternProperty.FindPropertyRelative("showPreview").boolValue = true;
        patternProperty.FindPropertyRelative("previewColor").colorValue = Color.red;
    }
    
    private void RefreshPreview()
    {
        if (bulletSystem == null) return;
        
        previewPoints.Clear();
        previewDirections.Clear();
        
        // 현재 선택된 패턴의 미리보기 생성
        var patternsProperty = serializedObject.FindProperty("patterns");
        
        if (selectedPatternIndex > 0 && selectedPatternIndex <= patternsProperty.arraySize)
        {
            var pattern = patternsProperty.GetArrayElementAtIndex(selectedPatternIndex - 1);
            GeneratePreviewPoints(pattern);
        }
        
        SceneView.RepaintAll();
    }
    
    private void GeneratePreviewPoints(SerializedProperty patternProperty)
    {
        Vector3 center = bulletSystem.transform.position;
        PatternType type = (PatternType)patternProperty.FindPropertyRelative("patternType").enumValueIndex;
        int bulletCount = patternProperty.FindPropertyRelative("bulletCount").intValue;
        float startAngle = patternProperty.FindPropertyRelative("startAngle").floatValue;
        float angleSpread = patternProperty.FindPropertyRelative("angleSpread").floatValue;
        float distance = patternProperty.FindPropertyRelative("distance").floatValue;
        
        switch (type)
        {
            case PatternType.Circle:
                GenerateCirclePreview(center, bulletCount, startAngle, angleSpread, distance);
                break;
            case PatternType.Spiral:
                GenerateSpiralPreview(center, bulletCount, startAngle, angleSpread, distance);
                break;
            case PatternType.Line:
                GenerateLinePreview(center, bulletCount, distance);
                break;
            // 다른 패턴들도 추가...
        }
    }
    
    private void GenerateCirclePreview(Vector3 center, int count, float startAngle, float spread, float distance)
    {
        float angleStep = spread / count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 point = center + (Vector3)direction * distance * previewScale;
            
            previewPoints.Add(point);
            previewDirections.Add(direction);
        }
    }
    
    private void GenerateSpiralPreview(Vector3 center, int count, float startAngle, float spread, float distance)
    {
        float angleStep = spread / count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            float spiralDistance = distance + (i * 0.5f);
            Vector3 point = center + (Vector3)direction * spiralDistance * previewScale;
            
            previewPoints.Add(point);
            previewDirections.Add(direction);
        }
    }
    
    private void GenerateLinePreview(Vector3 center, int count, float distance)
    {
        Vector2 perpendicular = Vector2.right; // 기본 방향
        float spacing = distance / count;
        
        for (int i = 0; i < count; i++)
        {
            float offset = (i - count * 0.5f) * spacing;
            Vector3 point = center + (Vector3)perpendicular * offset * previewScale;
            
            previewPoints.Add(point);
            previewDirections.Add(Vector2.up); // 위쪽으로 발사
        }
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreviewInScene || bulletSystem == null || previewPoints.Count == 0)
            return;
        
        var patternsProperty = serializedObject.FindProperty("patterns");
        if (selectedPatternIndex <= 0 || selectedPatternIndex > patternsProperty.arraySize)
            return;
        
        var pattern = patternsProperty.GetArrayElementAtIndex(selectedPatternIndex - 1);
        var colorProperty = pattern.FindPropertyRelative("previewColor");
        Color previewColor = colorProperty.colorValue;
        
        Handles.color = previewColor;
        
        // 발사 지점들 그리기
        for (int i = 0; i < previewPoints.Count; i++)
        {
            Vector3 point = previewPoints[i];
            Vector3 direction = previewDirections[i];
            
            // 발사 지점
            Handles.DrawSolidDisc(point, Vector3.forward, 0.1f * previewScale);
            
            // 발사 방향 화살표
            Handles.color = previewColor * 0.7f;
            Handles.DrawLine(point, point + direction * 1f * previewScale);
            
            // 화살표 끝
            Vector3 arrowEnd = point + direction * 1f * previewScale;
            Vector3 arrowLeft = arrowEnd + Quaternion.Euler(0, 0, 135) * direction * 0.3f * previewScale;
            Vector3 arrowRight = arrowEnd + Quaternion.Euler(0, 0, -135) * direction * 0.3f * previewScale;
            
            Handles.DrawLine(arrowEnd, arrowLeft);
            Handles.DrawLine(arrowEnd, arrowRight);
            
            Handles.color = previewColor;
        }
        
        // 중심점 표시
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(bulletSystem.transform.position, Vector3.forward, 0.5f * previewScale);
    }
}
#endif