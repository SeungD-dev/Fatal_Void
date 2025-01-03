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
    private DropInfo dropInfo;
    private PlayerStats playerStats;
    private bool isAutoMagneted = false;
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

    public void Initialize(DropInfo info)
    {
        dropInfo = info;
        itemType = info.itemType;
    }

    private void Start()
    {
        GameManager.Instance?.CombatController?.RegisterCollectible(this);
        FindPlayer();
        InitializePlayerStats();

        // PlayerStats의 자석 효과 이벤트 구독
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
                // PlayerStats의 PickupRange가 변경될 때마다 자석 거리 업데이트
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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        GameManager.Instance?.CombatController?.RegisterCollectible(this);

        // Equipment 효과 적용을 위해 PlayerStats 초기화
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
            // PlayerStats의 PickupRange 값을 기반으로 자석 거리 계산
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

        if (dropInfo == null || !dropInfo.isMagnetable)
        {
            return;
        }

        // isAutoMagneted가 true이거나 기존 자석 효과 범위 내에 있을 때
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
        if (dropInfo == null || !dropInfo.isMagnetable) return;
        isPulledByMagnet = true;
        currentMagnetSpeed = magnetForce;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && GameManager.Instance?.CombatController != null)
        {
            GameManager.Instance.CombatController.ApplyItemEffect(itemType);
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
            PullToPlayer(magnetSpeed * 2f);  // 자동 자석 효과는 더 강하게
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
        GameManager.Instance?.CombatController?.UnregisterCollectible(this);
    }
}