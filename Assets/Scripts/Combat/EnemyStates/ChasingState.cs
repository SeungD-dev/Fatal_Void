using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private readonly Enemy enemyStats;
    private readonly Transform playerTransform;
    private readonly Rigidbody2D rb;
    public ChasingState(EnemyAI enemyAI)
    {
        this.enemyAI = enemyAI;
        this.enemyTransform = enemyAI.transform;
        this.enemyStats = enemyAI.GetComponent<Enemy>();
        this.rb = enemyAI.GetComponent<Rigidbody2D>();
        this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void OnEnter()
    {

    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();
    }

    public void Update()
    {
        
        if (enemyStats.IsKnockBack) return;

        if (playerTransform != null && GameManager.Instance.currentGameState == GameState.Playing)
        {
            
            Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
            if (direction.x != 0)
            {
                enemyAI.spriteRenderer.flipX = direction.x < 0;
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
    }


    public void FixedUpdate()
    {
    }


}