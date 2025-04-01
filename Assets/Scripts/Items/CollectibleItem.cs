using UnityEngine;
using System.Collections;

public class CollectibleItem : MonoBehaviour, IPooledObject
{
    [Header("Movement Settings")]
    [SerializeField] private float basemagnetDistance = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private ItemType itemType;
    public ItemType GetItemType() => itemType;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerStats playerStats;
    private CombatController combatController;

    private float currentMagnetSpeed;
    private float currentMagnetDistance;
    private bool isBeingMagneted;
    private bool isPulledByMagnet;
    private bool isAutoMagneted;

    private bool isMagnetable = true;
    private int goldAmount;

    private bool isInitialized;
    private bool isRegistered;
    private bool isCollected;

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

        // Set default magnetability based on item type
        isMagnetable = (itemType == ItemType.ExperienceSmall ||
                        itemType == ItemType.ExperienceMedium ||
                        itemType == ItemType.ExperienceLarge ||
                        itemType == ItemType.Gold);
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
                
                StartCoroutine(TryRegisterLater());
                return;
            }
        }

        
        combatController.RegisterCollectible(this);
        isRegistered = true;
    }

    private IEnumerator TryRegisterLater()
    {
        
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

        
        isMagnetable = dropInfo.isMagnetable &&
            (itemType == ItemType.ExperienceSmall ||
             itemType == ItemType.ExperienceMedium ||
             itemType == ItemType.ExperienceLarge ||
             itemType == ItemType.Gold);

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
        
        isCollected = false;
        isBeingMagneted = false;
        isPulledByMagnet = false;
        isRegistered = false;

        
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }

        
        if (!isInitialized)
        {
            Initialize();
        }
        else
        {
            
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

            
            movementDirection.x = playerTransform.position.x - transform.position.x;
            movementDirection.y = playerTransform.position.y - transform.position.y;
            float magnitude = Mathf.Sqrt(movementDirection.x * movementDirection.x + movementDirection.y * movementDirection.y);

            
            if (magnitude > 0.001f)
            {
                
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
        
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;
        if (combatController == null) return;

        
        isCollected = true;

        
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

        // Don't reset isInitialized here - we want to maintain our initialization state
        // when the object is re-enabled from the pool
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