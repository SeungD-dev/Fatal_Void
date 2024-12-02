using UnityEngine;

public class ChasingState : IState
{
    private readonly EnemyAI enemyAI;
    private readonly Transform enemyTransform;
    private readonly Enemy enemyStats;
    private readonly Transform playerTransform;

    public ChasingState(EnemyAI enemyAI)
    {
        this.enemyAI = enemyAI;
        this.enemyTransform = enemyAI.transform;
        this.enemyStats = enemyAI.GetComponent<Enemy>();
        this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void OnEnter()
    {
        Debug.Log($"{enemyAI.gameObject.name} started chasing player");
    }

    public void OnExit()
    {
        enemyStats.ResetBounceEffect();
        Debug.Log($"{enemyAI.gameObject.name} stopped chasing");
    }

    public void Update()
    {
        if (playerTransform != null && GameManager.Instance.currentGameState == GameState.Playing)
        {
            // 이동 로직
            Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
            if (direction.x != 0)
            {
                enemyAI.spriteRenderer.flipX = direction.x < 0;
            }
            enemyTransform.Translate(direction * enemyStats.MoveSpeed * Time.deltaTime);

            // 바운스 효과 업데이트
            enemyStats.UpdateBounceEffect();
        }
    }

    public void FixedUpdate()
    {
    }


}