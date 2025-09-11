using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CalamityBoss : Enemy
{
    [Header("보스 행동 설정")]
    [SerializeField] private float actionStartDelay = 2f; // 보스 등장 후 첫 행동까지 딜레이
    [SerializeField] private float patternCooldown = 3f; // 패턴 간 대기시간
    [SerializeField] private bool useRandomOrder = true; // 랜덤 패턴 순서
    
    [Header("소환 설정")]
    [SerializeField] private int summonCount = 5; // 한번에 소환할 적 수 (고정)
    
    [Header("탄막 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private string bulletPoolTag = "BossBullet";
    
    
    // 초기화에서 받는 참조들
    private Transform playerTarget;
    private ObjectPool bulletPool;
    private WaveManager waveManager;
    private WaveData.Wave currentWaveData;
    
    // 행동 제어 변수들
    private int currentPatternIndex = 0;
    private bool isActionInProgress = false;
    private Coroutine actionCoroutine;
    
    // 상태 추적
    private bool isBossActive = false;
    
    // 패턴 타입
    private enum BossPattern
    {
        SummonEnemies,
        BulletHell
    }
    
    /// <summary>
    /// WaveManager에서 호출하는 보스 초기화 메서드
    /// </summary>
    public void InitializeBoss(WaveData.Wave waveData, ObjectPool objectPool, Transform player, WaveManager waveManagerRef)
    {
        currentWaveData = waveData;
        bulletPool = objectPool;
        playerTarget = player;
        waveManager = waveManagerRef;
        
        Debug.Log($"CalamityBoss initialized with {waveData.enemies.Count} enemy types available for summoning");
    }
    
    public new void OnObjectSpawn()
    {
        // 부모 클래스의 OnObjectSpawn 호출
        base.OnObjectSpawn();
        
        // 보스 활성화
        isBossActive = true;
        isActionInProgress = false;
        currentPatternIndex = 0;
        
        // EnemyData가 이미 설정되어 있으므로 바로 행동 시작
        StartCoroutine(StartBossActions());
    }
    
    private IEnumerator StartBossActions()
    {
        // 등장 후 초기 딜레이
        yield return new WaitForSeconds(actionStartDelay);
        
        while (isBossActive && CurrentHealth > 0)
        {
            isActionInProgress = true;
            
            // 패턴 선택 (간단한 교대 방식)
            BossPattern currentPattern = useRandomOrder ? 
                (BossPattern)Random.Range(0, 2) : 
                (BossPattern)(currentPatternIndex % 2);
            
            Debug.Log($"Calamity Boss starting pattern: {currentPattern}");
            
            // 패턴 실행
            switch (currentPattern)
            {
                case BossPattern.SummonEnemies:
                    yield return ExecuteSummonPattern();
                    break;
                    
                case BossPattern.BulletHell:
                    yield return ExecuteBulletHellPattern();
                    break;
            }
            
            // 패턴 완료
            isActionInProgress = false;
            currentPatternIndex++;
        }
    }
    
    private IEnumerator ExecuteSummonPattern()
    {
        if (currentWaveData == null || currentWaveData.enemies.Count == 0)
        {
            Debug.LogWarning("No enemies available in current wave data for summoning");
            yield return new WaitForSeconds(1f);
            yield break;
        }
        
        Debug.Log($"Summoning {summonCount} enemies using wave formation settings");
        
        // 웨이브의 enemies 배열에서 랜덤하게 한 종류 선택
        var randomEnemyEntry = currentWaveData.enemies[Random.Range(0, currentWaveData.enemies.Count)];
        EnemyData enemyToSpawn = randomEnemyEntry.enemyData;
        
        if (enemyToSpawn == null)
        {
            Debug.LogWarning("Selected enemy data is null");
            yield return new WaitForSeconds(1f);
            yield break;
        }
        
        Debug.Log($"Summoning {summonCount} x {enemyToSpawn.enemyName}");
        
        // 웨이브의 Formation Settings를 사용해서 플레이어 기준으로 소환 위치 생성
        List<Vector2> spawnPositions = GenerateFormationPositions(currentWaveData.spawnSettings, summonCount);
        
        // SpawnWarning 후 적들 소환
        yield return StartCoroutine(ShowWarningsAndSpawnEnemies(spawnPositions, enemyToSpawn));
        
        // 소환 완료 후 3초 휴식
        yield return new WaitForSeconds(3f);
    }
    
    private IEnumerator ExecuteBulletHellPattern()
    {
        if (EnemyData == null || EnemyData.enemyType != EnemyType.Boss)
        {
            Debug.LogWarning("Boss data not found or not boss type!");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        // 모든 패턴 중에서 랜덤 선택
        BulletPatternType[] allPatterns = { BulletPatternType.Blast, BulletPatternType.Spiral, BulletPatternType.Flower };
        BulletPatternType selectedPattern = allPatterns[Random.Range(0, allPatterns.Length)];
        
        Debug.Log($"Starting random bullet hell pattern: {selectedPattern}");
        
        // 선택된 패턴 실행
        switch (selectedPattern)
        {
            case BulletPatternType.Blast:
                yield return StartCoroutine(BlastPattern(
                    transform, 
                    EnemyData.blastSettings.shotNum, 
                    EnemyData.blastSettings.volly, 
                    EnemyData.blastSettings.spread, 
                    EnemyData.blastSettings.shotTime
                ));
                break;
                
            case BulletPatternType.Spiral:
                // 랜덤하게 시계방향 또는 반시계방향 선택
                bool randomClockwise = Random.value > 0.5f;
                yield return StartCoroutine(SpiralPattern(
                    transform,
                    EnemyData.spiralSettings.spiralTime,
                    EnemyData.spiralSettings.directions,
                    EnemyData.spiralSettings.rotationSpeed,
                    EnemyData.spiralSettings.waitTime,
                    randomClockwise
                ));
                break;
                
            case BulletPatternType.Flower:
                yield return StartCoroutine(FlowerPattern(
                    transform,
                    EnemyData.flowerSettings.flowerTime,
                    EnemyData.flowerSettings.directions,
                    EnemyData.flowerSettings.phaseTime,
                    EnemyData.flowerSettings.rotationAngle,
                    EnemyData.flowerSettings.waitTime
                ));
                break;
        }
        
        Debug.Log("Bullet hell pattern completed");
        
        // 패턴 완료 후 1초 휴식
        yield return new WaitForSeconds(1f);
    }
    
    /// <summary>
    /// 웨이브의 Formation Settings를 사용해서 플레이어 기준으로 소환 위치 생성
    /// </summary>
    private List<Vector2> GenerateFormationPositions(WaveData.SpawnSettings settings, int count)
    {
        if (playerTarget == null)
        {
            Debug.LogWarning("Player target is null, using boss position as fallback");
            return new List<Vector2> { transform.position };
        }
        
        Vector2 playerPos = playerTarget.position;
        List<Vector2> positions = new List<Vector2>();
        
        switch (settings.formation)
        {
            case WaveData.SpawnFormation.Surround:
                positions = GenerateSurroundPositions(count, settings.surroundDistance, settings.angleOffset, playerPos);
                break;
            case WaveData.SpawnFormation.Rectangle:
                positions = GenerateRectanglePositions(count, settings.surroundDistance, playerPos);
                break;
            case WaveData.SpawnFormation.Line:
                positions = GenerateLinePositions(count, settings.lineStart, settings.lineEnd);
                break;
            case WaveData.SpawnFormation.Random:
                positions = GenerateRandomPositions(count, playerPos);
                break;
            case WaveData.SpawnFormation.EdgeRandom:
            default:
                // 기본값 - 플레이어 주변 랜덤
                positions = GenerateRandomPositions(count, playerPos);
                break;
        }
        
        return positions;
    }
    
    private List<Vector2> GenerateSurroundPositions(int count, float radius, float angleOffset, Vector2 center)
    {
        List<Vector2> positions = new List<Vector2>();
        float angleStep = 360f / count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep + angleOffset;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            positions.Add(position);
        }
        
        return positions;
    }
    
    private List<Vector2> GenerateRectanglePositions(int count, float distance, Vector2 center)
    {
        List<Vector2> positions = new List<Vector2>();
        int enemiesPerSide = Mathf.CeilToInt(count / 4f);
        int remainingEnemies = count;
        
        // 위쪽
        int topCount = Mathf.Min(enemiesPerSide, remainingEnemies);
        for (int i = 0; i < topCount; i++)
        {
            float t = (topCount == 1) ? 0.5f : (float)i / (topCount - 1);
            float xPos = center.x - distance + distance * 2 * t;
            float yPos = center.y + distance;
            positions.Add(new Vector2(xPos, yPos));
        }
        remainingEnemies -= topCount;
        
        // 오른쪽, 아래쪽, 왼쪽도 동일한 방식으로 처리
        if (remainingEnemies > 0)
        {
            int rightCount = Mathf.Min(enemiesPerSide, remainingEnemies);
            for (int i = 0; i < rightCount; i++)
            {
                float t = (rightCount == 1) ? 0.5f : (float)i / (rightCount - 1);
                float xPos = center.x + distance;
                float yPos = center.y + distance - distance * 2 * t;
                positions.Add(new Vector2(xPos, yPos));
            }
            remainingEnemies -= rightCount;
        }
        
        return positions;
    }
    
    private List<Vector2> GenerateLinePositions(int count, Vector2 start, Vector2 end)
    {
        List<Vector2> positions = new List<Vector2>();
        
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            Vector2 position = Vector2.Lerp(start, end, t);
            positions.Add(position);
        }
        
        return positions;
    }
    
    private List<Vector2> GenerateRandomPositions(int count, Vector2 center)
    {
        List<Vector2> positions = new List<Vector2>();
        
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(3f, 8f);
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            positions.Add(position);
        }
        
        return positions;
    }
    
    /// <summary>
    /// SpawnWarning 표시 후 적들 소환 (WaveManager와 동일한 방식)
    /// </summary>
    private IEnumerator ShowWarningsAndSpawnEnemies(List<Vector2> positions, EnemyData enemyToSpawn)
    {
        if (ObjectPool.Instance == null || positions.Count == 0)
        {
            Debug.LogWarning("ObjectPool is null or no positions available");
            yield break;
        }
        
        // SpawnWarning이 ObjectPool에 등록되어 있는지 확인
        if (!ObjectPool.Instance.DoesPoolExist("SpawnWarning"))
        {
            Debug.LogWarning("SpawnWarning pool does not exist, spawning enemies directly");
            // 경고 없이 바로 소환
            SpawnEnemiesDirectly(positions, enemyToSpawn);
            yield break;
        }
        
        List<GameObject> warnings = new List<GameObject>();
        
        // 경고 표시 생성
        foreach (Vector2 pos in positions)
        {
            GameObject warning = ObjectPool.Instance.SpawnFromPool("SpawnWarning", pos, Quaternion.identity);
            if (warning != null)
            {
                warnings.Add(warning);
            }
        }
        
        Debug.Log($"Showing {warnings.Count} spawn warnings for boss summon");
        
        // 경고 표시 시간 (1초)
        yield return new WaitForSeconds(1f);
        
        // 경고 표시 비활성화
        foreach (GameObject warning in warnings)
        {
            if (warning != null)
            {
                ObjectPool.Instance.ReturnToPool("SpawnWarning", warning);
            }
        }
        
        // 적들 소환
        SpawnEnemiesDirectly(positions, enemyToSpawn);
    }
    
    /// <summary>
    /// 실제 적들을 소환하는 메서드
    /// </summary>
    private void SpawnEnemiesDirectly(List<Vector2> positions, EnemyData enemyToSpawn)
    {
        for (int i = 0; i < Mathf.Min(summonCount, positions.Count); i++)
        {
            Vector2 spawnPos = positions[i];
            
            var spawnedEnemy = ObjectPool.Instance.SpawnFromPool(
                enemyToSpawn.enemyName, // enemyName 사용 (ObjectPool 등록명)
                spawnPos, 
                Quaternion.identity
            );
            
            if (spawnedEnemy != null)
            {
                var enemy = spawnedEnemy.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.SetEnemyData(enemyToSpawn);
                    enemy.Initialize(playerTarget);
                    enemy.OnObjectSpawn();
                    
                    Debug.Log($"Boss summoned {enemyToSpawn.enemyName} at {spawnPos}");
                }
            }
        }
    }
    
    public new void TakeDamage(float damage)
    {
        // 부모 클래스의 TakeDamage 호출
        base.TakeDamage(damage);
        
        // 보스가 죽으면 모든 행동 중지
        if (CurrentHealth <= 0)
        {
            OnBossDeath();
        }
    }
    
    private void OnBossDeath()
    {
        isBossActive = false;
        
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        
        // WaveManager에게 보스 처치 알림
        if (waveManager != null)
        {
            waveManager.OnBossDefeated();
        }
        
        // 보스 파괴 (ObjectPool을 사용하지 않으므로 직접 파괴)
        Destroy(gameObject);
        
        Debug.Log("Calamity Boss defeated!");
    }
    
    // 탄막 패턴 구현
    private IEnumerator BlastPattern(Transform shooter, int shotNum, int volly, float spread, float shotTime)
    {
        int originalVolly = volly; // 원본 volly 값 저장
        int currentVollyIndex = 1; // 현재 volly 인덱스 (1부터 시작)
        
        if (shotNum <= 1)
        {
            // 단발 직사 - 플레이어 방향으로
            if (playerTarget != null)
            {
                Vector2 directionToPlayer = (playerTarget.position - shooter.position).normalized;
                float angleToPlayer = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                FireBullet(shooter.position, angleToPlayer, BulletPatternType.Blast);
            }
        }
        else
        {
            while (volly > 0)
            {
                // 각 volly마다 플레이어 방향을 다시 계산 (플레이어가 움직일 수 있으므로)
                if (playerTarget != null)
                {
                    Vector2 directionToPlayer = (playerTarget.position - shooter.position).normalized;
                    float angleToPlayer = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    
                    // 각 volly마다 spread가 누적되어 증가
                    float currentSpread = spread * currentVollyIndex;
                    float bulletRot = angleToPlayer - (currentSpread / 2); // 플레이어 방향 중심으로 확산 시작
                    
                    for (int i = 0; i < shotNum; i++)
                    {
                        FireBullet(shooter.position, bulletRot, BulletPatternType.Blast);
                        bulletRot += currentSpread / (shotNum - 1); // 다음 탄환 각도
                        
                        if (shotTime > 0)
                        {
                            yield return new WaitForSeconds(shotTime);
                        }
                    }
                }
                
                volly--;
                currentVollyIndex++; // 다음 volly로 인덱스 증가
            }
        }
    }
    
    private IEnumerator SpiralPattern(Transform shooter, float spiralTime, int directions, float rotationSpeed, float waitTime, bool clockwise)
    {
        float bulletRot = 0.0f;
        float rotationDirection = clockwise ? 1f : -1f;
        
        while (spiralTime > 0)
        {
            // 현재 나선 패턴 발사 (여러 방향으로 동시에)
            for (int i = 0; i < directions; i++)
            {
                FireBullet(shooter.position, bulletRot, BulletPatternType.Spiral);
                bulletRot += 360.0f / directions; // 균등하게 분산
            }
            
            // 다음 프레임을 위해 전체 패턴 회전
            bulletRot += rotationSpeed * rotationDirection * waitTime;
            
            // 각도 정규화 (0-360도 범위 유지)
            if (bulletRot > 360)
            {
                bulletRot -= 360;
            }
            else if (bulletRot < 0)
            {
                bulletRot += 360;
            }
            
            spiralTime -= waitTime;
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    private IEnumerator FlowerPattern(Transform shooter, float flowerTime, int directions, float phaseTime, float rotationAngle, float waitTime)
    {
        float currentBaseAngle = 0.0f;
        
        while (flowerTime > 0)
        {
            // 각 단계(Phase) 동안 고정된 방향으로 발사
            float currentPhaseTime = phaseTime;
            
            while (currentPhaseTime > 0 && flowerTime > 0)
            {
                // 현재 기준 각도에서 directions 수만큼 균등하게 발사
                for (int i = 0; i < directions; i++)
                {
                    float bulletAngle = currentBaseAngle + (360.0f / directions) * i;
                    FireBullet(shooter.position, bulletAngle, BulletPatternType.Flower);
                }
                
                currentPhaseTime -= waitTime;
                flowerTime -= waitTime;
                yield return new WaitForSeconds(waitTime);
            }
            
            // 단계 종료 시 다음 단계를 위해 기준 각도 변경
            if (flowerTime > 0)
            {
                // 랜덤하게 시계방향 또는 반시계방향으로 회전
                bool clockwise = Random.value > 0.5f;
                currentBaseAngle += clockwise ? rotationAngle : -rotationAngle;
                
                // 각도 정규화
                if (currentBaseAngle >= 360)
                {
                    currentBaseAngle -= 360;
                }
                else if (currentBaseAngle < 0)
                {
                    currentBaseAngle += 360;
                }
                
                Debug.Log($"Flower pattern phase changed - New base angle: {currentBaseAngle:F1}°, Direction: {(clockwise ? "Clockwise" : "Counter-clockwise")}");
            }
        }
    }
    
    private void FireBullet(Vector3 position, float angle, BulletPatternType patternType)
    {
        if (bulletPool == null) return;

        var bullet = bulletPool.SpawnFromPool(bulletPoolTag, position, Quaternion.Euler(0, 0, angle));
        if (bullet != null)
        {
            var bossBullet = bullet.GetComponent<BossBullet>();
            if (bossBullet != null)
            {
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                float bulletSpeed = 8f;
                float bulletLifetime = 10f;
                
                // 패턴 타입에 따라 다른 speed와 lifetime 사용
                if (EnemyData != null && EnemyData.enemyType == EnemyType.Boss)
                {
                    switch (patternType)
                    {
                        case BulletPatternType.Blast:
                            bulletSpeed = EnemyData.blastSettings.bulletSpeed;
                            bulletLifetime = EnemyData.blastSettings.bulletLifetime;
                            break;
                        case BulletPatternType.Spiral:
                            bulletSpeed = EnemyData.spiralSettings.bulletSpeed;
                            bulletLifetime = EnemyData.spiralSettings.bulletLifetime;
                            break;
                        case BulletPatternType.Flower:
                            bulletSpeed = EnemyData.flowerSettings.bulletSpeed;
                            bulletLifetime = EnemyData.flowerSettings.bulletLifetime;
                            break;
                    }
                }
                
                float bulletDamage = Damage; // Enemy 클래스의 Damage 속성 사용 (bossDamage 값)
                
                bossBullet.Initialize(
                    direction: direction,
                    speed: bulletSpeed,
                    damage: bulletDamage,
                    lifetime: bulletLifetime // 패턴별로 다른 생존 시간
                );
            }
        }
    }
    
    // 디버그용 - 현재 상태 확인
    private void OnGUI()
    {
        if (!isBossActive) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Boss Health: {CurrentHealth:F1}/{MaxHealth:F1}");
        GUILayout.Label($"Pattern Index: {currentPatternIndex}");
        GUILayout.Label($"Action In Progress: {isActionInProgress}");
        GUILayout.EndArea();
    }
}