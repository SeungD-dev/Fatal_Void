using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private FloatingJoystick joystick;

    [Header("Components")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    public PlayerStats playerStats;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Rigidbody2D 설정
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        // GameManager로부터 PlayerStats 참조 가져오기
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found!");
        }

        // 게임 상태 변경 감지
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // Paused 상태에서는 이동 불가
        enabled = (newState == GameState.Playing);
    }

    private void FixedUpdate()
    {
        if (enabled && playerStats != null)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        Vector2 movement = new Vector2(joystick.Horizontal, joystick.Vertical);
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // PlayerStats의 movementSpeed 사용
        rb.linearVelocity = movement * playerStats.movementSpeed;

        
        if (movement.x != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }
}
