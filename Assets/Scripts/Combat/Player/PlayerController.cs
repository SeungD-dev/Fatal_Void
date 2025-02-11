using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private FloatingJoystick joystick;
    [SerializeField] private float minMovementSpeed = 2f;

    [Header("Components")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerStats playerStats;
    private Animator animator;

    // 캐싱된 값들
    private float currentMovementSpeed;
    private bool wasWalking;
    private bool wasFacingLeft;

    private Vector2 movementVector;

    private void Awake()
    {
        CacheComponents();
        SetupRigidbody();
    }

    private void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found!");
            enabled = false;
            return;
        }

        currentMovementSpeed = playerStats.MovementSpeed;

        // 이벤트 구독
        playerStats.OnMovementSpeedChanged += HandleMovementSpeedChanged;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void HandleMovementSpeedChanged(float newSpeed)
    {
        currentMovementSpeed = Mathf.Max(minMovementSpeed, newSpeed);
    }

    private void HandleMovement()
    {
        if (joystick == null) return;

        float horizontalInput = joystick.Horizontal;
        float verticalInput = joystick.Vertical;

        movementVector.Set(horizontalInput, verticalInput);
        float magnitude = movementVector.magnitude;

        if (magnitude > 1f)
        {
            horizontalInput /= magnitude;
            verticalInput /= magnitude;
            movementVector.Set(horizontalInput, verticalInput);
        }

        rb.linearVelocity = movementVector * currentMovementSpeed;

        // 애니메이션 상태 업데이트
        if (animator != null)
        {
            bool isWalking = magnitude > 0;
            if (wasWalking != isWalking)
            {
                animator.SetBool("IsWalking", isWalking);
                wasWalking = isWalking;
            }
        }

        // 스프라이트 플립
        if (spriteRenderer != null && horizontalInput != 0)
        {
            bool shouldFaceLeft = horizontalInput < 0;
            if (wasFacingLeft != shouldFaceLeft)
            {
                spriteRenderer.flipX = shouldFaceLeft;
                wasFacingLeft = shouldFaceLeft;
            }
        }
    }

    private void FixedUpdate()
    {
        if (enabled)
        {
            HandleMovement();
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        enabled = (newState == GameState.Playing);

        // 일시정지 시 속도 즉시 0으로 설정
        if (!enabled && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnValidate()
    {
        if (minMovementSpeed < 0)
        {
            minMovementSpeed = 0;
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnMovementSpeedChanged -= HandleMovementSpeedChanged;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
}