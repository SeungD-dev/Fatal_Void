using UnityEngine;

public enum BossActionType
{
    SummonEnemies,  // Wisp/Brute 랜덤 소환
    BulletHell      // 패턴 발사
}

[System.Serializable]
public class BossAction
{
    [Header("행동 타입")]
    public BossActionType actionType = BossActionType.BulletHell;
    
    [Header("소환 설정")]
    [Tooltip("소환할 몬스터 개수")]
    public int summonCount = 3;
    [Tooltip("소환 후 패턴 종료까지 대기시간")]
    public float summonDuration = 5f;
    
    [Header("Bullet Hell 설정")]
    public BulletPattern bulletPattern;
    [Tooltip("이 패턴에서 발사할 총 투사체 개수")]
    public int totalBulletCount = 50;
    [Tooltip("마지막 투사체 발사 후 대기시간")]
    public float endDelay = 2f;
    
    // 런타임에서 사용할 변수들
    [System.NonSerialized]
    public int currentBulletCount = 0;
    [System.NonSerialized]
    public float actionStartTime = 0f;
    [System.NonSerialized]
    public bool isCompleted = false;
    [System.NonSerialized]
    public float lastBulletFireTime = 0f;
    
    public void ResetAction()
    {
        currentBulletCount = 0;
        actionStartTime = Time.time;
        isCompleted = false;
        lastBulletFireTime = 0f;
    }
    
    public bool IsActionComplete()
    {
        switch (actionType)
        {
            case BossActionType.SummonEnemies:
                // 소환 후 지정된 시간이 지나면 완료
                return Time.time - actionStartTime >= summonDuration;
                
            case BossActionType.BulletHell:
                // 총 탄환 개수 도달하고 + 마지막 발사 후 딜레이 시간 경과
                return currentBulletCount >= totalBulletCount && 
                       Time.time - lastBulletFireTime >= endDelay;
                       
            default:
                return true;
        }
    }
    
    public void OnBulletFired()
    {
        if (actionType == BossActionType.BulletHell)
        {
            currentBulletCount++;
            lastBulletFireTime = Time.time;
        }
    }
}