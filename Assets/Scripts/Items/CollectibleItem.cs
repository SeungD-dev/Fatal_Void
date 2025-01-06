using UnityEngine;


public class CollectibleItem : MonoBehaviour, IPooledObject
{
    [Header("Movement Settings")]
    [SerializeField] private float basemagnetDistance = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private ItemType itemType;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isBeingMagneted = false;
    private bool isPulledByMagnet = false;
    private float currentMagnetSpeed;
    private float currentMagnetDistance;
    private bool isAutoMagneted = false;
    private PlayerStats playerStats;
    private bool isMagnetable = true;
    private int goldAmount = 0;
    private bool isInitialized = false;

    private CombatController combatController;
    private bool isCollected = false;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 3f;
        }
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (isInitialized) return;

        FindPlayer();
        InitializePlayerStats();

        if (GameManager.Instance?.CombatController != null)
        {
            combatController = GameManager.Instance.CombatController;
            combatController.RegisterCollectible(this);
        }

        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged += HandleMagnetEffectChanged;
        }

        isInitialized = true;
    }

    private void RegisterToManager()
    {
        if (GameManager.Instance?.CombatController != null)
        {
            GameManager.Instance.CombatController.RegisterCollectible(this);
        }
    }

    public void Initialize(AdditionalDrop dropInfo)
    {
        itemType = dropInfo.itemType;
        isMagnetable = dropInfo.isMagnetable;
    }

    public void SetGoldAmount(int amount)
    {
        goldAmount = amount;
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    private void InitializePlayerStats()
    {
        if (playerTransform != null && playerStats == null)
        {
            playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                UpdateMagnetDistance();
            }
        }
    }

    public void OnObjectSpawn()
    {
        isBeingMagneted = false;
        isPulledByMagnet = false;
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;
        goldAmount = 0;
        isInitialized = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Initialize();
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
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        UpdateMagnetDistance();

        if (!isMagnetable) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (isAutoMagneted || isPulledByMagnet || distance <= currentMagnetDistance)
        {
            isBeingMagneted = true;
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            float speed = isAutoMagneted ? magnetSpeed * 2f : currentMagnetSpeed;
            rb.linearVelocity = direction * speed;
        }
        else if (isBeingMagneted && !isPulledByMagnet && !isAutoMagneted)
        {
            isBeingMagneted = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void PullToPlayer(float magnetForce)
    {
        if (!isMagnetable) return;
        isPulledByMagnet = true;
        currentMagnetSpeed = magnetForce;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isCollected || combatController == null) return;

        isCollected = true; // �ߺ� ȣ�� ����

        // ���� ������ �����Ͽ� ����
        var itemTypeToApply = itemType;
        var goldAmountToApply = goldAmount;

        // ���� ���� �� ȿ�� ����
        combatController.UnregisterCollectible(this);
        combatController.ApplyItemEffect(itemTypeToApply, goldAmountToApply);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        ObjectPool.Instance.ReturnToPool(itemTypeToApply.ToString(), gameObject);
    }


    private void HandleMagnetEffectChanged(bool isActive)
    {
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
        isCollected = false;
        if (combatController != null)
        {
            combatController.UnregisterCollectible(this);
        }
        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged -= HandleMagnetEffectChanged;
        }
        isInitialized = false;
    }
    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged -= HandleMagnetEffectChanged;
        }
        GameManager.Instance?.CombatController?.UnregisterCollectible(this);
    }
}