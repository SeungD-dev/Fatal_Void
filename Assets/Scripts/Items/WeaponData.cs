using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[System.Serializable]
public class TierStats
{
    [Header("Stats")]
    [Tooltip("해당 티어의 기본 데미지")]
    public float damage = 10f;
    [Tooltip("해당 티어의 기본 공격 딜레이")]
    public float attackDelay = 1f;
    [Tooltip("해당 티어의 투사체 속도")]
    public float projectileSpeed = 10f;
    [Tooltip("해당 티어의 넉백 수치")]
    public float knockback = 1f;
    [Tooltip("해당 티어의 투사체 크기")]
    public float projectileSize = 1f;
    [Tooltip("해당 티어의 사거리")]
    public float range = 5f;

    [Header("Projectile Properties")]
    [Tooltip("true일 경우 투사체가 적을 관통합니다")]
    public bool canPenetrate = false;
    [Tooltip("관통 가능한 최대 적 수 (0 = 무한)")]
    public int maxPenetrationCount = 0;
    [Tooltip("관통시 데미지 감소율 (0.1 = 10% 감소)")]
    public float penetrationDamageDecay = 0.1f;

    public int projectileCount = 3;
    public float spreadAngle = 45f;

    [Header("Grinder Properties")]
    [Tooltip("장판 공격 범위")]
    public float attackRadius = 2f;
    [Tooltip("장판 지속 시간")]
    public float groundEffectDuration = 3f;
    [Tooltip("장판 대미지 틱 간격")]
    public float damageTickInterval = 0.5f;

    [Header("Force Field Properties")]
    [Tooltip("포스 필드 공격 범위")]
    public float forceFieldRadius = 3f;
    [Tooltip("포스 필드 대미지 틱 간격")]
    public float forceFieldTickInterval = 0.5f;

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
    [Tooltip("공격력 증가량")]
    public float powerIncrease;

    [Header("Speed Upper Settings")]
    [Tooltip("이동속도 증가량")]
    public float speedIncrease;
    [Tooltip("쿨다운 감소량 (4티어 전용)")]
    public float hasteIncrease;

    [Header("Health Upper Settings")]
    [Tooltip("최대 체력 증가량")]
    public float healthIncrease;
    [Tooltip("체력 재생 증가량 (4티어 전용)")]
    public float regenIncrease;

    [Header("Haste Upper Settings")]
    [Tooltip("쿨다운 감소량")]
    public float hasteValue;

    [Header("Portable Magnet Settings")]
    [Tooltip("아이템 획득 범위 증가량 (유닛)")]
    public float pickupRangeIncrease;
    [Header("Portable Magnet Additional Effect")]
    [Tooltip("4티어 자동 자석 효과 활성화")]
    public bool enableAutoMagnet = false;

    [Header("Knockback Upper Settings")]
    [Tooltip("넉백 증가량")]
    public float knockbackIncrease;
    [Tooltip("공격력 증가량 (4티어 전용)")]
    public float knockbackPowerIncrease;

    [Header("Regen Upper Settings")]
    [Tooltip("체력 재생 증가량")]
    public float regenValue;
    [Tooltip("쿨다운 감소량 (4티어 전용)")]
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

