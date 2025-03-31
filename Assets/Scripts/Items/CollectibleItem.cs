using UnityEngine;
using System.Collections;

public class CollectibleItem : MonoBehaviour, IPooledObject
{
    [Header("Movement Settings")]
    [SerializeField] private float basemagnetDistance = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private ItemType itemType;

    // Cached components
    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerStats playerStats;
    private CombatController combatController;

    // Movement state
    private float currentMagnetSpeed;
    private float currentMagnetDistance;
    private bool isBeingMagneted;
    private bool isPulledByMagnet;
    private bool isAutoMagneted;

    // Item properties
    private bool isMagnetable = true;
    private int goldAmount;

    // State tracking
    private bool isInitialized;
    private bool isRegistered;
    private bool isCollected;

    // Cached vectors for optimization
    private Vector2 movementDirection = Vector2.zero;
    private Vector2 tempVelocity = Vector2.zero;

    private void Awake()
    {
        // Cache components
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 3f;
        }

        // Initialize values
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        else if (!isRegistered && combatController != null)
        {
            RegisterToCombatController();
        }
    }

    private void Initialize()
    {
        if (isInitialized) return;

        FindReferences();
        RegisterToCombatController();
        SubscribeToEvents();

        isInitialized = true;
    }

    private void FindReferences()
    {
        // Get references through GameManager when possible to avoid expensive FindGameObjectWithTag
        if (GameManager.Instance != null)
        {
            if (playerTransform == null && GameManager.Instance.PlayerTransform != null)
            {
                playerTransform = GameManager.Instance.PlayerTransform;
                if (playerStats == null && playerTransform != null)
                {
                    playerStats = playerTransform.GetComponent<PlayerStats>();
                }
            }

            if (combatController == null && GameManager.Instance.CombatController != null)
            {
                combatController = GameManager.Instance.CombatController;
            }
        }

        // Fallback to Find operation if needed (should happen rarely)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (playerStats == null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }
        }
    }

    private void RegisterToCombatController()
    {
        if (combatController == null)
        {
            if (GameManager.Instance?.CombatController != null)
            {
                combatController = GameManager.Instance.CombatController;
            }
            else
            {
                // If we still don't have a reference, try again later
                StartCoroutine(TryRegisterLater());
                return;
            }
        }

        // Register with combat controller
        combatController.RegisterCollectible(this);
        isRegistered = true;
    }

    private IEnumerator TryRegisterLater()
    {
        // Delay and try again to find CombatController
        yield return new WaitForSeconds(0.5f);

        if (!isRegistered && gameObject.activeInHierarchy && !isCollected)
        {
            if (GameManager.Instance?.CombatController != null)
            {
                combatController = GameManager.Instance.CombatController;
                RegisterToCombatController();
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged += HandleMagnetEffectChanged;
        }
    }

    public void Initialize(AdditionalDrop dropInfo)
    {
        itemType = dropInfo.itemType;
        isMagnetable = dropInfo.isMagnetable;
        isCollected = false;

        if (!isInitialized)
        {
            isInitialized = true;
        }
    }

    public void SetGoldAmount(int amount)
    {
        goldAmount = amount;
    }

    public void OnObjectSpawn()
    {
        // Reset state
        isCollected = false;
        isBeingMagneted = false;
        isPulledByMagnet = false;
        isRegistered = false;

        // Reset physics
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Enable colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }

        // Re-initialize if needed
        if (!isInitialized)
        {
            Initialize();
        }
        else
        {
            // Just register to combat controller
            RegisterToCombatController();
        }
    }

    private void UpdateMagnetDistance()
    {
        if (playerStats != null)
        {
            currentMagnetDistance = basemagnetDistance + playerStats.PickupRange;
        }
    }

    private void FixedUpdate()
    {
        if (isCollected) return;

        if (playerTransform == null)
        {
            FindReferences();
            return;
        }

        UpdateMagnetDistance();

        if (!isMagnetable) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (isAutoMagneted || isPulledByMagnet || distance <= currentMagnetDistance)
        {
            isBeingMagneted = true;

            // Optimize: Reuse vector2 instance instead of creating new one
            movementDirection.x = playerTransform.position.x - transform.position.x;
            movementDirection.y = playerTransform.position.y - transform.position.y;
            float magnitude = Mathf.Sqrt(movementDirection.x * movementDirection.x + movementDirection.y * movementDirection.y);

            // Avoid division by zero
            if (magnitude > 0.001f)
            {
                // Normalize and scale by speed
                float speed = isAutoMagneted ? magnetSpeed * 2f : currentMagnetSpeed;
                float invMagnitude = 1f / magnitude;

                tempVelocity.x = movementDirection.x * invMagnitude * speed;
                tempVelocity.y = movementDirection.y * invMagnitude * speed;

                rb.linearVelocity = tempVelocity;
            }
        }
        else if (isBeingMagneted && !isPulledByMagnet && !isAutoMagneted)
        {
            isBeingMagneted = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void PullToPlayer(float magnetForce)
    {
        if (!isMagnetable || isCollected) return;

        isPulledByMagnet = true;
        currentMagnetSpeed = magnetForce;

        // Force an immediate movement to prevent items from appearing stuck
        if (rb != null && playerTransform != null)
        {
            movementDirection.x = playerTransform.position.x - transform.position.x;
            movementDirection.y = playerTransform.position.y - transform.position.y;
            float magnitude = Mathf.Sqrt(movementDirection.x * movementDirection.x + movementDirection.y * movementDirection.y);

            if (magnitude > 0.001f)
            {
                float invMagnitude = 1f / magnitude;
                tempVelocity.x = movementDirection.x * invMagnitude * magnetForce;
                tempVelocity.y = movementDirection.y * invMagnitude * magnetForce;
                rb.linearVelocity = tempVelocity;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Early return checks for performance
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;
        if (combatController == null) return;

        // Mark as collected immediately to prevent double collection
        isCollected = true;

        // Store references for use after pool return
        ItemType itemTypeToApply = itemType;
        int goldAmountToApply = goldAmount;

        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        
        if (itemType != ItemType.Magnet)
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        
        if (itemType == ItemType.Gold)
        {
            SoundManager.Instance?.PlaySound("Coin_sfx", 1f, false);
        }
        else if (itemType == ItemType.ExperienceLarge || itemType == ItemType.ExperienceMedium || itemType == ItemType.ExperienceSmall)
        {
            SoundManager.Instance?.PlaySound("Exp_sfx", 1f, false);
        }

        
        combatController.UnregisterCollectible(this);
        isRegistered = false;

        
        ObjectPool.Instance?.ReturnToPool(itemTypeToApply.ToString(), gameObject);

        
        combatController.ApplyItemEffect(itemTypeToApply, goldAmountToApply);
    }

    private void HandleMagnetEffectChanged(bool isActive)
    {
        if (isCollected) return;

        isAutoMagneted = isActive;

        if (isActive)
        {
            PullToPlayer(magnetSpeed * 2f);
        }
        else
        {
            isPulledByMagnet = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void OnDisable()
    {
        
        if (isCollected) return;

        
        if (isRegistered && combatController != null)
        {
            combatController.UnregisterCollectible(this);
            isRegistered = false;
        }

    }

    private void OnDestroy()
    {
        
        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged -= HandleMagnetEffectChanged;
        }

        
        if (isRegistered)
        {
            GameManager.Instance?.CombatController?.UnregisterCollectible(this);
        }
    }
}