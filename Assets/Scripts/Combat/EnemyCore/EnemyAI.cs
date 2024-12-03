using System;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    public StateMachine stateMachine;
    protected Enemy enemyStats;
    protected Transform playerTransform;
    [HideInInspector] public SpriteRenderer spriteRenderer;


    protected virtual void Awake()
    {
        enemyStats = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new StateMachine();
        InitializeStates();
    }

    protected virtual void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        // 게임 상태 변경 이벤트 구독
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    protected virtual void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // 게임 상태에 따라 AI 활성화/비활성화
        enabled = (newState == GameState.Playing);
    }

    protected virtual void InitializeStates()
    {
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);
        var dieState = new DieState(this);

        stateMachine.SetState(idleState);
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => IsPlayerAlive() && IsGamePlaying()));
    }

    protected virtual void Update()
    {
        // 게임이 플레이 중일 때만 상태 머신 업데이트
        if (IsGamePlaying())
        {
            stateMachine.Update();
        }
    }

    protected virtual bool IsPlayerAlive()
    {
        return GameManager.Instance.PlayerStats != null &&
               GameManager.Instance.PlayerStats.CurrentHealth > 0;
    }

    protected virtual bool IsGamePlaying()
    {
        return GameManager.Instance.currentGameState == GameState.Playing;
    }
}
