using UnityEngine;

public class ChasingState : IState
{
    // ������Ʈ ĳ��
    private readonly Transform enemyTransform;
    private readonly Transform playerTransform;
    private readonly Rigidbody2D rb;
    private readonly Enemy enemyStats;
    private readonly SpriteRenderer spriteRenderer;

    // ������ ����
    private readonly Vector2 directionVector = Vector2.zero;
    private readonly Vector2 velocityVector = Vector2.zero;

    // ����ȭ�� ���� ĳ�� ����
    private float currentMoveSpeed;
    private bool wasFlipped;
    private GameManager gameManager;

    public ChasingState(EnemyAI enemyAI)
    {
        enemyTransform = enemyAI.transform;
        enemyStats = enemyAI.GetComponent<Enemy>();
        rb = enemyAI.GetComponent<Rigidbody2D>();
        spriteRenderer = enemyAI.spriteRenderer;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        gameManager = GameManager.Instance;
    }

    public void OnEnter()
    {
        // ���� ���� �� �ӵ� �ʱ�ȭ
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        currentMoveSpeed = enemyStats.MoveSpeed;
        wasFlipped = spriteRenderer.flipX;
    }

    public void OnExit()
    {
        // ���� ���� �� �ٿ ȿ�� ����
        enemyStats.ResetBounceEffect();

        // ���� �ӵ� �ʱ�ȭ
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Update()
    {
        if (ShouldSkipUpdate()) return;

        CalculateDirection();
        UpdateMovement();
        UpdateVisuals();
    }

    private bool ShouldSkipUpdate()
    {
        return enemyStats.IsKnockBack ||
               playerTransform == null ||
               gameManager.currentGameState != GameState.Playing;
    }

    private void CalculateDirection()
    {
        float dx = playerTransform.position.x - enemyTransform.position.x;
        float dy = playerTransform.position.y - enemyTransform.position.y;

        directionVector.Set(dx, dy);
        float magnitude = directionVector.magnitude;

        if (magnitude > 0)
        {
            // ����ȭ ����ȭ
            float invMagnitude = 1f / magnitude;
            directionVector.Set(
                directionVector.x * invMagnitude,
                directionVector.y * invMagnitude
            );
        }
    }

    private void UpdateMovement()
    {
        if (rb != null)
        {
            // �ӵ� ���� ����
            velocityVector.Set(
                directionVector.x * currentMoveSpeed,
                directionVector.y * currentMoveSpeed
            );
            rb.linearVelocity = velocityVector;
        }
        else
        {
            // Transform ���� �̵�
            Vector3 currentPos = enemyTransform.position;
            currentPos.x += directionVector.x * currentMoveSpeed * Time.deltaTime;
            currentPos.y += directionVector.y * currentMoveSpeed * Time.deltaTime;
            enemyTransform.position = currentPos;
        }
    }

    private void UpdateVisuals()
    {
        // ��������Ʈ �ø� ����ȭ
        bool shouldFlip = directionVector.x < 0;
        if (wasFlipped != shouldFlip)
        {
            spriteRenderer.flipX = shouldFlip;
            wasFlipped = shouldFlip;
        }

        // �ٿ ȿ�� ������Ʈ
        enemyStats.UpdateBounceEffect();
    }

    public void FixedUpdate()
    {
        // FixedUpdate�� ������� ���� - ��� ���� ������Ʈ�� Update���� ó��
    }
}