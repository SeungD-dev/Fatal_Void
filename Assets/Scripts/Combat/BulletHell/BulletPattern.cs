using UnityEngine;

public enum PatternType
{
    Circle,     // 원형
    Spiral,     // 나선형
    Line,       // 직선
    Wave,       // 파형
    Burst,      // 폭발형
    Cross,      // 십자형
    Random      // 랜덤
}

public enum SpeedType
{
    Constant,   // 일정 속도
    Accelerate, // 가속
    Decelerate, // 감속
    Sine        // 사인파
}

[System.Serializable]
public class BulletPattern
{
    [Header("기본 설정")]
    public string patternName = "New Pattern";
    public PatternType patternType = PatternType.Circle;
    
    [Header("발사 설정")]
    [Range(1, 50)]
    public int bulletCount = 8;
    [Range(0.1f, 20f)]
    public float bulletSpeed = 5f;
    [Range(0f, 10f)]
    public float fireRate = 1f;
    
    [Header("방향 설정")]
    [Range(0f, 360f)]
    public float startAngle = 0f;
    [Range(0f, 360f)]
    public float angleSpread = 360f;
    [Range(-360f, 360f)]
    public float rotationSpeed = 0f;
    
    [Header("거리 설정")]
    [Range(0.5f, 20f)]
    public float distance = 5f;
    [Range(0f, 10f)]
    public float distanceVariation = 0f;
    
    [Header("속도 변화")]
    public SpeedType speedType = SpeedType.Constant;
    [Range(0.1f, 5f)]
    public float speedMultiplier = 1f;
    
    [Header("특수 설정")]
    [Range(0f, 5f)]
    public float lifetime = 10f;
    public bool useGravity = false;
    [Range(-10f, 10f)]
    public float gravityStrength = -9.8f;
    
    [Header("미리보기")]
    public bool showPreview = true;
    public Color previewColor = Color.red;
    
    // 런타임에서 사용할 계산된 값들
    [System.NonSerialized]
    public float currentRotation = 0f;
    [System.NonSerialized]
    public float timeSinceLastFire = 0f;
}