using System.Collections.Generic;
using UnityEngine;

public class SawbladeProjectile : BaseProjectile
{
    [SerializeField] private float rotationSpeed = 720f;
    private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>(8); // �ʱ� �뷮 ����
    private int bounceCount;
    private Camera mainCamera;
    private const int MAX_BOUNCES = 2;
    private float angleZ;

    // ī�޶� ��� ĳ��
    private float cameraHeight;
    private float cameraWidth;
    private Vector2 cameraPosition;
    private float leftBound;
    private float rightBound;
    private float bottomBound;
    private float topBound;
    private Vector2 currentPosition;
    private Vector2 newDirection;
    private Vector2 knockbackForce;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        UpdateCameraBounds();
    }

    private void UpdateCameraBounds()
    {
        if (mainCamera == null) return;

        cameraHeight = 2f * mainCamera.orthographicSize;
        cameraWidth = cameraHeight * mainCamera.aspect;
        cameraPosition = mainCamera.transform.position;

        leftBound = cameraPosition.x - cameraWidth * 0.5f;
        rightBound = cameraPosition.x + cameraWidth * 0.5f;
        bottomBound = cameraPosition.y - cameraHeight * 0.5f;
        topBound = cameraPosition.y + cameraHeight * 0.5f;
    }
    public override void OnObjectSpawn()
    {
        base.OnObjectSpawn();
        bounceCount = 0;
        hitEnemies.Clear();
        angleZ = 0f;
        UpdateCameraBounds();
    }
    protected override void Update()
    {
        // ȸ�� ����ȭ
        angleZ = (angleZ + rotationSpeed * Time.deltaTime) % 360f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleZ);

        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        if (Time.frameCount % 5 == 0) // 5�����Ӹ��� ��� üũ
        {
            UpdateCameraBounds();
            CheckCameraBounds();
        }
    }

    private void CheckCameraBounds()
    {
        currentPosition = transform.position;
        bool bounced = false;
        newDirection = direction;

        if (currentPosition.x <= leftBound || currentPosition.x >= rightBound)
        {
            newDirection.x = -direction.x;
            bounced = true;
            currentPosition.x = Mathf.Clamp(currentPosition.x, leftBound, rightBound);
        }

        if (currentPosition.y <= bottomBound || currentPosition.y >= topBound)
        {
            newDirection.y = -direction.y;
            bounced = true;
            currentPosition.y = Mathf.Clamp(currentPosition.y, bottomBound, topBound);
        }

        if (bounced)
        {
            bounceCount++;
            transform.position = currentPosition;
            direction = newDirection;
            hitEnemies.Clear();

            if (bounceCount > MAX_BOUNCES)
            {
                ReturnToPool();
            }
        }
    }
    protected override void ApplyDamageAndEffects(Enemy enemy)
    {
        if (!hitEnemies.Add(enemy)) return; // HashSet.Add�� ��ȯ�� Ȱ��

        enemy.TakeDamage(damage);

        if (knockbackPower > 0)
        {
            knockbackForce.x = direction.x * knockbackPower;
            knockbackForce.y = direction.y * knockbackPower;
            enemy.ApplyKnockback(knockbackForce);
        }

        // SawBlade는 벽에 튕기면서 계속 날아가야 하므로 HandlePenetration() 호출하지 않음
        // 화면 경계에서 MAX_BOUNCES만큼 튕긴 후에만 ReturnToPool() 됨
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        bounceCount = 0;
        hitEnemies.Clear();
        angleZ = 0f;
    }
}