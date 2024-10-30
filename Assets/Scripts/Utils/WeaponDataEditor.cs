// WeaponDataEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private bool[,] editingShape;
    private Vector2Int gridSize = new Vector2Int(3, 3);

    public override void OnInspectorGUI()
    {
        WeaponData weaponData = (WeaponData)target;

        // 기본 인스펙터 프로퍼티들 표시
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Weapon Shape Editor", EditorStyles.boldLabel);

        // 그리드 크기 조절
        gridSize = EditorGUILayout.Vector2IntField("Grid Size", gridSize);
        if (gridSize.x < 1) gridSize.x = 1;
        if (gridSize.y < 1) gridSize.y = 1;

        // 초기 shape 배열이 없거나 크기가 다르면 새로 생성
        if (editingShape == null ||
            editingShape.GetLength(0) != gridSize.x ||
            editingShape.GetLength(1) != gridSize.y)
        {
            editingShape = new bool[gridSize.x, gridSize.y];

            // 기존 데이터가 있다면 복사
            if (!string.IsNullOrEmpty(weaponData.shapeLayout))
            {
                string[] rows = weaponData.shapeLayout.Split('/');
                for (int y = 0; y < Mathf.Min(rows.Length, gridSize.y); y++)
                {
                    string[] cols = rows[y].Split(',');
                    for (int x = 0; x < Mathf.Min(cols.Length, gridSize.x); x++)
                    {
                        editingShape[x, y] = cols[x] == "1";
                    }
                }
            }
        }

        // 그리드 에디터 그리기
        EditorGUILayout.Space();
        var rect = GUILayoutUtility.GetRect(200, 200);
        float cellSize = Mathf.Min(rect.width / gridSize.x, rect.height / gridSize.y);

        for (int y = 0; y < gridSize.y; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++)
            {
                // 토글 버튼으로 셀 상태 변경
                editingShape[x, y] = EditorGUILayout.Toggle(editingShape[x, y],
                    GUILayout.Width(cellSize), GUILayout.Height(cellSize));
            }
            EditorGUILayout.EndHorizontal();
        }

        // 변경사항 적용 버튼
        if (GUILayout.Button("Apply Shape"))
        {
            // bool[,] 배열을 문자열로 변환
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int y = 0; y < gridSize.y; y++)
            {
                if (y > 0) sb.Append('/');
                for (int x = 0; x < gridSize.x; x++)
                {
                    if (x > 0) sb.Append(',');
                    sb.Append(editingShape[x, y] ? "1" : "0");
                }
            }

            // Undo 등록
            Undo.RecordObject(weaponData, "Update Weapon Shape");

            // 데이터 업데이트
            weaponData.shapeLayout = sb.ToString();
            weaponData.size = gridSize;

            // 에셋 저장
            EditorUtility.SetDirty(weaponData);
        }
    }
}
#endif
