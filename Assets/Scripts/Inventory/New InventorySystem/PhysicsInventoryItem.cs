using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// �κ��丮 �����ۿ� ������ Ư���� �߰��ϴ� ������Ʈ
/// �׸���� ������ ȯ�� ������ ��ȯ�� �����մϴ�.
/// ������Ʈ Ǯ���� �����մϴ�.
/// </summary>
public class PhysicsInventoryItem : MonoBehaviour, IPooledObject
{
    #region Serialized Fields
    [Header("Physics Settings")]
    [SerializeField] private float gravityScale = 980f;
    [SerializeField] private float dragDamping = 0.92f;
    [SerializeField] private float bounceMultiplier = 0.4f;
    [SerializeField] private float groundFriction = 0.8f;
    [SerializeField] private float minimumVelocity = 10f;
    [SerializeField] private Vector2 initialImpulse = new Vector2(0f, 150f);
    [SerializeField] private float visualFeedbackDuration = 0.1f;

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image itemImage;
    public InventoryItem inventoryItem;
    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    #endregion

    #region Private Fields    
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 velocity;
    private bool isPhysicsActive = false;
    private bool isBeingDragged = false;
    private Vector2 screenBounds;
    private Vector2 lastTouchPosition;
    private float itemLiftOffset = 350f; // InventoryController�� ������ ��
    private Vector2Int originalGridPosition;
    private bool isInitialized = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 spawnPosition;
    private bool isProtected = false;
    private bool isPaused = false;
    private Color originalColor;
    private float protectionEndTime = 0f;
    private float lastMoveTime = 0f;
    private bool isSleeping = false;
    #endregion

    #region Properties
    public bool IsPhysicsActive => isPhysicsActive;
    public bool IsBeingDragged => isBeingDragged;
    public int ActivationFrame { get; private set; }
    public float ActivationTime { get; private set; }
    public bool IsRecentlyActivated => Time.frameCount - ActivationFrame < 30;
    public float FloorY { get; set; } = 0f;

    public void SetProtected(bool state, float duration = 0f)
    {
        isProtected = state;

        if (duration > 0f)
        {
            // �ð� ��� ��ȣ ����
            protectionEndTime = Time.time + duration;
            Debug.Log($"Item {gameObject.name} protected for {duration} seconds (until {protectionEndTime})");
        }
    }
    public bool IsProtected
    {
        get
        {
            return isProtected || Time.time < protectionEndTime;
        }
    }

    public void PausePhysics(bool pause)
    {
        isPaused = pause;
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (isPhysicsActive && !isBeingDragged && !isPaused)
        {
            // Use unscaledDeltaTime instead of deltaTime for timeScale independence
            UpdatePhysics(Time.unscaledDeltaTime);
        }
    }

    private void OnDisable()
    {
        // Ǯ�� ��ȯ�� �� ���� �ʱ�ȭ
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// ������Ʈ ���� �ʱ�ȭ
    /// </summary>
    public void ForceInitialize()
    {
        // Components
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
        inventoryItem = GetComponent<InventoryItem>();

        // ���� ����
        if (itemImage != null)
        {
            originalColor = itemImage.color;
        }

        // Find canvas in multiple ways
        if (parentCanvas == null)
        {
            // Try parent first
            parentCanvas = GetComponentInParent<Canvas>();

            // If not found in hierarchy, find in scene
            if (parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        break;
                    }
                }

                // As a last resort, take any canvas
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                }
            }

