using UnityEngine;
using System.Collections.Generic;

public class BulletHellSystem : MonoBehaviour
{
    [Header("패턴 설정")]
    [SerializeField] private List<BulletPattern> patterns = new List<BulletPattern>();
    
    [Header("발사체 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private string bulletPoolTag = "BossBullet";
    
    [Header("타겟 설정")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("보스 데이터")]
    [SerializeField] private EnemyData bossData;
    
    // 캐시된 컴포넌트들
    private Transform cachedTransform;
    private ObjectPool bulletPool;
    private CalamityBoss parentBoss; // 부모 보스 참조
    
    private void Awake()
    {
        cachedTransform = transform;
        
        // ObjectPool 찾기
        bulletPool = FindObjectOfType<ObjectPool>();
        if (bulletPool == null)
        {
            Debug.LogError("ObjectPool not found! BulletHellSystem requires ObjectPool component.");
        }
        
        // 플레이어 자동 탐지
        if (autoFindPlayer && target == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
                target = player.transform;
        }
        
        // 부모 보스 참조 설정
        parentBoss = GetComponent<CalamityBoss>();
    }
    
    private void Update()
    {
        // 모든 패턴 업데이트
        for (int i = 0; i < patterns.Count; i++)
        {
            UpdatePattern(patterns[i]);
        }
    }
    
    private void UpdatePattern(BulletPattern pattern)
    {
        if (pattern == null) return;
        
        pattern.timeSinceLastFire += Time.deltaTime;
        pattern.currentRotation += pattern.rotationSpeed * Time.deltaTime;
        
        // 발사 시간 체크
        if (pattern.timeSinceLastFire >= 1f / pattern.fireRate)
        {
            FirePattern(pattern);
            pattern.timeSinceLastFire = 0f;
        }
    }
    
    private void FirePattern(BulletPattern pattern)
    {
        Vector3 centerPosition = cachedTransform.position;
        
        switch (pattern.patternType)
        {
            case PatternType.Circle:
                FireCirclePattern(pattern, centerPosition);
                break;
            case PatternType.Spiral:
                FireSpiralPattern(pattern, centerPosition);
                break;
            case PatternType.Line:
                FireLinePattern(pattern, centerPosition);
                break;
            case PatternType.Wave:
                FireWavePattern(pattern, centerPosition);
                break;
            case PatternType.Burst:
                FireBurstPattern(pattern, centerPosition);
                break;
            case PatternType.Cross:
                FireCrossPattern(pattern, centerPosition);
                break;
            case PatternType.Random:
                FireRandomPattern(pattern, centerPosition);
                break;
        }
    }
    
    private void FireCirclePattern(BulletPattern pattern, Vector3 center)
    {
        float angleStep = pattern.angleSpread / pattern.bulletCount;
        
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = pattern.startAngle + (angleStep * i) + pattern.currentRotation;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            FireBullet(center, direction, pattern);
        }
    }
    
    private void FireSpiralPattern(BulletPattern pattern, Vector3 center)
    {
        float angleStep = pattern.angleSpread / pattern.bulletCount;
        
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = pattern.startAngle + (angleStep * i) + pattern.currentRotation;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            // 나선형은 거리가 점점 증가
            float spiralDistance = pattern.distance + (i * 0.5f);
            Vector3 startPos = center + (Vector3)direction * spiralDistance;
            
            FireBullet(startPos, direction, pattern);
        }
    }
    
    private void FireLinePattern(BulletPattern pattern, Vector3 center)
    {
        Vector2 targetDirection = target != null ? 
            ((Vector2)(target.position - center)).normalized : 
            Vector2.right;
        
        Vector2 perpendicular = new Vector2(-targetDirection.y, targetDirection.x);
        float spacing = pattern.distance / pattern.bulletCount;
        
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float offset = (i - pattern.bulletCount * 0.5f) * spacing;
            Vector3 startPos = center + (Vector3)perpendicular * offset;
            
            FireBullet(startPos, targetDirection, pattern);
        }
    }
    
    private void FireWavePattern(BulletPattern pattern, Vector3 center)
    {
        Vector2 baseDirection = target != null ? 
            ((Vector2)(target.position - center)).normalized : 
            Vector2.right;
        
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float t = (float)i / (pattern.bulletCount - 1);
            float waveAngle = Mathf.Sin(t * Mathf.PI * 2 + Time.time * pattern.rotationSpeed) * pattern.angleSpread * 0.5f;
            
            Vector2 direction = RotateVector2(baseDirection, waveAngle);
            FireBullet(center, direction, pattern);
        }
    }
    
    private void FireBurstPattern(BulletPattern pattern, Vector3 center)
    {
        // 모든 방향으로 한번에 발사
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            FireBullet(center, direction, pattern);
        }
    }
    
    private void FireCrossPattern(BulletPattern pattern, Vector3 center)
    {
        // 십자 방향으로 발사
        Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        
        foreach (Vector2 baseDir in directions)
        {
            for (int i = 0; i < pattern.bulletCount / 4; i++)
            {
                Vector2 direction = RotateVector2(baseDir, pattern.currentRotation + i * 15f);
                FireBullet(center, direction, pattern);
            }
        }
    }
    
    private void FireRandomPattern(BulletPattern pattern, Vector3 center)
    {
        for (int i = 0; i < pattern.bulletCount; i++)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector2 direction = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            
            // 랜덤 위치에서 시작
            Vector2 randomOffset = Random.insideUnitCircle * pattern.distanceVariation;
            Vector3 startPos = center + (Vector3)randomOffset;
            
            FireBullet(startPos, direction, pattern);
        }
    }
    
    private void FireBullet(Vector3 position, Vector2 direction, BulletPattern pattern)
    {
        if (bulletPool == null) return;
        
        var bullet = bulletPool.SpawnFromPool(bulletPoolTag, position, Quaternion.identity);
        if (bullet != null)
        {
            
            // BossBullet 컴포넌트 사용
            var bossBullet = bullet.GetComponent<BossBullet>();
            if (bossBullet != null)
            {
                float bulletDamage = (bossData != null && bossData.enemyType == EnemyType.Boss) 
                    ? bossData.bossDamage 
                    : 10f; // 기본 데미지
                
                bossBullet.Initialize(
                    direction: direction,
                    speed: pattern.bulletSpeed,  // 패턴별 속도
                    damage: bulletDamage,        // 보스별 데미지
                    lifetime: pattern.lifetime   // 패턴별 수명
                );
                
                // 부모 보스에게 탄환 발사 알림 (현재 사용하지 않음)
            }
            else
            {
                // BossBullet이 없으면 기본 방식으로 처리
                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * pattern.bulletSpeed;
                }
                StartCoroutine(DeactivateBulletAfterTime(bullet, pattern.lifetime));
            }
        }
    }
    
    private Vector2 RotateVector2(Vector2 vector, float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    // 패턴 추가/제거 메서드
    public void AddPattern(BulletPattern pattern)
    {
        patterns.Add(pattern);
    }
    
    public void RemovePattern(int index)
    {
        if (index >= 0 && index < patterns.Count)
            patterns.RemoveAt(index);
    }
    
    public void ClearPatterns()
    {
        patterns.Clear();
    }
    
    public void SetBossData(EnemyData bossEnemyData)
    {
        this.bossData = bossEnemyData;
    }
    
    private System.Collections.IEnumerator DeactivateBulletAfterTime(GameObject bullet, float time)
    {
        yield return new WaitForSeconds(time);
        
        if (bullet != null && bullet.activeInHierarchy)
        {
            bullet.SetActive(false);
        }
    }
}