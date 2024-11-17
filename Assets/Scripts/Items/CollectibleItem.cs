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
    private DropInfo dropInfo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 3f;
        }
        currentMagnetSpeed = magnetSpeed;
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
    }

    public void OnObjectSpawn()
    {
        isBeingMagneted = false;
        isPulledByMagnet = false;
        currentMagnetSpeed = magnetSpeed;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        GameManager.Instance?.CombatController?.RegisterCollectible(this);
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

    private void FixedUpdate()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // dropInfo가 없거나 자석 효과 대상이 아니면 무시
        if (dropInfo == null || !dropInfo.isMagnetable)
        {
            return;
        }

        if (isPulledByMagnet ||
            Vector2.Distance(transform.position, playerTransform.position) <= basemagnetDistance)
        {
            isBeingMagneted = true;
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * currentMagnetSpeed;
        }
        else if (isBeingMagneted && !isPulledByMagnet)
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

    private void OnDestroy()
    {
        GameManager.Instance?.CombatController?.UnregisterCollectible(this);
    }
}