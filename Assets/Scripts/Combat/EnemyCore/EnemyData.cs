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

public enum BulletPatternType
{
    Blast,
    Spiral,
    Flower
}

[System.Serializable]
public class BlastPatternSettings
{
    [Header("Blast Pattern")]
    public int shotNum = 5;
    public int volly = 1;
    public float spread = 45f;
    public float shotTime = 0.1f;
    
    [Header("Blast Bullet Properties")]
    public float bulletSpeed = 8f;
    public float bulletLifetime = 10f;
}

[System.Serializable]
public class SpiralPatternSettings
{
    [Header("Spiral Pattern")]
    public int shotNum = 8;
    public int volly = 3;
    public float shotTime = 0.05f;
    public bool clockwise = true;
    
    [Header("Spiral Bullet Properties")]
    public float bulletSpeed = 6f;
    public float bulletLifetime = 12f;
}

[System.Serializable]
public class FlowerPatternSettings
{
    [Header("Flower Pattern")]
    public float flowerTime = 3f;
    public int directions = 6;
    public float rotTime = 10f;
    public float waitTime = 0.1f;
    
    [Header("Flower Bullet Properties")]
    public float bulletSpeed = 5f;
    public float bulletLifetime = 15f;
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
    public float bossDamage; // 투사체 데미지로 사용
    
    [Header("Boss Bullet Pattern Settings")]
    public BulletPatternType bulletPatternType = BulletPatternType.Blast;
    public BlastPatternSettings blastSettings = new BlastPatternSettings();
    public SpiralPatternSettings spiralSettings = new SpiralPatternSettings();
    public FlowerPatternSettings flowerSettings = new FlowerPatternSettings();

    [Header("Pool Settings")]
    public int initialPoolSize = 10;  // �ʱ� Ǯ ������

    [Header("Drop Settings")]
    public EnemyDropTable dropTable;  // �⺻ ��� ���̺� (����ġ/���)

    [Header("Additional Drop Settings")]
    [Range(0f, 100f)]
    public float additionalDropRate;  // �߰� ������ ��� Ȯ��

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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boss Bullet Pattern Settings", EditorStyles.boldLabel);
            enemyData.bulletPatternType = (BulletPatternType)EditorGUILayout.EnumPopup("Pattern Type", enemyData.bulletPatternType);

            EditorGUILayout.Space();
            
            // 선택된 패턴에 따라 설정 표시
            switch (enemyData.bulletPatternType)
            {
                case BulletPatternType.Blast:
                    EditorGUILayout.LabelField("Blast Pattern Settings", EditorStyles.boldLabel);
                    enemyData.blastSettings.shotNum = EditorGUILayout.IntField("Shot Number", enemyData.blastSettings.shotNum);
                    enemyData.blastSettings.volly = EditorGUILayout.IntField("Volly", enemyData.blastSettings.volly);
                    enemyData.blastSettings.spread = EditorGUILayout.FloatField("Spread", enemyData.blastSettings.spread);
                    enemyData.blastSettings.shotTime = EditorGUILayout.FloatField("Shot Time", enemyData.blastSettings.shotTime);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Blast Bullet Properties", EditorStyles.boldLabel);
                    enemyData.blastSettings.bulletSpeed = EditorGUILayout.FloatField("Bullet Speed", enemyData.blastSettings.bulletSpeed);
                    enemyData.blastSettings.bulletLifetime = EditorGUILayout.FloatField("Bullet Lifetime", enemyData.blastSettings.bulletLifetime);
                    break;
                    
                case BulletPatternType.Spiral:
                    EditorGUILayout.LabelField("Spiral Pattern Settings", EditorStyles.boldLabel);
                    enemyData.spiralSettings.shotNum = EditorGUILayout.IntField("Shot Number", enemyData.spiralSettings.shotNum);
                    enemyData.spiralSettings.volly = EditorGUILayout.IntField("Volly", enemyData.spiralSettings.volly);
                    enemyData.spiralSettings.shotTime = EditorGUILayout.FloatField("Shot Time", enemyData.spiralSettings.shotTime);
                    enemyData.spiralSettings.clockwise = EditorGUILayout.Toggle("Clockwise", enemyData.spiralSettings.clockwise);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Spiral Bullet Properties", EditorStyles.boldLabel);
                    enemyData.spiralSettings.bulletSpeed = EditorGUILayout.FloatField("Bullet Speed", enemyData.spiralSettings.bulletSpeed);
                    enemyData.spiralSettings.bulletLifetime = EditorGUILayout.FloatField("Bullet Lifetime", enemyData.spiralSettings.bulletLifetime);
                    break;
                    
                case BulletPatternType.Flower:
                    EditorGUILayout.LabelField("Flower Pattern Settings", EditorStyles.boldLabel);
                    enemyData.flowerSettings.flowerTime = EditorGUILayout.FloatField("Flower Time", enemyData.flowerSettings.flowerTime);
                    enemyData.flowerSettings.directions = EditorGUILayout.IntField("Directions", enemyData.flowerSettings.directions);
                    enemyData.flowerSettings.rotTime = EditorGUILayout.FloatField("Rotation Time", enemyData.flowerSettings.rotTime);
                    enemyData.flowerSettings.waitTime = EditorGUILayout.FloatField("Wait Time", enemyData.flowerSettings.waitTime);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Flower Bullet Properties", EditorStyles.boldLabel);
                    enemyData.flowerSettings.bulletSpeed = EditorGUILayout.FloatField("Bullet Speed", enemyData.flowerSettings.bulletSpeed);
                    enemyData.flowerSettings.bulletLifetime = EditorGUILayout.FloatField("Bullet Lifetime", enemyData.flowerSettings.bulletLifetime);
                    break;
            }
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