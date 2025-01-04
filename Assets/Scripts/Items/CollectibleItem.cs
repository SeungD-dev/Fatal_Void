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

    public void Initialize(AdditionalDrop dropInfo)
    {
        itemType = dropInfo.itemType;
        isMagnetable = dropInfo.isMagnetable;
    }

    public void SetGoldAmount(int amount)
    {
        goldAmount = amount;
    }

    private void Start()
    {
        GameManager.Instance?.CombatController?.RegisterCollectible(this);
        FindPlayer();
        InitializePlayerStats();

        if (playerStats != null)
        {
            playerStats.OnMagnetEffectChanged += HandleMagnetEffectChanged;
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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        GameManager.Instance?.CombatController?.RegisterCollectible(this);
        InitializePlayerStats();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                InitializePlayerStats();
            }
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
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        UpdateMagnetDistance();

        if (!isMagnetable) return;

        if (isAutoMagneted || isPulledByMagnet ||
            Vector2.Distance(transform.position, playerTransform.position) <= currentMagnetDistance)
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
        if (other.CompareTag("Player") && GameManager.Instance?.CombatController != null)
        {
            GameManager.Instance.CombatController.ApplyItemEffect(itemType, goldAmount);
            GameManager.Instance.CombatController.UnregisterCollectible(this);
            rb.linearVelocity = Vector2.zero;
            ObjectPool.Instance.ReturnToPool(itemType.ToString(), gameObject);
        }
    }

    private void HandleMagnetEffectChanged(bool isActive)
    {
        if (isActive)
        {
            isAutoMagneted = true;
            PullToPlayer(magnetSpeed * 2f);
        }
        else
        {
            isAutoMagneted = false;
            isPulledByMagnet = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
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