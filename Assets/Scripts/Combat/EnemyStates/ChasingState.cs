using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private Transform playerTransform;
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
        this.enemyAI = enemyAI;
        enemyTransform = enemyAI.transform;
        enemyStats = enemyAI.GetComponent<Enemy>();
        rb = enemyAI.GetComponent<Rigidbody2D>();
        spriteRenderer = enemyAI.spriteRenderer;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }


    private Transform PlayerTransform => enemyAI.PlayerTransform;

    public void OnEnter()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();
    }

    public void Update()
    {
        if (enemyStats.IsKnockBack || playerTransform == null ||
            GameManager.Instance.currentGameState != GameState.Playing)
            return;

        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;

        if (direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * enemyStats.MoveSpeed;
        }
        else
        {
            enemyTransform.Translate(direction * enemyStats.MoveSpeed * Time.deltaTime);
        }

        enemyStats.UpdateBounceEffect();
    }
  

    public void FixedUpdate()
    {
        
    }
}