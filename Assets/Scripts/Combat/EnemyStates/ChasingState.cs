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
        Debug.Log($"{enemyAI.gameObject.name} stopped chasing");
    }

    public void Update()
    {
        // GameManager 상태 확인 추가
        if (playerTransform != null && GameManager.Instance.currentGameState == GameState.Playing)
        {
            Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
            enemyTransform.Translate(direction * enemyStats.MoveSpeed * Time.deltaTime);
        }
    }


    public void FixedUpdate()
    {
    }
}
