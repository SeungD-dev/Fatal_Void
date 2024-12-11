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
    private float currentMovementSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found!");
            return;
        }

        
        currentMovementSpeed = playerStats.MovementSpeed;

        playerStats.OnMovementSpeedChanged += HandleMovementSpeedChanged;
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void HandleMovementSpeedChanged(float newSpeed)
    {
        currentMovementSpeed = Mathf.Max(minMovementSpeed, newSpeed);
    }

    private void HandleMovement()
    {
        Vector2 movement = new Vector2(joystick.Horizontal, joystick.Vertical);
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        rb.linearVelocity = movement * currentMovementSpeed;

        if (movement.x != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = movement.x < 0;
        }
    }

    private void FixedUpdate()
    {
        if (enabled && playerStats != null)
        {
            HandleMovement();
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        enabled = (newState == GameState.Playing);
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
