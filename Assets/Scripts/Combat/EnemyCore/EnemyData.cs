using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum EnemyType
{
    Hunter,
    Walker,
    Heavy,
    Crawler,
    Brute,
    Wisp,
    Boss
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Prefab Reference")]
    public GameObject enemyPrefab;  // �ν��Ͻ�ȭ�� �� ������

    [Header("Enemy Info")]
    public string enemyName;
    public Sprite enemySprite;
    public EnemyType enemyType;

    [Header("Base Stats")]
    public float baseHealth;
    public float maxPossibleHealth;
    public float baseDamage;
    public float moveSpeed;

    [Header("Boss Stats")]
    public GameObject bossPrefab;
    public float bossHealth;
    public float bossDamage;

    [Header("Pool Settings")]
    public int initialPoolSize = 10;  // �ʱ� Ǯ ������

    [Header("Drop Settings")]
    public EnemyDropTable dropTable;  // �⺻ ��� ���̺� (����ġ/���)

    [Header("Additional Drop Settings")]
    [Range(0f, 100f)]
    public float additionalDropRate;  // �߰� ������ ��� Ȯ��

    private void OnValidate()
    {
        // ������ ��ȿ�� ����
        if (baseHealth <= 0)
            Debug.LogError($"Invalid base health for {enemyName}: must be greater than 0");

        if (maxPossibleHealth < baseHealth)
            Debug.LogError($"Invalid max possible health for {enemyName}: must be greater than or equal to base health");

        if (baseDamage < 0)
            Debug.LogError($"Invalid base damage for {enemyName}: must be greater than or equal to 0");

        if (moveSpeed <= 0)
            Debug.LogError($"Invalid move speed for {enemyName}: must be greater than 0");

        if (initialPoolSize <= 0)
            Debug.LogError($"Invalid initial pool size for {enemyName}: must be greater than 0");

        if (dropTable == null)
            Debug.LogWarning($"No drop table assigned for {enemyName}");

        if (enemyPrefab == null)
            Debug.LogError($"No prefab assigned for {enemyName}");

        if (enemySprite == null)
            Debug.LogWarning($"No sprite assigned for {enemyName}");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyData enemyData = (EnemyData)target;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Prefab Reference", EditorStyles.boldLabel);
        enemyData.enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", enemyData.enemyPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy Info", EditorStyles.boldLabel);
        enemyData.enemyName = EditorGUILayout.TextField("Enemy Name", enemyData.enemyName);
        enemyData.enemySprite = (Sprite)EditorGUILayout.ObjectField("Enemy Sprite", enemyData.enemySprite, typeof(Sprite), false);
        enemyData.enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", enemyData.enemyType);

        EditorGUILayout.Space();

        bool isBoss = enemyData.enemyType == EnemyType.Boss;

        if (isBoss)
        {
            EditorGUILayout.LabelField("Boss Prefab Reference", EditorStyles.boldLabel);
            enemyData.bossPrefab = (GameObject)EditorGUILayout.ObjectField("Boss Prefab", enemyData.bossPrefab, typeof(GameObject), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boss Stats", EditorStyles.boldLabel);
            enemyData.bossHealth = EditorGUILayout.FloatField("Health", enemyData.bossHealth);
            enemyData.bossDamage = EditorGUILayout.FloatField("Damage", enemyData.bossDamage);
        }
        else
        {
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
            enemyData.baseHealth = EditorGUILayout.FloatField("Base Health", enemyData.baseHealth);
            enemyData.maxPossibleHealth = EditorGUILayout.FloatField("Max Possible Health", enemyData.maxPossibleHealth);
            enemyData.baseDamage = EditorGUILayout.FloatField("Base Damage", enemyData.baseDamage);
            enemyData.moveSpeed = EditorGUILayout.FloatField("Move Speed", enemyData.moveSpeed);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pool Settings", EditorStyles.boldLabel);
            enemyData.initialPoolSize = EditorGUILayout.IntField("Initial Pool Size", enemyData.initialPoolSize);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Drop Settings", EditorStyles.boldLabel);
            enemyData.dropTable = (EnemyDropTable)EditorGUILayout.ObjectField("Drop Table", enemyData.dropTable, typeof(EnemyDropTable), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Drop Settings", EditorStyles.boldLabel);
            enemyData.additionalDropRate = EditorGUILayout.Slider("Additional Drop Rate", enemyData.additionalDropRate, 0f, 100f);
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(enemyData);
        }
    }
}
#endif