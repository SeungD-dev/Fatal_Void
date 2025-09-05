using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TierStats
{
    [Header("Stats")]
    [Tooltip("�ش� Ƽ���� �⺻ ������")]
    public float damage = 10f;
    [Tooltip("�ش� Ƽ���� �⺻ ���� ������")]
    public float attackDelay = 1f;
    [Tooltip("�ش� Ƽ���� ����ü �ӵ�")]
    public float projectileSpeed = 10f;
    [Tooltip("�ش� Ƽ���� �˹� ��ġ")]
    public float knockback = 1f;
    [Tooltip("�ش� Ƽ���� ����ü ũ��")]
    public float projectileSize = 1f;
    [Tooltip("�ش� Ƽ���� ��Ÿ�")]
    public float range = 5f;

    [Header("Projectile Properties")]
    [Tooltip("true�� ��� ����ü�� ���� �����մϴ�")]
    public bool canPenetrate = false;
    [Tooltip("���� ������ �ִ� �� �� (0 = ����)")]
    public int maxPenetrationCount = 0;
    [Tooltip("����� ������ ������ (0.1 = 10% ����)")]
    public float penetrationDamageDecay = 0.1f;

    public int projectileCount = 3;
    public float spreadAngle = 45f;

    [Header("Grinder Properties")]
    [Tooltip("���� ���� ����")]
    public float attackRadius = 2f;
    [Tooltip("���� ���� �ð�")]
    public float groundEffectDuration = 3f;
    [Tooltip("���� ����� ƽ ����")]
    public float damageTickInterval = 0.5f;

    [Header("Force Field Properties")]
    [Tooltip("���� �ʵ� ���� ����")]
    public float forceFieldRadius = 3f;
    [Tooltip("���� �ʵ� ����� ƽ ����")]
    public float forceFieldTickInterval = 0.5f;

    [Header("Machinegun Properties")]
    [Tooltip("�Ӽ� �ߣ� ����")]
    public int burstCount = 1;
    [Tooltip("�Ӽ� �߻� �� ���� ������ (��)")]
    public float burstDelay = 0.1f;
    [Tooltip("�˹� ���� (1.0 = �⺻��)")]
    public float knockbackMultiplier = 1.0f;

    [Header("Cutter Properties")]
    [Tooltip("����ü ���� ���� (��)")]
    public float cutterSpreadAngle = 15f;

    [Header("Phantom Saber Properties")]
    [Tooltip("������ ����")]
    public float phantomSaberDamageMultiplier = 1.5f;
    [Tooltip("���� ���� ����")]
    public float phantomSaberRangeMultiplier = 1.3f;
    [Tooltip("Power ������")]
    public float phantomSaberPowerBonus = 15f;
    [Tooltip("Haste ������")]
    public float phantomSaberHasteBonus = 15f;
    [Tooltip("�̵��ӵ� ������")]
    public float phantomSaberSpeedBonus = 25f;

    // Black Hole (Grinder X-Tier) 속성들
    [Header("Black Hole Properties")]
    public float blackHoleDamageInterval = 0.25f;
    public float blackHoleScaleMultiplier = 1.5f;
    public float blackHoleDuration = 5f;

    public struct PenetrationInfo
    {
        public bool canPenetrate;
        public int maxCount;
        public float damageDecay;
    }

    public PenetrationInfo GetPenetrationInfo() => new PenetrationInfo
    {
        canPenetrate = canPenetrate,
        maxCount = maxPenetrationCount,
        damageDecay = penetrationDamageDecay
    };
}

[System.Serializable]
public class EquipmentStats
{
    [Header("Power Upper Settings")]
    [Tooltip("���ݷ� ������")]
    public float powerIncrease;

    [Header("Speed Upper Settings")]
    [Tooltip("�̵��ӵ� ������")]
    public float speedIncrease;
    [Tooltip("��ٿ� ���ҷ� (4Ƽ�� ����)")]
    public float hasteIncrease;

    [Header("Health Upper Settings")]
    [Tooltip("�ִ� ü�� ������")]
    public float healthIncrease;
    [Tooltip("ü�� ��� ������ (4Ƽ�� ����)")]
    public float regenIncrease;

    [Header("Haste Upper Settings")]
    [Tooltip("��ٿ� ���ҷ�")]
    public float hasteValue;

    [Header("Portable Magnet Settings")]
    [Tooltip("������ ȹ�� ���� ������ (����)")]
    public float pickupRangeIncrease;
    [Header("Portable Magnet Additional Effect")]
    [Tooltip("4Ƽ�� �ڵ� �ڼ� ȿ�� Ȱ��ȭ")]
    public bool enableAutoMagnet = false;

    [Header("Knockback Upper Settings")]
    [Tooltip("�˹� ������")]
    public float knockbackIncrease;
    [Tooltip("���ݷ� ������ (4Ƽ�� ����)")]
    public float knockbackPowerIncrease;

    [Header("Regen Upper Settings")]
    [Tooltip("ü�� ��� ������")]
    public float regenValue;
    [Tooltip("��ٿ� ���ҷ� (4Ƽ�� ����)")]
    public float regenHasteIncrease;
}

