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
        if (isInitialized) return;

        itemType = dropInfo.itemType;
        isMagnetable = dropInfo.isMagnetable;
        isCollected = false; // 초기화 시 수집 상태 리셋

        isInitialized = true;
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
        isCollected = false;
        isBeingMagneted = false;
        isPulledByMagnet = false;
        currentMagnetSpeed = magnetSpeed;
        currentMagnetDistance = basemagnetDistance;

        if (rb != null)
        {
            rb.simulated = true;  // 물리 시뮬레이션 다시 활성화
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 콜라이더 재활성화
        var colliders = GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
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
        // 이미 수집되었거나 필요한 컴포넌트가 없으면 무시
        if (isCollected) return;
        if (!other.CompareTag("Player") || combatController == null) return;

        // 즉시 수집 상태로 변경
        isCollected = true;

        // 현재 상태 저장
        var itemTypeToApply = itemType;
        var goldAmountToApply = goldAmount;

        // 물리 효과는 유지하되 속도만 0으로 설정
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Magnet 아이템이 아닌 경우에만 콜라이더 비활성화
        if (itemType != ItemType.Magnet)  // ItemType.Magnet은 실제 enum 값에 맞게 수정
        {
            var colliders = GetComponents<Collider2D>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }

        if(itemType == ItemType.Gold)
        {
            SoundManager.Instance.PlaySound("Coin_sfx", 1f, false);
        }
        if (itemType== ItemType.ExperienceLarge || itemType == ItemType.ExperienceMedium || itemType == ItemType.ExperienceSmall)
        {
            SoundManager.Instance.PlaySound("Exp_sfx", 1f, false);
        }

        // UnregisterCollectible 호출
        combatController.UnregisterCollectible(this);

        // 오브젝트 풀에 반환
        ObjectPool.Instance.ReturnToPool(itemTypeToApply.ToString(), gameObject);

        // 마지막으로 효과 적용
        combatController.ApplyItemEffect(itemTypeToApply, goldAmountToApply);
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
        if (isCollected) return; // 이미 수집된 아이템은 처리하지 않음

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