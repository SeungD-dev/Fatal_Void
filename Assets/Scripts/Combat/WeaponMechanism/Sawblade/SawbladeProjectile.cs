using System.Collections.Generic;
using UnityEngine;

public class SawbladeProjectile : BaseProjectile
{
    [SerializeField] private float rotationSpeed = 720f;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
    private int bounceCount = 0;
    private Camera mainCamera;
    private const int MAX_BOUNCES = 2;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
    }

    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        bounceCount = 0;
        hitEnemies.Clear();
    }

    protected override void Update()
    {
        // 지속적인 회전
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 투사체 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        CheckCameraBounds();
    }

    private void CheckCameraBounds()
    {
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        Vector2 cameraPosition = mainCamera.transform.position;
        float leftBound = cameraPosition.x - cameraWidth / 2;
        float rightBound = cameraPosition.x + cameraWidth / 2;
        float bottomBound = cameraPosition.y - cameraHeight / 2;
        float topBound = cameraPosition.y + cameraHeight / 2;

        Vector2 position = transform.position;
        bool bounced = false;
        Vector2 newDirection = direction;

        // 수평 경계 체크
        if (position.x <= leftBound || position.x >= rightBound)
        {
            newDirection.x = -direction.x;
            bounced = true;
            position.x = Mathf.Clamp(position.x, leftBound, rightBound);
        }

        // 수직 경계 체크
        if (position.y <= bottomBound || position.y >= topBound)
        {
            newDirection.y = -direction.y;
            bounced = true;
            position.y = Mathf.Clamp(position.y, bottomBound, topBound);
        }

        if (bounced)
        {
            bounceCount++;
            Debug.Log($"Bounce count: {bounceCount}"); // 디버그용

            transform.position = position;
            direction = newDirection.normalized;
            hitEnemies.Clear();

            // 3번째 벽과 충돌할 때 (bounceCount가 2를 초과할 때) 풀로 반환
            if (bounceCount > MAX_BOUNCES)
            {
                ReturnToPool();
            }
        }
    }

    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        if (hitEnemies.Contains(enemy)) return;

        hitEnemies.Add(enemy);
        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            Vector2 knockbackForce = direction * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        HandlePenetration();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bounceCount = 0;
        hitEnemies.Clear();
    }
}