        // WeaponType이 Equipment일 때는 Equipment 설정을, 아닐 때는 일반 무기 설정을 보여줌
        if (weaponData.weaponType == WeaponType.Equipment)
        {
            DrawEquipmentSettings();
        }
        else
        {
            DrawTierStats(weaponData);
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
                        DrawEquipmentProperty(tierStat, "powerIncrease", "Power Increase", "공격력 증가량");
                        break;
                    case EquipmentType.SpeedUpper:
                        DrawEquipmentProperty(tierStat, "speedIncrease", "Speed Increase", "이동속도 증가량");
                        if (i == 3) // 4티어
                        {
                            DrawEquipmentProperty(tierStat, "hasteIncrease", "Haste Increase", "쿨다운 감소량");
                        }
                        break;
                    case EquipmentType.HealthUpper:
                        DrawEquipmentProperty(tierStat, "healthIncrease", "Health Increase", "체력 증가량");
                        if (i == 3) // 4티어
                        {
                            DrawEquipmentProperty(tierStat, "regenIncrease", "Regen Increase", "체력 재생 증가량");
                        }
                        break;
                    case EquipmentType.HasteUpper:
                        DrawEquipmentProperty(tierStat, "hasteValue", "Haste Value", "쿨다운 감소량");
                        break;
                    case EquipmentType.PortableMagnet:
                        DrawEquipmentProperty(tierStat, "pickupRangeIncrease", "Pickup Range Increase", "아이템 획득 범위 증가량");
                        if (i == 3) // 4티어
                        {
                            DrawEquipmentProperty(tierStat, "enableAutoMagnet", "Auto Magnet", "자동 자석 효과 활성화");
                        }
                        break;
                    case EquipmentType.KnockbackUpper:
                        DrawEquipmentProperty(tierStat, "knockbackIncrease", "Knockback Increase", "넉백 증가량");
                        if (i == 3) // 4티어
                        {
                            DrawEquipmentProperty(tierStat, "knockbackPowerIncrease", "Power Increase", "공격력 증가량");
                        }
                        break;
                    case EquipmentType.RegenUpper:
                        DrawEquipmentProperty(tierStat, "regenValue", "Regen Value", "체력 재생 증가량");
                        if (i == 3) // 4티어
                        {
                            DrawEquipmentProperty(tierStat, "regenHasteIncrease", "Haste Increase", "쿨다운 감소량");
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

                // 기본 스탯들
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

                // 무기 타입별 추가 속성
                if (weaponData.weaponType == WeaponType.Shotgun)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Shotgun Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("projectileCount"),
                        new GUIContent("Projectile Count", "샷건의 발사 투사체 수"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("spreadAngle"),
                        new GUIContent("Spread Angle", "샷건의 발사 각도 범위 (도)"));
                }
                else if (weaponData.weaponType == WeaponType.Grinder)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Grinder Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("attackRadius"),
                        new GUIContent("Attack Radius", "장판 공격 범위"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("groundEffectDuration"),
                        new GUIContent("Ground Effect Duration", "장판 지속 시간"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("damageTickInterval"),
                        new GUIContent("Damage Tick Interval", "장판 대미지 틱 간격"));
                }
                else if (weaponData.weaponType == WeaponType.ForceFieldGenerator)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Force Field Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("forceFieldRadius"),
                        new GUIContent("Force Field Radius", "포스 필드의 공격 범위"));
                    EditorGUILayout.PropertyField(tierStat.FindPropertyRelative("forceFieldTickInterval"),
                        new GUIContent("Damage Tick Interval", "대미지가 적용되는 시간 간격"));
                }
            }

            EditorGUILayout.Space();
        }
        EditorGUI.indentLevel--;
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
    [Tooltip("무기 타입이 Equipment일 때만 사용됨")]
    public EquipmentType equipmentType = EquipmentType.None;

    [Tooltip("각 티어별 장비 능력치")]
    public EquipmentStats[] equipmentTierStats = new EquipmentStats[4]; // 1-4 티어

    // 현재 티어의 장비 능력치 getter
    public EquipmentStats CurrentEquipmentStats => equipmentTierStats[Mathf.Clamp(currentTier - 1, 0, 3)];

    [Header("Tier Configuration")]
    [Tooltip("현재 무기의 티어")]
    public int currentTier = 1;

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
            // currentPrice가 설정되어 있다면 그 값을 사용
            if (currentPrice >= 0)
                return currentPrice;

            // 아니라면 티어에 따른 기본 가격 반환
            return currentTier switch
            {
                1 => tier1Price,
                2 => tier2Price,
                3 => tier3Price,
                4 => tier4Price,
                _ => tier1Price
            };
        }
        set
        {
            currentPrice = value;
        }
    }
    public int SellPrice => Mathf.RoundToInt(price * sellPriceRatio);

    private static readonly Color tier1Color = Color.white;       // 티어 1: 흰색 (기본)
    private static readonly Color tier2Color = new Color(0.3f, 1f, 0.3f);  // 티어 2: 초록색
    private static readonly Color tier3Color = new Color(0.3f, 0.7f, 1f);  // 티어 3: 파란색
    private static readonly Color tier4Color = new Color(1f, 0.3f, 0.3f);  // 티어 4: 빨간색

    [Tooltip("각 티어별 스탯 설정")]
    public TierStats[] tierStats = new TierStats[4]; // 1-4 티어

    [Header("Prefabs")]
    public GameObject projectilePrefab;
   
    [Header("Grinder Settings")]
    [Tooltip("Grinder 타입일 때만 사용되는 설정들")]
    public float attackRadius = 2f;
    public float groundEffectDuration = 3f;
    public float damageTickInterval = 0.5f;


    // 현재 티어의 스탯 getter
    public TierStats CurrentTierStats => tierStats[Mathf.Clamp(currentTier - 1, 0, 3)];

    private void OnEnable()
    {
        currentPrice = -1;  // 복제될 때마다 현재 가격 초기화
    }

    // 현재 티어에 해당하는 색상 반환
    public Color GetTierColor()
    {
        return currentTier switch
        {
            1 => tier1Color,
            2 => tier2Color,
            3 => tier3Color,
            4 => tier4Color,
            _ => tier1Color
        };
    }

    // 색상이 적용된 무기 아이콘 반환
    public Sprite GetColoredWeaponIcon()
    {
        if (weaponIcon == null) return null;
        return weaponIcon;
    }

    // 색상이 적용된 인벤토리 무기 아이콘 반환
    public Sprite GetColoredInventoryWeaponIcon()
    {
        if (inventoryWeaponIcon == null) return null;
        return inventoryWeaponIcon;
    }

    public float GetAttackRadius()
    {
        if (weaponType != WeaponType.Grinder) return 0f;
        return attackRadius * (currentTier * 0.25f + 0.75f); // 티어에 따라 범위 증가
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

    // PlayerStats를 고려한 최종 스탯 계산 메서드들
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
        return CurrentTierStats.range;  // 사거리는 플레이어 스탯의 영향을 받지 않음
    }

    // 관통 정보 getter
    public TierStats.PenetrationInfo GetPenetrationInfo()
    {
        return CurrentTierStats.GetPenetrationInfo();
    }

    public float CalculateAttacksPerSecond(PlayerStats playerStats)
    {
        return 1f / CalculateFinalAttackDelay(playerStats);
    }

    // DPS 계산 (UI 표시용)
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


    // 다음 티어 무기 생성
    public WeaponData CreateNextTierWeapon()
    {
        if (currentTier >= 4) return null;

        WeaponData nextTierWeapon = Instantiate(this);
        nextTierWeapon.currentTier = currentTier + 1;
        nextTierWeapon.weaponName = $"{weaponName} Tier {nextTierWeapon.currentTier}";

        return nextTierWeapon;
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

                // 기본값이 아직 설정되지 않은 경우에만 설정
                if (tierStats[i].projectileCount <= 0)
                {
                    tierStats[i].projectileCount = 3 + i;  // 1티어: 3발, 2티어: 4발, ...
                }
                if (tierStats[i].spreadAngle <= 0)
                {
                    tierStats[i].spreadAngle = 45f + (i * 5f);  // 1티어: 45도, 2티어: 50도, ...
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
                float tierMultiplier = 1f + (i * 0.25f); // 티어당 25% 증가

                // 기본값 설정 (에디터에서 수정 가능)
                switch (equipmentType)
                {
                    case EquipmentType.PowerUpper:
                        newEquipmentTierStats[i].powerIncrease = 5f + (i * 3f); // 5, 8, 11, 14
                        break;

                    case EquipmentType.SpeedUpper:
                        newEquipmentTierStats[i].speedIncrease = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5
                        if (i == 3) // 4티어
                        {
                            newEquipmentTierStats[i].hasteIncrease = 20f; // 4티어 쿨다운 감소
                        }
                        break;

                    case EquipmentType.HealthUpper:
                        newEquipmentTierStats[i].healthIncrease = 25f + (i * 15f); // 25, 40, 55, 70
                        if (i == 3) // 4티어
                        {
                            newEquipmentTierStats[i].regenIncrease = 2f; // 4티어 체력 재생
                        }
                        break;

                    case EquipmentType.HasteUpper:
                        newEquipmentTierStats[i].hasteValue = 15f + (i * 5f); // 15, 20, 25, 30
                        break;

                    case EquipmentType.PortableMagnet:
                        newEquipmentTierStats[i].pickupRangeIncrease = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5 (유일하게 % 아닌 실제 거리)
                        break;

                    case EquipmentType.KnockbackUpper:
                        newEquipmentTierStats[i].knockbackIncrease = 3f + (i * 2f); // 3, 5, 7, 9
                        if (i == 3) // 4티어
                        {
                            newEquipmentTierStats[i].knockbackPowerIncrease = 15f; // 4티어 공격력
                        }
                        break;

                    case EquipmentType.RegenUpper:
                        newEquipmentTierStats[i].regenValue = 1f + (i * 0.5f); // 1, 1.5, 2, 2.5
                        if (i == 3) // 4티어
                        {
                            newEquipmentTierStats[i].regenHasteIncrease = 15f; // 4티어 쿨다운 감소
                        }
                        break;
                }
            }
            equipmentTierStats = newEquipmentTierStats;
        }
    }
}