            Debug.Log($"Found canvas: {(parentCanvas ? parentCanvas.name : "NONE")}");
        }

        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }

        // Original state
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }

        isInitialized = true;
    }

    /// <summary>
    /// IPooledObject �������̽��� OnObjectSpawn ����
    /// ������Ʈ Ǯ���� ������ �� ȣ���
    /// </summary>
    public void OnObjectSpawn()
    {
        // ���� �ʱ�ȭ
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;

        // �ʱ�ȭ Ȯ��
        if (!isInitialized)
        {
            Initialize();
        }

        // ���� ���� ����
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
        }

        // �÷� ����
        if (itemImage != null)
        {
            itemImage.color = originalColor;
        }

        // �κ��丮 ������ ������Ʈ ��������
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        try
        {
            // Components
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (itemImage == null) itemImage = GetComponent<Image>();
            if (inventoryItem == null) inventoryItem = GetComponent<InventoryItem>();
            if (inventoryItem == null) inventoryItem = GetComponent<InventoryItem>();
            // ���� ����
            if (itemImage != null)
            {
                originalColor = itemImage.color;
            }

            // Canvas reference
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
                // ... ���� �ڵ� ...
            }

            if (parentCanvas != null)
            {
                canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
                UpdateScreenBounds();
            }
            else
            {
                Debug.LogWarning("No canvas found for physics item!");
            }

            // Grid�� Scale Ȯ�� �� ����
            ItemGrid grid = FindAnyObjectByType<ItemGrid>();
            if (grid != null)
            {
                RectTransform gridRectTransform = grid.GetComponent<RectTransform>();
                if (gridRectTransform != null)
                {
                    // Grid�� Scale ���� ����
                    originalScale = gridRectTransform.localScale;
                    Debug.Log($"Saved grid scale: {originalScale}");
                }
            }
            else if (rectTransform != null && rectTransform.localScale != Vector3.zero)
            {
                // Grid�� ã�� ���ߴٸ� ���� ������ ����
                originalScale = rectTransform.localScale;
            }
            else
            {
                // �⺻������ ����
                originalScale = new Vector3(6, 6, 1); // Grid�� Scale�� 6�� ���
            }

            // ���� ȸ���� ����
            originalRotation = rectTransform.localRotation;

            if (inventoryItem != null)
            {
                originalGridPosition = inventoryItem.GridPosition;
            }

            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing PhysicsInventoryItem: {e.Message}");
        }
    }
    #endregion

    #region Physics Methods
    /// <summary>
    /// ���� �ý��� Ȱ��ȭ �� �ʱ� �ӵ� ����
    /// </summary>
    public void ActivatePhysics(Vector2? initialVelocity = null, Vector2? position = null)
    {
        if (!isInitialized) Initialize();

        try
        {
            // ������Ʈ Ȱ��ȭ ���� ����
            gameObject.SetActive(true);

            // ĵ���� ���� Ȯ�� �� �ʱ�ȭ
            EnsureCanvasReference();

            // �ʱ� ���� ����
            isPhysicsActive = true;
            velocity = initialVelocity ?? initialImpulse;
            isSleeping = false;
            lastMoveTime = Time.time;
            UpdateScreenBounds();

            // ��ġ�� ������ ��� ���������� ����
            if (position.HasValue && rectTransform != null)
            {
                Vector2 safePosisiton = EnsurePositionWithinScreen(position.Value);
                rectTransform.position = new Vector3(safePosisiton.x, safePosisiton.y, rectTransform.position.z);
            }

            // Grid �ۿ� ���� ���� Scale�� 6,6,1�� ����
            rectTransform.localScale = new Vector3(6, 6, 1);

            // InventoryItem의 현재 회전 상태 유지
            if (inventoryItem != null)
            {
                rectTransform.localRotation = Quaternion.Euler(0, 0, inventoryItem.IsRotated ? 90f : 0f);
            }

            // ������ �̹��� ������Ʈ ���� Ȯ��
            EnsureItemVisibility();

            // Ȱ��ȭ �ð� ���
            ActivationFrame = Time.frameCount;
            ActivationTime = Time.time;

            // �ڵ� ��ȣ ���� - 10�� ���� ��ȣ
            SetProtected(false, 10f);

            Debug.Log($"Physics activated on {gameObject.name}, frame: {ActivationFrame}, position: {rectTransform.position}, scale: {rectTransform.localScale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ActivatePhysics: {e.Message}");
        }
    }
    // ��ġ�� ȭ�� �ȿ� �ִ��� Ȯ���ϰ� �ʿ�� �����ϴ� �޼���
    private Vector2 EnsurePositionWithinScreen(Vector2 position)
    {
        if (canvasRectTransform == null)
        {
            // ĵ���� ������ ������ ���� �ʱ�ȭ �õ�
            EnsureCanvasReference();

            // ������ ������ ���� ��ġ ��ȯ
            if (canvasRectTransform == null) return position;
        }

        // ĵ���� ��ǥ�迡�� ȭ�� ��� ���
        Vector3[] corners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(corners);

        // ���� �Ʒ�(0)�� ������ ��(2) �ڳ� ���
        float minX = corners[0].x + rectTransform.sizeDelta.x * 0.5f;
        float maxX = corners[2].x - rectTransform.sizeDelta.x * 0.5f;
        float minY = corners[0].y + rectTransform.sizeDelta.y * 0.5f;
        float maxY = corners[2].y - rectTransform.sizeDelta.y * 0.5f;

        // ���� ���� �߰�
        float safeMargin = 10f;
        minX += safeMargin;
        maxX -= safeMargin;
        minY += safeMargin;
        maxY -= safeMargin;

        // ��ġ ����
        float safeX = Mathf.Clamp(position.x, minX, maxX);
        float safeY = Mathf.Clamp(position.y, minY, maxY);

        return new Vector2(safeX, safeY);
    }
    private void EnsureCanvasReference()
    {
        if (parentCanvas != null) return;

        // ĵ���� �˻� ����
        Transform current = transform;
        while (current != null)
        {
            Canvas canvas = current.GetComponent<Canvas>();
            if (canvas != null)
            {
                parentCanvas = canvas;
                Debug.Log($"Found parent canvas in hierarchy: {canvas.name}");
                break;
            }
            current = current.parent;
        }

        // �������� ã�� ���ߴٸ� ������ �˻�
        if (parentCanvas == null)
        {
            // ���� UI ���� �������� ã�� �õ�
            GameObject combatUI = GameObject.Find("CombatUI");
            Transform canvasTransform = null;

            if (combatUI != null)
            {
                // CombatUI/Canvas ��� ã��
                canvasTransform = combatUI.transform.Find("Canvas");
                if (canvasTransform != null)
                {
                    Canvas canvas = canvasTransform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found canvas in CombatUI: {canvas.name}");
                    }
                }
            }

            // �� ã������ ��� ĵ���� �˻�
            if (parentCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

                // �������� ����� ĵ���� �켱 ã��
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found ScreenSpaceOverlay canvas: {canvas.name}");
                        break;
                    }
                }

                // �� ã������ ù ��° ĵ���� ���
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                    Debug.Log($"Using first available canvas: {parentCanvas.name}");
                }
            }
        }

        // ĵ���� ã������ rectTransform ����
        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }
    }
    private void EnsureItemVisibility()
    {
        // �̹��� ������Ʈ Ȯ��
        if (itemImage != null)
        {
            // ��Ȱ��ȭ�� ��� Ȱ��ȭ
            if (!itemImage.enabled)
            {
                itemImage.enabled = true;
                Debug.Log("Enabled item image component");
            }

            // ������ Ȯ��
            if (itemImage.color.a < 0.5f)
            {
                Color color = itemImage.color;
                color.a = 1.0f;
                itemImage.color = color;
                Debug.Log("Restored item image opacity");
            }
        }

        // ĵ���� �׷쵵 Ȯ��
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null && group.alpha < 0.5f)
        {
            group.alpha = 1.0f;
            Debug.Log("Restored canvas group alpha");
        }
    }

    /// <summary>
    /// ���� �ùķ��̼��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivatePhysics()
    {
        isPhysicsActive = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// ���� ���¸� �� ������ ������Ʈ�մϴ�.
    /// </summary>
    private void UpdatePhysics(float deltaTime)
    {
        try
        {
            // �ش����� ��ġ �˻� �� ����
            ResetPositionIfExtreme();

            // �̹��� ǥ�� ���� ������ üũ (��� �����Ӹ��� �� �ʿ� ����)
            if (Time.frameCount % 120 == 0)
            {
                EnsureItemVisibility();
            }

            // 'Sleep' ���� üũ - �ӵ��� �ſ� ���� ���� �ð� ���� ū ��ȭ�� ������
            if (!isSleeping && velocity.sqrMagnitude < minimumVelocity * 0.5f)
            {
                if (Time.time - lastMoveTime > 1.0f)
                {
                    isSleeping = true;
                    Debug.Log($"Physics item {gameObject.name} entered sleep state");
                }
            }

            // 'Sleep' ���¸� ���� ��� �ּ�ȭ
            if (isSleeping)
            {
                // Sleep ���¿����� �浹 �˻縸 ���� ����
                if (Time.frameCount % 30 == 0)
                {
                    CheckBoundaryCollisions();
                }
                return;
            }

            // Apply gravity
            velocity += Vector2.down * gravityScale * deltaTime;

            // �ӵ� �Ѱ� ���� (�ʹ� ������ �ʵ���)
            float maxSpeed = 2000f;
            float currentSpeed = velocity.magnitude;
            if (currentSpeed > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }

            // ���� ��ġ ���� (������ ������)
            Vector3 oldPosition = rectTransform.position;

            // Update position
            rectTransform.position += (Vector3)velocity * deltaTime;

            // ������ ����
            float movement = Vector3.Distance(oldPosition, rectTransform.position);
            if (movement > 0.5f)
            {
                lastMoveTime = Time.time;
                if (isSleeping)
                {
                    isSleeping = false;
                    Debug.Log($"Physics item {gameObject.name} woke from sleep state");
                }
            }

            // Check boundary collisions
            CheckBoundaryCollisions();

            // �ش����� ��ġ �ٽ� �˻� (�浹 ó�� ��)
            ResetPositionIfExtreme();

            // ���� ���� ���� (����)
            velocity *= dragDamping;

            // Stop if velocity is too low
            if (velocity.sqrMagnitude < minimumVelocity * minimumVelocity)
            {
                velocity = Vector2.zero;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdatePhysics: {e.Message}");
            velocity = Vector2.zero;
        }
    }
    private void ResetPositionIfExtreme()
    {
        if (rectTransform == null || !isPhysicsActive) return;

        // ���� ��ġ ��������
        Vector3 currentPos = rectTransform.position;

        // �ش����� �� üũ (��100,000 �̻�)
        bool isExtremePosition = Mathf.Abs(currentPos.x) > 100000f ||
                                 Mathf.Abs(currentPos.y) > 100000f ||
                                 Mathf.Abs(currentPos.z) > 100000f;

        // NaN üũ
        bool isNaN = float.IsNaN(currentPos.x) ||
                    float.IsNaN(currentPos.y) ||
                    float.IsNaN(currentPos.z);

        if (isExtremePosition || isNaN)
        {
            Debug.LogWarning($"Extreme position detected: {currentPos}. Resetting position.");

            // ĵ������ ������ �߾����� ����, ������ ��������
            if (canvasRectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                canvasRectTransform.GetWorldCorners(corners);

                // ĵ���� �߾� ���
                Vector3 center = new Vector3(
                    (corners[0].x + corners[2].x) * 0.5f,
                    (corners[0].y + corners[2].y) * 0.5f,
                    currentPos.z
                );

                rectTransform.position = center;
            }
            else
            {
                rectTransform.position = new Vector3(Screen.width / 2, Screen.height / 2, currentPos.z);
            }

            // �ӵ� �ʱ�ȭ
            velocity = Vector2.zero;

            // Sleep ���� ����
            isSleeping = false;
            lastMoveTime = Time.time;
        }
    }

    /// <summary>
    /// ȭ�� ������ �浹�� üũ�ϰ� �����մϴ�.
    /// </summary>
    private void CheckBoundaryCollisions()
    {
        // ĵ������ ���ų� RectTransform�� ������ �浹 ó�� �Ұ�
        if (canvasRectTransform == null || rectTransform == null) return;

        try
        {
            // ĵ���� ��ǥ�迡�� ȭ�� ��� ���
            Vector3[] corners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(corners);

            // ��� ��
            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[2].y;
            
            // ������ �ٴ� ��ġ ��� (Manager���� ���޹��� ��)
            if (FloorY > 0)
            {
                minY = FloorY; // ��ư ���� �ٴ� ����
            }

            // RectTransform�� ũ�� ���� (�浹 ������)
            Vector2 halfSize = rectTransform.sizeDelta * rectTransform.localScale / 2f;
            Vector3 pos = rectTransform.position;

            // ��� �浹 �� �ٿ ����
            bool collided = false;

            // X�� �浹 ó��
            if (pos.x + halfSize.x > maxX)
            {
                pos.x = maxX - halfSize.x;
                velocity.x = -velocity.x * bounceMultiplier;
                collided = true;

                // �ӵ��� �ſ� ������ �ε巴�� ����
                if (Mathf.Abs(velocity.x) < 50f)
                {
                    velocity.x *= 0.5f;
                }
            }
            else if (pos.x - halfSize.x < minX)
            {
                pos.x = minX + halfSize.x;
                velocity.x = -velocity.x * bounceMultiplier;
                collided = true;

                if (Mathf.Abs(velocity.x) < 50f)
                {
                    velocity.x *= 0.5f;
                }
            }

            // Y�� �浹 ó��
            if (pos.y + halfSize.y > maxY)
            {
                pos.y = maxY - halfSize.y;
                velocity.y = -velocity.y * bounceMultiplier;
                collided = true;

                if (Mathf.Abs(velocity.y) < 50f)
                {
                    velocity.y *= 0.5f;
                }
            }
            else if (pos.y - halfSize.y < minY)
            {
                pos.y = minY + halfSize.y;
                velocity.y = -velocity.y * bounceMultiplier;
                velocity.x *= groundFriction; // �ٴڿ� ����� �� X�� ���� ����
                collided = true;

                // �ٴڿ� ������ X�� �ӵ��� �� ���� ���ҽ�Ŵ (���� �����ϵ���)
                if (Mathf.Abs(velocity.y) < 50f)
                {
                    velocity.y *= 0.3f;
                }

                // �ٴڿ� ���� �� ���� Ȯ���� ȿ���� ���
                if (velocity.magnitude > 300f)
                {
                    float volume = Mathf.Clamp01(velocity.magnitude / 1000f) * 0.3f;
                    SoundManager.Instance?.PlaySound("ItemBounce_sfx", volume, false);
                }
            }

            // �浹�� �־��ٸ� ��ġ ������Ʈ
            if (collided)
            {
                rectTransform.position = pos;
                lastMoveTime = Time.time; // �浹�� '������'���� ����
                isSleeping = false; // �浹�� ������ Sleep ���� ����
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CheckBoundaryCollisions: {e.Message}");
        }
    }
    /// <summary>
    /// ȭ�� ��� ������ ������Ʈ�մϴ�.
    /// </summary>
    private void UpdateScreenBounds()
    {
        if (canvasRectTransform != null)
        {
            // ĵ������ ��� ���
            Vector3[] corners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(corners);

            // ���ϴܰ� ���� �ڳ� �������� ��� ���
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];

            // �߾� ���� ȭ�� ũ���� ���� ��
            screenBounds = new Vector2(
                (topRight.x - bottomLeft.x) * 0.5f,
                (topRight.y - bottomLeft.y) * 0.5f
            );
        }
    }
    #endregion

    #region Interaction Methods
    /// <summary>
    /// ������ �巡�� ����
    /// </summary>
    public void StartDrag(Vector2 touchPosition)
    {
        if (!isInitialized) Initialize();

        try
        {
            isBeingDragged = true;
            lastTouchPosition = touchPosition;

            // ���� �������� �巡���� ���� Scale�� 6,6,1�� ����
            rectTransform.localScale = new Vector3(6, 6, 1);

            // InventoryItem의 현재 회전 상태 유지
            if (inventoryItem != null)
            {
                rectTransform.localRotation = Quaternion.Euler(0, 0, inventoryItem.IsRotated ? 90f : 0f);
            }
            else
            {
                rectTransform.rotation = originalRotation;
            }

            // ���� ȿ�� ��Ȱ��ȭ
            DeactivatePhysics();

            // ��ġ ��ġ + ���������� �̵� (���� ��¦ ���)
            Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
            rectTransform.position = liftedPosition;
            Debug.Log($"Started dragging physics item: {gameObject.name}, scale: {rectTransform.localScale}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in StartDrag: {e.Message}");
        }
    }
    /// <summary>
    /// ������ �巡�� �� ��ġ ������Ʈ
    /// </summary>
    public void UpdateDragPosition(Vector2 touchPosition)
    {
        if (!isBeingDragged) return;

        try
        {
            lastTouchPosition = touchPosition;
            Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
            rectTransform.position = liftedPosition;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateDragPosition: {e.Message}");
        }
    }

    /// <summary>
    /// ������ �巡�� ����
    /// </summary>
    public void EndDrag(ItemGrid targetGrid, Vector2 finalPosition)
    {
        if (!isBeingDragged) return;

        try
        {
            isBeingDragged = false;

            // �׸��� ��ġ �õ�
            if (targetGrid != null)
            {
                // �׸��� ��ġ Ȯ��
                Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);
                Debug.Log($"Grid position: {gridPosition}, can place: {targetGrid.IsValidPosition(gridPosition) && targetGrid.CanPlaceItem(inventoryItem, gridPosition)}");

                // ��ȿ�� �׸��� ��ġ�̰� ��ġ �����ϸ� �׸��忡 ��ġ
                if (targetGrid.IsValidPosition(gridPosition) &&
                    targetGrid.CanPlaceItem(inventoryItem, gridPosition))
                {
                    // �׸��忡 ��ġ�ϱ� ���� Scale�� 1,1,1�� ����
                    rectTransform.localScale = Vector3.one;

                    // �׸��忡 ��ġ
                    rectTransform.SetParent(targetGrid.transform, false);
                    targetGrid.PlaceItem(inventoryItem, gridPosition);
                    DeactivatePhysics();
                    return;
                }
            }

            // ��ȿ���� ���� ��ġ�� ���� Ȱ��ȭ (Scale�� ActivatePhysics���� ó��)
            Vector2 dragVelocity = (finalPosition - lastTouchPosition) * 5f;
            ActivatePhysics(dragVelocity);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in EndDrag: {e.Message}");
            // ���� �߻� �� ���� ȿ�� Ȱ��ȭ
            ActivatePhysics();
        }
    }
    /// <summary>
    /// ������ ȸ��
    /// </summary>
    public void Rotate()
    {
        if (inventoryItem != null)
        {
            inventoryItem.Rotate();
        }
    }

    /// <summary>
    /// ���� ��ġ ����
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        rectTransform.position = position;
    }
    public WeaponData GetWeaponData()
    {
        return inventoryItem?.GetWeaponData();
    }
    /// <summary>
    /// ������Ʈ Ǯ�� ������ ��ȯ
    /// </summary>
    public void ReturnToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        // ���� ��Ȱ��ȭ
        DeactivatePhysics();

        // Ǯ�� ��ȯ
        ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
    }
}


    #endregion