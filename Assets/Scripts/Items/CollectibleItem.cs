using UnityEngine;

public class CollectibleItem : MonoBehaviour, IPooledObject
{
    [Header("Movement Settings")]
    [SerializeField] private float magnetDistance = 5f;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private ItemType itemType;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private CombatController combatController;
    private bool isBeingMagneted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 3f;
        }
    }

    private void Start()
    {
        combatController = FindFirstObjectByType<CombatController>();
        if (combatController == null)
        {
            Debug.LogError("CombatController not found!");
        }
    }

    public void OnObjectSpawn()
    {
        isBeingMagneted = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= magnetDistance)
        {
            isBeingMagneted = true;
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * magnetSpeed;
        }
        else if (isBeingMagneted)
        {
            isBeingMagneted = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && combatController != null)
        {
            combatController.ApplyItemEffect(itemType);
            rb.linearVelocity = Vector2.zero;
            ObjectPool.Instance.ReturnToPool(itemType.ToString(), gameObject);
        }
    }
}
