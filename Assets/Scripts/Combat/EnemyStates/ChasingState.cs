using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private readonly Transform playerTransform;
    private readonly Rigidbody2D rb;
    private readonly Enemy enemyStats;
    private readonly SpriteRenderer spriteRenderer;

    
    private readonly Vector2 directionVector = Vector2.zero;
    private readonly Vector2 velocityVector = Vector2.zero;

    
    private float currentMoveSpeed;
    private bool wasFlipped;
    private GameManager gameManager;


    public ChasingState(EnemyAI enemyAI)
    {
        this.enemyAI = enemyAI ?? throw new System.ArgumentNullException(nameof(enemyAI));
        enemyTransform = enemyAI.transform;
        enemyStats = enemyAI.GetComponent<Enemy>();
        rb = enemyAI.GetComponent<Rigidbody2D>();
        spriteRenderer = enemyAI.spriteRenderer;
        gameManager = GameManager.Instance;
    }

    private Transform PlayerTransform => enemyAI.PlayerTransform;

    public void OnEnter()
    {
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        currentMoveSpeed = enemyStats.MoveSpeed;
        wasFlipped = spriteRenderer.flipX;
    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Update()
    {
        if (enemyAI == null || !IsValidState()) return;

        CalculateDirection();
        UpdateMovement();
        UpdateVisuals();
    }
    private bool IsValidState()
    {
        return !enemyStats.IsKnockBack &&
               enemyAI.PlayerTransform != null &&
               gameManager.currentGameState == GameState.Playing;
    }


    private void CalculateDirection()
    {
        var playerPos = enemyAI.PlayerTransform.position;
        var enemyPos = enemyTransform.position;

        float dx = playerPos.x - enemyPos.x;
        float dy = playerPos.y - enemyPos.y;

        directionVector.Set(dx, dy);
        float magnitude = directionVector.magnitude;

        if (magnitude > 0)
        {
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
            
            velocityVector.Set(
                directionVector.x * currentMoveSpeed,
                directionVector.y * currentMoveSpeed
            );
            rb.linearVelocity = velocityVector;
        }
        else
        {
            
            Vector3 currentPos = enemyTransform.position;
            currentPos.x += directionVector.x * currentMoveSpeed * Time.deltaTime;
            currentPos.y += directionVector.y * currentMoveSpeed * Time.deltaTime;
            enemyTransform.position = currentPos;
        }
    }

    private void UpdateVisuals()
    {
        
        bool shouldFlip = directionVector.x < 0;
        if (wasFlipped != shouldFlip)
        {
            spriteRenderer.flipX = shouldFlip;
            wasFlipped = shouldFlip;
        }
        enemyStats.UpdateBounceEffect();
    }

    public void FixedUpdate()
    {
        
    }
}