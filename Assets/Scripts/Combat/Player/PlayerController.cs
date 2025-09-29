using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Keyboard Input")]
    private InputAction wasdMovement;
    private InputAction arrowMovement;

    // 캐싱된 값들
    private float currentMovementSpeed;
    private bool wasWalking;
    private bool wasFacingLeft;

    private Vector2 movementVector;

    private void Awake()
    {
        CacheComponents();
        SetupRigidbody();
        SetupKeyboardInput();
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

    private void SetupKeyboardInput()
    {
        // WASD 키보드 입력 설정 (2D 복합 입력)
        wasdMovement = new InputAction("WASDMovement", InputActionType.Value);
        wasdMovement.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // 방향키 입력 설정 (2D 복합 입력)
        arrowMovement = new InputAction("ArrowMovement", InputActionType.Value);
        arrowMovement.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
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

        // 키보드 입력 활성화
        if (wasdMovement != null)
        {
            wasdMovement.Enable();
        }
        if (arrowMovement != null)
        {
            arrowMovement.Enable();
        }

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
        float horizontalInput = 0f;
        float verticalInput = 0f;

        // 조이스틱 입력 (모바일)
        if (joystick != null)
        {
            horizontalInput = joystick.Horizontal;
            verticalInput = joystick.Vertical;
        }

        // 키보드 입력 (PC) - 조이스틱 입력과 합산
        if (wasdMovement != null)
        {
            Vector2 wasdInput = wasdMovement.ReadValue<Vector2>();
            horizontalInput += wasdInput.x;
            verticalInput += wasdInput.y;
        }

        // 방향키 입력 (PC) - 다른 입력과 합산
        if (arrowMovement != null)
        {
            Vector2 arrowInput = arrowMovement.ReadValue<Vector2>();
            horizontalInput += arrowInput.x;
            verticalInput += arrowInput.y;
        }

        movementVector.Set(horizontalInput, verticalInput);
        float magnitude = movementVector.magnitude;

        // 입력 크기 정규화 (대각선 이동 시 속도 제한)
        if (magnitude > 1f)
        {
            movementVector = movementVector.normalized;
        }

        rb.linearVelocity = movementVector * currentMovementSpeed;

        // 애니메이션 상태 업데이트
        if (animator != null)
        {
            bool isWalking = magnitude > 0.1f; // 작은 임계값 추가
            if (wasWalking != isWalking)
            {
                animator.SetBool("IsWalking", isWalking);
                wasWalking = isWalking;
            }
        }

        // 스프라이트 플립
        if (spriteRenderer != null && Mathf.Abs(movementVector.x) > 0.1f)
        {
            bool shouldFaceLeft = movementVector.x < 0;
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

        // 키보드 입력 상태 관리
        if (wasdMovement != null)
        {
            if (enabled)
            {
                wasdMovement.Enable();
            }
            else
            {
                wasdMovement.Disable();
            }
        }
        if (arrowMovement != null)
        {
            if (enabled)
            {
                arrowMovement.Enable();
            }
            else
            {
                arrowMovement.Disable();
            }
        }

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

        // 키보드 입력 정리
        if (wasdMovement != null)
        {
            wasdMovement.Disable();
            wasdMovement.Dispose();
        }
        if (arrowMovement != null)
        {
            arrowMovement.Disable();
            arrowMovement.Dispose();
        }
    }
}