#if UNITY_EDITOR
[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private SerializedProperty width;
    private SerializedProperty height;
    private SerializedProperty weaponIcon;
    private SerializedProperty inventoryWeaponIcon;
    private SerializedProperty weaponType;
    private SerializedProperty price;
    private SerializedProperty weaponName;
    private SerializedProperty weaponDescription;
    private SerializedProperty sellPriceRatio;
    private SerializedProperty currentTier;
    private SerializedProperty projectilePrefab;
    private SerializedProperty tierStats;
    private SerializedProperty tier1Price;
    private SerializedProperty tier2Price;
    private SerializedProperty tier3Price;
    private SerializedProperty tier4Price;
    private SerializedProperty equipmentType;
    private SerializedProperty equipmentTierStats;
    private SerializedProperty supportsXTier;
    private SerializedProperty xTierStats;
    private SerializedProperty xTierPrice;
    private SerializedProperty xTierProjectilePrefab;
    private SerializedProperty xTierWeaponIcon;
    private SerializedProperty xTierInventoryWeaponIcon;
    private SerializedProperty xTierWidth;
    private SerializedProperty xTierHeight;
    private SerializedProperty xTierWeaponName;
    private SerializedProperty xTierWeaponDescription;
    private void OnEnable()
    {
        width = serializedObject.FindProperty("width");
        height = serializedObject.FindProperty("height");
        weaponIcon = serializedObject.FindProperty("weaponIcon");
        inventoryWeaponIcon = serializedObject.FindProperty("inventoryWeaponIcon");
        weaponType = serializedObject.FindProperty("weaponType");
        price = serializedObject.FindProperty("price");
        weaponName = serializedObject.FindProperty("weaponName");
        weaponDescription = serializedObject.FindProperty("weaponDescription");
        sellPriceRatio = serializedObject.FindProperty("sellPriceRatio");
        currentTier = serializedObject.FindProperty("currentTier");
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        tierStats = serializedObject.FindProperty("tierStats");
        tier1Price = serializedObject.FindProperty("tier1Price");
        tier2Price = serializedObject.FindProperty("tier2Price");
        tier3Price = serializedObject.FindProperty("tier3Price");
        tier4Price = serializedObject.FindProperty("tier4Price");
        equipmentType = serializedObject.FindProperty("equipmentType");
        equipmentTierStats = serializedObject.FindProperty("equipmentTierStats");
        supportsXTier = serializedObject.FindProperty("supportsXTier");
        xTierStats = serializedObject.FindProperty("xTierStats");
        xTierPrice = serializedObject.FindProperty("xTierPrice");
        xTierProjectilePrefab = serializedObject.FindProperty("xTierProjectilePrefab");
        xTierWeaponIcon = serializedObject.FindProperty("xTierWeaponIcon");
        xTierInventoryWeaponIcon = serializedObject.FindProperty("xTierInventoryWeaponIcon");
        xTierWidth = serializedObject.FindProperty("xTierWidth");
        xTierHeight = serializedObject.FindProperty("xTierHeight");
        xTierWeaponName = serializedObject.FindProperty("xTierWeaponName");
        xTierWeaponDescription = serializedObject.FindProperty("xTierWeaponDescription");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        WeaponData weaponData = (WeaponData)target;

        DrawBasicSettings();
        EditorGUILayout.Space();
        DrawTierPrices();
        DrawTierConfiguration();
        EditorGUILayout.Space();

        // X-Tier ���� �߰�
        DrawXTierConfiguration();
        EditorGUILayout.Space();

        // WeaponType�� Equipment�� ���� Equipment ������, �ƴ� ���� �Ϲ� ���� ������ ������
        if (weaponData.weaponType == WeaponType.Equipment)
        {
            DrawEquipmentSettings();
        }
        else
        {
            DrawTierStats(weaponData);

            // X-Tier ���� ���� (Ȱ��ȭ�� ��쿡��)
            if (weaponData.supportsXTier)
            {
                DrawXTierStats(weaponData);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicSettings()
    {
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(width);
        EditorGUILayout.PropertyField(height);
        EditorGUILayout.PropertyField(weaponIcon);
        EditorGUILayout.PropertyField(inventoryWeaponIcon);
        EditorGUILayout.PropertyField(weaponType);

        if (weaponType.enumValueIndex == (int)WeaponType.Equipment)
        {
            EditorGUILayout.PropertyField(equipmentType);
        }

        EditorGUILayout.PropertyField(weaponName);
        EditorGUILayout.PropertyField(weaponDescription);
        EditorGUILayout.PropertyField(sellPriceRatio);
    }

    private void DrawTierConfiguration()
    {
        EditorGUILayout.LabelField("Tier Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(currentTier);

        if (weaponType.enumValueIndex != (int)WeaponType.Equipment)
        {
            EditorGUILayout.PropertyField(projectilePrefab);
        }
    }

    private void DrawTierPrices()
    {
        EditorGUILayout.LabelField("Tier Prices", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(tier1Price, new GUIContent("Tier 1 Price"));
        EditorGUILayout.PropertyField(tier2Price, new GUIContent("Tier 2 Price"));
        EditorGUILayout.PropertyField(tier3Price, new GUIContent("Tier 3 Price"));
        EditorGUILayout.PropertyField(tier4Price, new GUIContent("Tier 4 Price"));

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    private void DrawEquipmentSettings()
    {
        if (equipmentTierStats == null) return;

        EditorGUILayout.LabelField("Equipment Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < 4; i++)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Tier {i + 1}", EditorStyles.boldLabel);

                SerializedProperty tierStat = equipmentTierStats.GetArrayElementAtIndex(i);
                WeaponData weaponData = (WeaponData)target;

                switch (weaponData.equipmentType)
                {
                    case EquipmentType.PowerUpper:
                        DrawEquipmentProperty(tierStat, "powerIncrease", "Power Increase", "���ݷ� ������");
                        break;
                    case EquipmentType.SpeedUpper:
                        DrawEquipmentProperty(tierStat, "speedIncrease", "Speed Increase", "�̵��ӵ� ������");
                        if (i == 3) // 4Ƽ��
                        {
                            DrawEquipmentProperty(tierStat, "hasteIncrease", "Haste Increase", "��ٿ� ���ҷ�");
                        }
                        break;
                    case EquipmentType.HealthUpper:
                        DrawEquipmentProperty(tierStat, "healthIncrease", "Health Increase", "ü�� ������");
                        if (i == 3) // 4Ƽ��
                        {
                            DrawEquipmentProperty(tierStat, "regenIncrease", "Regen Increase", "ü�� ��� ������");
                        }
                        break;
                    case EquipmentType.HasteUpper:
                        DrawEquipmentProperty(tierStat, "hasteValue", "Haste Value", "��ٿ� ���ҷ�");
                        break;
                    case EquipmentType.PortableMagnet:
                        DrawEquipmentProperty(tierStat, "pickupRangeIncrease", "Pickup Range Increase", "������ ȹ�� ���� ������");
                        if (i == 3) // 4Ƽ��
                        {
                            DrawEquipmentProperty(tierStat, "enableAutoMagnet", "Auto Magnet", "�ڵ� �ڼ� ȿ�� Ȱ��ȭ");
                        }
                        break;
                    case EquipmentType.KnockbackUpper:
                        DrawEquipmentProperty(tierStat, "knockbackIncrease", "Knockback Increase", "�˹� ������");
                        if (i == 3) // 4Ƽ��
                        {
                            DrawEquipmentProperty(tierStat, "knockbackPowerIncrease", "Power Increase", "���ݷ� ������");
                        }
                        break;
                    case EquipmentType.RegenUpper:
                        DrawEquipmentProperty(tierStat, "regenValue", "Regen Value", "ü�� ��� ������");
                        if (i == 3) // 4Ƽ��
                        {
                            DrawEquipmentProperty(tierStat, "regenHasteIncrease", "Haste Increase", "��ٿ� ���ҷ�");
                        }
                        break;
                }
            }

            EditorGUILayout.Space();
        }

        EditorGUI.indentLevel--;
    }

    private void DrawEquipmentProperty(SerializedProperty tierStat, string propertyName, string label, string tooltip)
    {
        var property = tierStat.FindPropertyRelative(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
        }
    }

    private void DrawTierStats(WeaponData weaponData)
    {
        EditorGUILayout.LabelField("Tier Stats", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        for (int i = 0; i < tierStats.arraySize; i++)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty tierStat = tierStats.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField($"Tier {i + 1}", EditorStyles.boldLabel);

                // �⺻ ���ȵ�
                EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("damage"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("attackDelay"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileSpeed"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("knockback"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileSize"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("range"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Projectile Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("canPenetrate"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("maxPenetrationCount"));
                EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("penetrationDamageDecay"));

                // ���� Ÿ�Ժ� �߰� �Ӽ�
                if (weaponData.weaponType == WeaponType.Shotgun)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Shotgun Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileCount"),
                        new GUIContent("Projectile Count", "������ �߻� ����ü ��"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("spreadAngle"),
                        new GUIContent("Spread Angle", "������ �߻� ���� ���� (��)"));
                }
                else if (weaponData.weaponType == WeaponType.Grinder)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Grinder Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("attackRadius"),
                        new GUIContent("Attack Radius", "���� ���� ����"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("groundEffectDuration"),
                        new GUIContent("Ground Effect Duration", "���� ���� �ð�"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("damageTickInterval"),
                        new GUIContent("Damage Tick Interval", "���� ����� ƽ ����"));
                    
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("blackHoleDamageInterval"),
                        new GUIContent("Black Hole Damage Interval", "BlackHole X-Tier 데미지 주기"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("blackHoleScaleMultiplier"),
                        new GUIContent("Black Hole Scale Multiplier", "BlackHole X-Tier 최대 확장 배율"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("blackHoleDuration"),
                        new GUIContent("Black Hole Duration", "BlackHole X-Tier 지속시간"));
                }
                else if (weaponData.weaponType == WeaponType.Machinegun)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Machinegun Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("burstCount"),
                        new GUIContent("Burst Count", "�Ӽ� �߻� ����"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("burstDelay"),
                        new GUIContent("Burst Delay", "�Ӽ� �߻� �� ���� ������ (��)"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("spreadAngle"),
                        new GUIContent("Spread Angle", "�� ���� ���� (��)"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("knockbackMultiplier"),
                        new GUIContent("Knockback Multiplier", "�˹� ���� (1.0 = �⺻��)"));
                }
                else if (weaponData.weaponType == WeaponType.Cutter)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Cutter Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("cutterSpreadAngle"),
                        new GUIContent("Spread Angle", "투사체 퍼짐 각도 (도)"));
                }
                else if (weaponData.weaponType == WeaponType.BeamSaber)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Phantom Saber Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("phantomSaberDamageMultiplier"),
                        new GUIContent("Damage Multiplier", "데미지 배율"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("phantomSaberRangeMultiplier"),
                        new GUIContent("Range Multiplier", "공격 범위 배율"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("phantomSaberPowerBonus"),
                        new GUIContent("Power Bonus", "Power 증가량"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("phantomSaberHasteBonus"),
                        new GUIContent("Haste Bonus", "Haste 증가량"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("phantomSaberSpeedBonus"),
                        new GUIContent("Speed Bonus", "이동속도 증가량"));
                }
                else if (weaponData.weaponType == WeaponType.ForceFieldGenerator)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Force Field Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("forceFieldRadius"),
                        new GUIContent("Force Field Radius", "���� �ʵ��� ���� ����"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("forceFieldTickInterval"),
                        new GUIContent("Damage Tick Interval", "������� ����Ǵ� �ð� ����"));
                }
            }

            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;
    }

    // X-Tier ���� UI �׸���
    private void DrawXTierConfiguration()
    {
        EditorGUILayout.LabelField("X-Tier Configuration", EditorStyles.boldLabel);

        // X-Tier ���� ���� ���
        EditorGUILayout.PropertyField(supportsXTier, new GUIContent("Supports X-Tier", "�� ���Ⱑ X-Tier ���׷��̵带 �����ϴ��� ����"));

        // X-Tier�� Ȱ��ȭ�� ��쿡�� �߰� ���� ǥ��
        if (supportsXTier.boolValue)
        {
            EditorGUI.indentLevel++;

            // X-Tier �⺻ ����
            EditorGUILayout.LabelField("X-Tier Basic Settings", EditorStyles.boldLabel);

            // ũ�� ����
            EditorGUILayout.PropertyField(xTierWidth, new GUIContent("X-Tier Width", "X-Tier ���� �ʺ�"));
            EditorGUILayout.PropertyField(xTierHeight, new GUIContent("X-Tier Height", "X-Tier ���� ����"));

            // ������ ����
            EditorGUILayout.PropertyField(xTierWeaponIcon, new GUIContent("X-Tier Weapon Icon", "X-Tier ���� ������"));
            EditorGUILayout.PropertyField(xTierInventoryWeaponIcon, new GUIContent("X-Tier Inventory Icon", "X-Tier �κ��丮 ������"));

            // �̸� �� ����
            EditorGUILayout.PropertyField(xTierWeaponName, new GUIContent("X-Tier Name", "X-Tier ���� �̸� (����θ� �⺻ ���� ���)"));
            EditorGUILayout.PropertyField(xTierWeaponDescription, new GUIContent("X-Tier Description", "X-Tier ���� ����"));

            // X-Tier ����
            EditorGUILayout.PropertyField(xTierPrice, new GUIContent("X-Tier Price", "X-Tier ���� ����"));

            // X-Tier ���� ����ü ������ (�Ϲ� ���⸸ �ش�)
            WeaponData weaponData = (WeaponData)target;
            if (weaponData.weaponType != WeaponType.Equipment)
            {
                EditorGUILayout.PropertyField(xTierProjectilePrefab, new GUIContent("X-Tier Projectile Prefab", "X-Tier ���� ����ü ������ (�������� ������ �⺻ ������ ���)"));
            }

            EditorGUI.indentLevel--;
        }
    }

    private void DrawXTierStats(WeaponData weaponData)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("X-Tier (Tier 5)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SerializedProperty xTierStatProperty = xTierStats;

            // �⺻ ���ȵ�
            EditorGUILayout.LabelField("Basic Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("damage"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("attackDelay"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("projectileSpeed"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("knockback"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("projectileSize"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("range"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Projectile Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("canPenetrate"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("maxPenetrationCount"));
            EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("penetrationDamageDecay"));

            // ���� Ÿ�Ժ� �߰� �Ӽ�
            if (weaponData.weaponType == WeaponType.Shotgun)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shotgun Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("projectileCount"),
                    new GUIContent("Projectile Count", "������ �߻� ����ü ��"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("spreadAngle"),
                    new GUIContent("Spread Angle", "������ �߻� ���� ���� (��)"));
            }
            else if (weaponData.weaponType == WeaponType.Machinegun)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Machinegun Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("burstCount"),
                    new GUIContent("Burst Count", "�Ӽ� �߻� ����"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("burstDelay"),
                    new GUIContent("Burst Delay", "�Ӽ� �߻� �� ���� ������ (��)"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("spreadAngle"),
                    new GUIContent("Spread Angle", "�� ���� ���� (��)"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("knockbackMultiplier"),
                    new GUIContent("Knockback Multiplier", "�˹� ���� (1.0 = �⺻��)"));
            }
            else if (weaponData.weaponType == WeaponType.Cutter)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Cutter Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("cutterSpreadAngle"),
                    new GUIContent("Spread Angle", "투사체 퍼짐 각도 (도)"));
            }
            else if (weaponData.weaponType == WeaponType.BeamSaber)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Phantom Saber Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("phantomSaberDamageMultiplier"),
                    new GUIContent("Damage Multiplier", "데미지 배율"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("phantomSaberRangeMultiplier"),
                    new GUIContent("Range Multiplier", "공격 범위 배율"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("phantomSaberPowerBonus"),
                    new GUIContent("Power Bonus", "Power 증가량"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("phantomSaberHasteBonus"),
                    new GUIContent("Haste Bonus", "Haste 증가량"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("phantomSaberSpeedBonus"),
                    new GUIContent("Speed Bonus", "이동속도 증가량"));
            }
            else if (weaponData.weaponType == WeaponType.Grinder)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Grinder Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("attackRadius"),
                    new GUIContent("Attack Radius", "���� ���� ����"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("groundEffectDuration"),
                    new GUIContent("Ground Effect Duration", "���� ���� �ð�"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("damageTickInterval"),
                    new GUIContent("Damage Tick Interval", "���� ����� ƽ ����"));
                    
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("blackHoleDamageInterval"),
                    new GUIContent("Black Hole Damage Interval", "BlackHole X-Tier 데미지 주기"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("blackHoleScaleMultiplier"),
                    new GUIContent("Black Hole Scale Multiplier", "BlackHole X-Tier 최대 확장 배율"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("blackHoleDuration"),
                    new GUIContent("Black Hole Duration", "BlackHole X-Tier 지속시간"));
            }
            else if (weaponData.weaponType == WeaponType.ForceFieldGenerator)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Force Field Properties", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("forceFieldRadius"),
                    new GUIContent("Force Field Radius", "���� �ʵ��� ���� ����"));
                EditorGUILayout.PropertyField(xTierStatProperty.FindPropertyRelative("forceFieldTickInterval"),
                    new GUIContent("Damage Tick Interval", "������� ����Ǵ� �ð� ����"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
    }
}
#endif
[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Settings")]
    public int width = 1;
    public int height = 1;
    public Sprite weaponIcon;
    public WeaponType weaponType;
    public string weaponName;
    public string weaponDescription;

    public Sprite inventoryWeaponIcon;

    [Header("Equipment Settings")]
    [Tooltip("���� Ÿ���� Equipment�� ���� ����")]
    public EquipmentType equipmentType = EquipmentType.None;

    [Tooltip("�� Ƽ� ��� �ɷ�ġ")]
    public EquipmentStats[] equipmentTierStats = new EquipmentStats[4]; // 1-4 Ƽ��

    // ���� Ƽ���� ��� �ɷ�ġ getter
    public EquipmentStats CurrentEquipmentStats => equipmentTierStats[Mathf.Clamp(currentTier - 1, 0, 3)];

    [Header("Tier Configuration")]
    [Tooltip("���� ������ Ƽ��")]
    public int currentTier = 1;

    [Header("X-Tier Configuration")]
    [Tooltip("X-Ƽ��(Tier 5) ���� ����")]
    public bool supportsXTier = false;
    public Sprite xTierWeaponIcon;         // X-Ƽ�� ���� ������
    public Sprite xTierInventoryWeaponIcon; // X-Ƽ�� �κ��丮 ������
    public int xTierWidth = 1;             // X-Ƽ�� ���� �ʺ�
    public int xTierHeight = 1;            // X-Ƽ�� ���� ����
    public string xTierWeaponName;         // X-Ƽ�� ���� �̸�
    [TextArea(3, 5)]
    public string xTierWeaponDescription;  // X-Ƽ�� ���� ����
    public TierStats xTierStats = new TierStats();
    public int xTierPrice;
    public GameObject xTierProjectilePrefab; // X-Ƽ�� ���� ����ü ������

    [Header("Tier Prices")]
    public int tier1Price;
    public int tier2Price;
    public int tier3Price;
    public int tier4Price;
    [SerializeField] private float sellPriceRatio = 0.5f;

    private int currentPrice = -1;


    public int price
    {
        get
        {
            // currentPrice�� �����Ǿ� �ִٸ� �� ���� ���
            if (currentPrice >= 0)
                return currentPrice;

            // �ƴ϶�� Ƽ� ���� �⺻ ���� ��ȯ
            return currentTier switch
            {
                1 => tier1Price,
                2 => tier2Price,
                3 => tier3Price,
                4 => tier4Price,
                5 => xTierPrice,
                _ => tier1Price
            };
        }
        set
        {
            currentPrice = value;
        }
    }
    public int SellPrice => Mathf.RoundToInt(price * sellPriceRatio);

    private static readonly Color tier1Color = Color.white;       // Ƽ�� 1: ��� (�⺻)
    private static readonly Color tier2Color = new Color(0.3f, 1f, 0.3f);  // Ƽ�� 2: �ʷϻ�
    private static readonly Color tier3Color = new Color(0.3f, 0.7f, 1f);  // Ƽ�� 3: �Ķ���
    private static readonly Color tier4Color = new Color(1f, 0.3f, 0.3f);  // Ƽ�� 4: ������
    private static readonly Color xTierColor = new Color(1f, 0f, 0.3f);  // ���� ��ũ/���� ����

    [Tooltip("�� Ƽ� ���� ����")]
    public TierStats[] tierStats = new TierStats[4]; // 1-4 Ƽ��

    [Header("Prefabs")]
    public GameObject projectilePrefab;
  
    [Header("Grinder Settings")]
    [Tooltip("Grinder Ÿ���� ���� ���Ǵ� ������")]
    public float attackRadius = 2f;
    public float groundEffectDuration = 3f;
    public float damageTickInterval = 0.5f;


    public TierStats CurrentTierStats
    {
        get
        {
            // X-Tier (Tier 5) Ư�� ó��
            if (currentTier == 5 && supportsXTier)
                return xTierStats;

            // ���� Ƽ�� (1-4)
            return tierStats[Mathf.Clamp(currentTier - 1, 0, 3)];
        }
    }
    private void OnEnable()
    {
        currentPrice = -1;  // ������ ������ ���� ���� �ʱ�ȭ
    }

    // ���� Ƽ� �ش��ϴ� ���� ��ȯ
    public Color GetTierColor()
    {
        return currentTier switch
        {
            1 => tier1Color,
            2 => tier2Color,
            3 => tier3Color,
            4 => tier4Color,
            5 => xTierColor,
            _ => tier1Color
        };
    }

    // ������ ����� ���� ������ ��ȯ
    public Sprite GetColoredWeaponIcon()
    {
        if (weaponIcon == null) return null;
        return weaponIcon;
    }

    // ������ ����� �κ��丮 ���� ������ ��ȯ
    public Sprite GetColoredInventoryWeaponIcon()
    {
        if (inventoryWeaponIcon == null) return null;
        return inventoryWeaponIcon;
    }

    public float GetAttackRadius()
    {
        if (weaponType != WeaponType.Grinder) return 0f;
        return attackRadius * (currentTier * 0.25f + 0.75f); // Ƽ� ���� ���� ����
    }

    public float GetGroundEffectDuration()
    {
        if (weaponType != WeaponType.Grinder) return 0f;
        return groundEffectDuration;
    }

    public float GetDamageTickInterval()
    {
        if (weaponType != WeaponType.Grinder) return 0f;
        return damageTickInterval;
    }

    // PlayerStats�� ������ ���� ���� ��� �޼����
    public float CalculateFinalDamage(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.damage;

        float baseDamage = CurrentTierStats.damage;
        float powerMultiplier = 1f + (playerStats.Power / 100f);

        return baseDamage * powerMultiplier;
    }

    public float CalculateFinalAttackDelay(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.attackDelay;

        float baseDelay = CurrentTierStats.attackDelay;
        float cooldownReduction = Mathf.Min(playerStats.CooldownReduce, 90f);

        return baseDelay * (1f - (cooldownReduction / 100f));
    }

    public float CalculateFinalKnockback(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.knockback;

        float baseKnockback = CurrentTierStats.knockback;
        float knockbackMultiplier = 1f + (playerStats.Knockback / 100f);

        return baseKnockback * knockbackMultiplier;
    }

    public float CalculateFinalProjectileSize(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.projectileSize;

        float baseSize = CurrentTierStats.projectileSize;
        float aoeMultiplier = 1f + (playerStats.AreaOfEffect / 100f);

        return baseSize * aoeMultiplier;
    }

    public float CalculateFinalRange(PlayerStats playerStats)
    {
        if (playerStats == null) return CurrentTierStats.range;
        return CurrentTierStats.range;  // ��Ÿ��� �÷��̾� ������ ������ ���� ����
    }

    // ���� ���� getter
    public TierStats.PenetrationInfo GetPenetrationInfo()
    {
        return CurrentTierStats.GetPenetrationInfo();
    }

    public float CalculateAttacksPerSecond(PlayerStats playerStats)
    {
        return 1f / CalculateFinalAttackDelay(playerStats);
    }

    // DPS ��� (UI ǥ�ÿ�)
    public float CalculateTheoreticalDPS(PlayerStats playerStats)
    {
        float damage = CalculateFinalDamage(playerStats);
        float attackDelay = CalculateFinalAttackDelay(playerStats);

        return damage / attackDelay;
    }
    public void ApplyEquipmentEffect(PlayerStats playerStats)
    {
        if (weaponType != WeaponType.Equipment || playerStats == null) return;

        EquipmentStats stats = CurrentEquipmentStats;

        switch (equipmentType)
        {
            case EquipmentType.PowerUpper:
                playerStats.ModifyPower(stats.powerIncrease);
                break;
            case EquipmentType.SpeedUpper:
                playerStats.ModifyMovementSpeed(stats.speedIncrease, false);
                if (currentTier == 4)
                {
                    playerStats.ModifyCooldownReduce(stats.hasteIncrease);
                }
                break;
            case EquipmentType.HealthUpper:
                playerStats.ModifyMaxHealth(stats.healthIncrease);
                if (currentTier == 4)
                {
                    playerStats.ModifyHealthRegen(stats.regenIncrease);
                }
                break;
            case EquipmentType.HasteUpper:
                playerStats.ModifyCooldownReduce(stats.hasteValue);
                break;
            case EquipmentType.PortableMagnet:
                playerStats.ModifyPickupRange(stats.pickupRangeIncrease);
                if (currentTier == 4)
                {
                    playerStats.EnablePeriodicMagnetEffect(true);
                }
                break;
            case EquipmentType.KnockbackUpper:
                playerStats.ModifyKnockback(stats.knockbackIncrease);
                if (currentTier == 4)
                {
                    playerStats.ModifyPower(stats.knockbackPowerIncrease);
                }
                break;
            case EquipmentType.RegenUpper:
                playerStats.ModifyHealthRegen(stats.regenValue);
                if (currentTier == 4)
                {
                    playerStats.ModifyCooldownReduce(stats.regenHasteIncrease);
                }
                break;
        }
    }

    public void RemoveEquipmentEffect(PlayerStats playerStats)
    {
        if (weaponType != WeaponType.Equipment || playerStats == null) return;

        EquipmentStats stats = CurrentEquipmentStats;

        switch (equipmentType)
        {
            case EquipmentType.PowerUpper:
                playerStats.ModifyPower(-stats.powerIncrease);
                break;
            case EquipmentType.SpeedUpper:
                playerStats.ModifyMovementSpeed(-stats.speedIncrease, false);
                if (currentTier == 4)
                {
                    playerStats.ModifyCooldownReduce(-stats.hasteIncrease);
                }
                break;
            case EquipmentType.HealthUpper:
                playerStats.ModifyMaxHealth(-stats.healthIncrease);
                if (currentTier == 4)
                {
                    playerStats.ModifyHealthRegen(-stats.regenIncrease);
                }
                break;
            case EquipmentType.HasteUpper:
                playerStats.ModifyCooldownReduce(-stats.hasteValue);
                break;
            case EquipmentType.PortableMagnet:
                playerStats.ModifyPickupRange(-stats.pickupRangeIncrease);
                if (currentTier == 4)
                {
                    playerStats.EnablePeriodicMagnetEffect(false);
                }
                break;
            case EquipmentType.KnockbackUpper:
                playerStats.ModifyKnockback(-stats.knockbackIncrease);
                if (currentTier == 4)
                {
                    playerStats.ModifyPower(-stats.knockbackPowerIncrease);
                }
                break;
            case EquipmentType.RegenUpper:
                playerStats.ModifyHealthRegen(-stats.regenValue);
                if (currentTier == 4)
                {
                    playerStats.ModifyCooldownReduce(-stats.regenHasteIncrease);
                }
                break;
        }
    }


    // ���� Ƽ�� ���� ����
    public WeaponData CreateNextTierWeapon()
    {
        if (currentTier >= 4) return null;

        WeaponData nextTierWeapon = Instantiate(this);
        nextTierWeapon.currentTier = currentTier + 1;
        nextTierWeapon.weaponName = $"{weaponName} Tier {nextTierWeapon.currentTier}";

        return nextTierWeapon;
    }


    // X-Ƽ�� ���� ���� �޼��� �߰�
    public WeaponData CreateXTierWeapon(string customXTierName = null)
    {
        if (currentTier != 4 || !supportsXTier)
        {
            Debug.LogWarning("Can only create X-Tier weapon from Tier 4 weapons that support X-Tier!");
            return null;
        }

        WeaponData xTierWeapon = Instantiate(this);
        xTierWeapon.currentTier = 5;

        // X-Ƽ�� Ŀ���� �Ӽ� ����
        if (!string.IsNullOrEmpty(xTierWeaponName))
        {
            xTierWeapon.weaponName = xTierWeaponName;
        }
        else if (!string.IsNullOrEmpty(customXTierName))
        {
            xTierWeapon.weaponName = customXTierName;
        }
        else
        {
            xTierWeapon.weaponName = $"X-{weaponName}";
        }

        // ���� ����
        if (!string.IsNullOrEmpty(xTierWeaponDescription))
        {
            xTierWeapon.weaponDescription = xTierWeaponDescription;
        }

        // ũ�� ����
        if (xTierWidth > 0 && xTierHeight > 0)
        {
            xTierWeapon.width = xTierWidth;
            xTierWeapon.height = xTierHeight;
        }

        // ������ ����
        if (xTierWeaponIcon != null)
        {
            xTierWeapon.weaponIcon = xTierWeaponIcon;
        }

        if (xTierInventoryWeaponIcon != null)
        {
            xTierWeapon.inventoryWeaponIcon = xTierInventoryWeaponIcon;
        }

        // ����ü �������� �����Ǿ� �ִٸ� ��ü
        if (xTierProjectilePrefab != null)
        {
            xTierWeapon.projectilePrefab = xTierProjectilePrefab;
        }

        return xTierWeapon;
    }

    private void OnValidate()
    {
        if (tierStats == null || tierStats.Length != 4)
        {
            TierStats[] newTierStats = new TierStats[4];
            if (tierStats != null)
            {
                for (int i = 0; i < Mathf.Min(tierStats.Length, 4); i++)
                {
                    newTierStats[i] = tierStats[i] ?? new TierStats();
                }
            }
            for (int i = (tierStats?.Length ?? 0); i < 4; i++)
            {
                newTierStats[i] = new TierStats();
            }
            tierStats = newTierStats;
        }

        for (int i = 0; i < 4; i++)
        {
            if (tierStats[i] == null)
            {
                tierStats[i] = new TierStats();
            }
        }
        if (weaponType == WeaponType.Shotgun)
        {
            for (int i = 0; i < tierStats.Length; i++)
            {
                if (tierStats[i] == null) continue;

                // �⺻���� ���� �������� ���� ��쿡�� ����
                if (tierStats[i].projectileCount <= 0)
                {
                    tierStats[i].projectileCount = 3 + i;  // 1Ƽ��: 3��, 2Ƽ��: 4��, ...
                }
                if (tierStats[i].spreadAngle <= 0)
                {
                    tierStats[i].spreadAngle = 45f + (i * 5f);  // 1Ƽ��: 45��, 2Ƽ��: 50��, ...
                }
            }
        }

        else if (weaponType == WeaponType.Grinder)
        {
            for (int i = 0; i < tierStats.Length; i++)
            {
                if (tierStats[i].attackRadius <= 0)
                {
                    tierStats[i].attackRadius = 2f + (i * 0.5f);
                }
                if (tierStats[i].groundEffectDuration <= 0)
                {
                    tierStats[i].groundEffectDuration = 3f + (i * 0.5f);
                }
                if (tierStats[i].damageTickInterval <= 0 || tierStats[i].damageTickInterval > 0.5f)
                {
                    tierStats[i].damageTickInterval = 0.5f - (i * 0.05f);
                }
            }
        }

        if (weaponType == WeaponType.Equipment &&
       (equipmentTierStats == null || equipmentTierStats.Length != 4))
        {
            EquipmentStats[] newEquipmentTierStats = new EquipmentStats[4];
            for (int i = 0; i < 4; i++)
            {
                newEquipmentTierStats[i] = new EquipmentStats();
                float tierMultiplier = 1f + (i * 0.25f); // Ƽ��� 25% ����

                // �⺻�� ���� (�����Ϳ��� ���� ����)
                switch (equipmentType)
                {
                    case EquipmentType.PowerUpper:
                        newEquipmentTierStats[i].powerIncrease = 5f + (i * 3f); // 5, 8, 11, 14
                        break;

                    case EquipmentType.SpeedUpper:
                        newEquipmentTierStats[i].speedIncrease = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5
                        if (i == 3) // 4Ƽ��
                        {
                            newEquipmentTierStats[i].hasteIncrease = 20f; // 4Ƽ�� ��ٿ� ����
                        }
                        break;

                    case EquipmentType.HealthUpper:
                        newEquipmentTierStats[i].healthIncrease = 25f + (i * 15f); // 25, 40, 55, 70
                        if (i == 3) // 4Ƽ��
                        {
                            newEquipmentTierStats[i].regenIncrease = 2f; // 4Ƽ�� ü�� ���
                        }
                        break;

                    case EquipmentType.HasteUpper:
                        newEquipmentTierStats[i].hasteValue = 15f + (i * 5f); // 15, 20, 25, 30
                        break;

                    case EquipmentType.PortableMagnet:
                        newEquipmentTierStats[i].pickupRangeIncrease = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5 (�����ϰ� % �ƴ� ���� �Ÿ�)
                        break;

                    case EquipmentType.KnockbackUpper:
                        newEquipmentTierStats[i].knockbackIncrease = 3f + (i * 2f); // 3, 5, 7, 9
                        if (i == 3) // 4Ƽ��
                        {
                            newEquipmentTierStats[i].knockbackPowerIncrease = 15f; // 4Ƽ�� ���ݷ�
                        }
                        break;

                    case EquipmentType.RegenUpper:
                        newEquipmentTierStats[i].regenValue = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5
                        if (i == 3) // 4Ƽ��
                        {
                            newEquipmentTierStats[i].regenHasteIncrease = 15f; // 4Ƽ�� ��ٿ� ����
                        }
                        break;
                }
            }
            equipmentTierStats = newEquipmentTierStats;
        }

        // X-Tier ���� �ʱ�ȭ �߰�
        if (supportsXTier && xTierStats == null)
        {
            xTierStats = new TierStats();

            // Tier 4 ������ ������� X-Tier ���� �ʱ� ����
            if (tierStats != null && tierStats.Length >= 4 && tierStats[3] != null)
            {
                // ������: 50% ����
                xTierStats.damage = tierStats[3].damage * 1.5f;

                // ���� �ӵ�: 30% ���� (������ 30% ����)
                xTierStats.attackDelay = tierStats[3].attackDelay * 0.7f;

                // ����ü �ӵ�: 30% ����
                xTierStats.projectileSpeed = tierStats[3].projectileSpeed * 1.3f;

                // �˹�: 50% ����
                xTierStats.knockback = tierStats[3].knockback * 1.5f;

                // ����ü ũ��: 30% ����
                xTierStats.projectileSize = tierStats[3].projectileSize * 1.3f;

                // ����: 30% ����
                xTierStats.range = tierStats[3].range * 1.3f;

                // ���� ����
                xTierStats.canPenetrate = true;
                xTierStats.maxPenetrationCount = 0; // ���� ����
                xTierStats.penetrationDamageDecay = 0.1f;

                // ���� Ÿ�Ժ� Ư�� ����
                if (weaponType == WeaponType.Shotgun)
                {
                    xTierStats.projectileCount = tierStats[3].projectileCount + 2;
                    xTierStats.spreadAngle = tierStats[3].spreadAngle * 1.2f;
                }
                else if (weaponType == WeaponType.Machinegun)
                {
                    xTierStats.burstCount = tierStats[3].burstCount * 2; // 연사 횟수 2배
                    xTierStats.burstDelay = tierStats[3].burstDelay * 0.5f; // 연사 간격 50% 단축
                    xTierStats.spreadAngle = tierStats[3].spreadAngle * 2f; // 탄퍼짐 각도 2배
                    xTierStats.knockbackMultiplier = tierStats[3].knockbackMultiplier * 1.2f; // 넉백 20% 증가
                }
                else if (weaponType == WeaponType.Cutter)
                {
                    xTierStats.cutterSpreadAngle = tierStats[3].cutterSpreadAngle * 1.5f; // 퍼짐 각도 50% 증가
                }
                else if (weaponType == WeaponType.BeamSaber)
                {
                    xTierStats.phantomSaberDamageMultiplier = tierStats[3].phantomSaberDamageMultiplier * 1.2f; // 데미지 배율 20% 증가
                    xTierStats.phantomSaberRangeMultiplier = tierStats[3].phantomSaberRangeMultiplier * 1.2f; // 범위 배율 20% 증가
                    xTierStats.phantomSaberPowerBonus = tierStats[3].phantomSaberPowerBonus * 1.5f; // Power 보너스 50% 증가
                    xTierStats.phantomSaberHasteBonus = tierStats[3].phantomSaberHasteBonus * 1.5f; // Haste 보너스 50% 증가
                    xTierStats.phantomSaberSpeedBonus = tierStats[3].phantomSaberSpeedBonus * 1.5f; // 속도 보너스 50% 증가
                }
                else if (weaponType == WeaponType.Grinder)
                {
                    xTierStats.attackRadius = tierStats[3].attackRadius * 1.5f;
                    xTierStats.groundEffectDuration = tierStats[3].groundEffectDuration * 1.5f;
                    xTierStats.damageTickInterval = tierStats[3].damageTickInterval * 0.7f;
                    
                    // Black Hole X-Tier 속성 초기화
                    xTierStats.blackHoleDamageInterval = 0.25f;
                    xTierStats.blackHoleScaleMultiplier = 1.5f;
                    xTierStats.blackHoleDuration = 5f;
                }
                else if (weaponType == WeaponType.ForceFieldGenerator)
                {
                    xTierStats.forceFieldRadius = tierStats[3].forceFieldRadius * 1.5f;
                    xTierStats.forceFieldTickInterval = tierStats[3].forceFieldTickInterval * 0.7f;
                }
            }

            if (string.IsNullOrEmpty(xTierWeaponName))
            {
                // ���� Ÿ�Կ� ���� �̸� ����
                switch (weaponType)
                {
                    case WeaponType.Buster:
                        xTierWeaponName = "Exterminator";
                        break;
                    case WeaponType.Machinegun:
                        xTierWeaponName = "Ultrain";
                        break;
                    case WeaponType.Blade:
                        xTierWeaponName = "Plasma Sword";
                        break;
                    case WeaponType.Cutter:
                        xTierWeaponName = "Cyclone Edge";
                        break;
                    case WeaponType.Sawblade:
                        xTierWeaponName = "Infinity Disc";
                        break;
                    case WeaponType.BeamSaber:
                        xTierWeaponName = "Phantom Saber";
                        break;
                    case WeaponType.Shotgun:
                        xTierWeaponName = "HellFire";
                        break;
                    case WeaponType.Grinder:
                        xTierWeaponName = "Black Hole";
                        break;
                    case WeaponType.ForceFieldGenerator:
                        xTierWeaponName = "Time Turner";
                        break;
                    default:
                        xTierWeaponName = $"X-{weaponName}";
                        break;
                }
            }
        }

    }
}