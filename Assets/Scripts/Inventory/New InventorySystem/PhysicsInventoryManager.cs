using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// ���� ��� �������� �����ϴ� �Ŵ��� Ŭ���� - ������Ʈ Ǯ Ȱ�� ����
/// </summary>
public class PhysicsInventoryManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private ItemGrid mainGrid;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private InventoryHighlight inventoryHighlight;
    [SerializeField] private WeaponInfoUI weaponInfoUI;
    [Header("Physics Settings")]
    [SerializeField] private float dragThreshold = 0.3f;
    [SerializeField] private float holdDelay = 0.3f;
    [SerializeField] private float physicsSpawnOffset = 50f;
    [SerializeField] private float physicsRandomVariance = 30f;
    [Header("Floor Boundary Settings")]
    [SerializeField] private RectTransform shopButtonRect;
    [SerializeField] private RectTransform progressButtonRect;
    [SerializeField] private float floorOffsetFromButtons = 10f;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int ensurePoolSize = 10; // �ּ��� ������ Ǯ ũ��
    #endregion

    #region Private Fields
    private List<PhysicsInventoryItem> physicsItems = new List<PhysicsInventoryItem>();
    private PhysicsInventoryItem selectedPhysicsItem;
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private bool isHolding = false;
    private bool isDragging = false;
    private Vector2 touchStartPosition;
    private Coroutine holdCoroutine;
    private Camera mainCamera;
    private bool isInitialized = false;
    private bool enableDebugLogs = true; // ���� �� false�� ���� ����
    private int updateCounter = 0;
    private float lastPerformanceCheck = 0f;
    private bool hasPerformanceWarning = false;
    private RectTransform canvasRectTransform;
    private float cachedFloorY = 0f;
    private bool isFloorPositionCached = false;
    private int lastFloorUpdateFrame = -1;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Debug.Log("PhysicsInventoryManager Awake called");

        InitializeComponents();
        InitializeCanvasReference();
        InitializePhysicsItemPool();
        CacheFloorPosition();

        if (enableDebugLogs)
        {
            DebugObjectPoolStatus();
            LogCanvasHierarchy();
        }
    }
    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private void OnEnable()
    {
        if (touchActions != null)
        {
            touchActions.Enable();

        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (touchActions != null)
        {
            touchActions.Disable();
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        // ���� ���� �ڷ�ƾ ����
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // When paused, make sure physics items stay visible
        if (newState == GameState.Paused)
        {
            // Suspend physics updates but keep items visible
            foreach (var item in physicsItems)
            {
                if (item != null)
                {
                    // Just disable physics calculations but keep item visible
                    item.PausePhysics(true);
                }
            }
        }
        else if (newState == GameState.Playing)
        {
            // Resume physics when game resumes
            foreach (var item in physicsItems)
            {
                if (item != null)
                {
                    item.PausePhysics(false);
                }
            }
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            InitializeComponents();
            SetupInputSystem();
            isInitialized = true;
            return;
        }

        // ���� ������ ���� ��� ���� ����
        if (Time.time - lastPerformanceCheck > 5f) // 5�ʸ��� ���� üũ
        {
            lastPerformanceCheck = Time.time;
            MonitorPerformance();
        }

        // ���� ������ ���� ���� Ȯ�� (�̹����� ������ ��� üũ)
        // �� �˻�� ������ ����� �����Ƿ� ���������θ� ����
        updateCounter++;
        if (updateCounter % 120 == 0) // �� 4�ʸ��� �� ���� (30fps ����)
        {
            EnsurePhysicsItemsVisible();
            updateCounter = 0;
        }

        // ���õ� �������� �ְ� �巡�� ���̸� ��ġ ������Ʈ
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 currentTouchPos = touchPosition.ReadValue<Vector2>();
            selectedPhysicsItem.UpdateDragPosition(currentTouchPos);

            // Update highlighter position
            if (inventoryHighlight != null && mainGrid != null)
            {
                InventoryItem inventoryItem = selectedPhysicsItem.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    Vector2Int gridPosition = mainGrid.GetGridPosition(currentTouchPos);
                    bool canPlace = mainGrid.IsValidPosition(gridPosition) &&
                                   mainGrid.CanPlaceItem(inventoryItem, gridPosition);

                    inventoryHighlight.Show(canPlace);

                    if (canPlace)
                    {
                        inventoryHighlight.SetPosition(mainGrid, inventoryItem, gridPosition.x, gridPosition.y);
                    }
                }
            }
        }

        // ���� ����ȭ: Ȱ�� ���� ������ ������Ʈ
        // ���� ������ ������ �������� �˻� �� ����
        int checkInterval = DetermineUpdateInterval();
        if (Time.frameCount % checkInterval == 0)
        {
            UpdateActivePhysicsItems();
        }

        // 60�����Ӹ��� (�� 1�ʿ� �� ��) �ٴ� ��ġ ������Ʈ
        if (Time.frameCount % 60 == 0 && physicsItems.Count > 0)
        {
            CacheFloorPosition();

            // ĳ�̵� �� ���
            foreach (var item in physicsItems)
            {
                if (item != null)
                {
                    item.FloorY = cachedFloorY;
                }
            }
        }
    }

    // �ٴ� ��ġ�� ĳ�� (�� ���� ����ϰ� ����)
    private void CacheFloorPosition()
    {
        if (isFloorPositionCached && Time.frameCount - lastFloorUpdateFrame < 60) return;

        float buttonTopY = 0f;

        // �̸� Inspector���� �Ҵ�� ���� ���
        if (shopButtonRect != null)
        {
            Vector3[] corners = new Vector3[4];
            shopButtonRect.GetWorldCorners(corners);
            buttonTopY = Mathf.Max(buttonTopY, corners[1].y);
        }

        if (progressButtonRect != null)
        {
            Vector3[] corners = new Vector3[4];
            progressButtonRect.GetWorldCorners(corners);
            buttonTopY = Mathf.Max(buttonTopY, corners[1].y);
        }

        // ������ ���� ��� fallback ������� ȭ�� �ϴ� 20% ��ġ ���
        if (buttonTopY <= 0f && parentCanvas != null)
        {
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                Vector3[] canvasCorners = new Vector3[4];
                canvasRect.GetWorldCorners(canvasCorners);
                float canvasBottom = canvasCorners[0].y;
                float canvasHeight = canvasCorners[1].y - canvasBottom;

                buttonTopY = canvasBottom + (canvasHeight * 0.2f);
            }
        }

        cachedFloorY = buttonTopY + floorOffsetFromButtons;
        isFloorPositionCached = true;
        lastFloorUpdateFrame = Time.frameCount;

        // ����� �α� (���� �߿��� ���)
        if (enableDebugLogs) Debug.Log($"Floor Y position cached: {cachedFloorY}");
    }
    private void MonitorPerformance()
    {
        int itemCount = physicsItems.Count;

        if (enableDebugLogs)
        {
            Debug.Log($"Active physics items: {itemCount}");
        }

        // ������ ���� �ʹ� ������ ��� �� ����ȭ ��ġ
        if (itemCount > 30 && !hasPerformanceWarning)
        {
            Debug.LogWarning($"High number of physics items ({itemCount}) may impact performance");
            hasPerformanceWarning = true;

            // ������ ������ ���� �Ϻ� ���� (������)
            if (itemCount > 50) // �������� 50�� �̻��̸� ���� ������ �ͺ��� ����
            {
                int itemsToRemove = itemCount - 40; // 40�������� ����
                RemoveOldestItems(itemsToRemove);
            }
        }
        else if (itemCount < 20 && hasPerformanceWarning)
        {
            hasPerformanceWarning = false;
        }
    }

    private int DetermineUpdateInterval()
    {
        int itemCount = physicsItems.Count;

        if (itemCount < 10) return 10;      // 10�� �̸�: �� 10�����Ӹ���
        else if (itemCount < 20) return 20; // 10-20��: �� 20�����Ӹ���
        else if (itemCount < 30) return 30; // 20-30��: �� 30�����Ӹ���
        else return 60;                     // 30�� �̻�: �� 60�����Ӹ���
    }
    private void RemoveOldestItems(int count)
    {
        if (physicsItems.Count <= count) return;

        // Ȱ��ȭ �ð� ���� ����
        var sortedItems = physicsItems
            .Where(item => item != null && !item.IsProtected && !item.IsBeingDragged)
            .OrderBy(item => item.ActivationTime)
            .Take(count)
            .ToList();

        foreach (var item in sortedItems)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Removing old physics item: {item.name}, age: {Time.time - item.ActivationTime}s");
            }
            RemovePhysicsItem(item);
        }
    }

    private void EnsurePhysicsItemsVisible()
    {
        foreach (var item in physicsItems)
        {
            if (item == null) continue;

            // ���� ������Ʈ Ȱ��ȭ ���� Ȯ��
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                if (enableDebugLogs) Debug.Log($"Re-activated game object: {item.name}");
            }

            // �̹��� ������Ʈ�� ��Ȱ��ȭ�� ��� �ٽ� Ȱ��ȭ
            Image image = item.GetComponent<Image>();
            if (image != null && !image.enabled)
            {
                image.enabled = true;
                if (enableDebugLogs) Debug.Log($"Restored visibility for physics item: {item.name}");
            }

            // ���������� ���� ��� ����
            if (image != null && image.color.a < 0.5f)
            {
                Color color = image.color;
                color.a = 1.0f;
                image.color = color;
                if (enableDebugLogs) Debug.Log($"Restored opacity for physics item: {item.name}");
            }

            // Canvas Group�� ��Ȱ��ȭ�� ���
            CanvasGroup group = item.GetComponent<CanvasGroup>();
            if (group != null && group.alpha < 0.5f)
            {
                group.alpha = 1.0f;
                if (enableDebugLogs) Debug.Log($"Restored canvas group alpha for physics item: {item.name}");
            }
        }
    }
    private void InitializeCanvasReference()
    {
        if (parentCanvas != null) return;

        Debug.Log("Initializing canvas reference...");

        // 1. ���� ���� ������ ���� ã��
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

        // 2. �������� ��� Ž��
        if (parentCanvas == null)
        {
            // CombatUI ���� ĵ���� ã��
            GameObject combatUI = GameObject.Find("CombatUI");
            if (combatUI != null)
            {
                // Canvas ���� ��ü ã��
                Transform canvasTransform = combatUI.transform.Find("Canvas");
                if (canvasTransform != null)
                {
                    Canvas canvas = canvasTransform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found canvas in CombatUI hierarchy: {canvas.name}");
                    }
                }

                // ���� ������Ʈ ����
                if (parentCanvas == null)
                {
                    Canvas canvas = combatUI.GetComponentInChildren<Canvas>();
                    if (canvas != null)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found canvas as child of CombatUI: {canvas.name}");
                    }
                }
            }
        }

        // 3. ������ ���� ã��
        if (parentCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length > 0)
            {
                // ù ��°�� ScreenSpaceOverlay ����� ĵ���� ã��
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found ScreenSpaceOverlay canvas: {canvas.name}");
                        break;
                    }
                }

                // �� ã�Ҵٸ� ù ��° ĵ���� ���
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                    Debug.Log($"Using first available canvas: {parentCanvas.name}");
                }
            }
        }

        // ã�� ĵ���� ����
        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("Failed to find any canvas! Physics items may not display correctly.");
        }
    }

    private void OnDestroy()
    {
        if (touchActions != null)
        {
            touchActions.Disable();
            touchActions.Dispose();
        }

        // Ǯ�� ������ ��ȯ
        ReturnAllItemsToPool();
    }
    #endregion

    #region Initialization
    private IEnumerator DelayedInitialization()
    {
        // ù �������� �ǳʶپ� �ٸ� �ʱ�ȭ�� �Ϸ�ǵ��� ��
        yield return null;

        // ������Ʈ Ǯ ���� �̸� �ʱ�ȭ
        if (useObjectPool && ObjectPool.Instance != null)
        {
            InitializePhysicsItemPool();
        }

        SetupInputSystem();
        isInitialized = true;
    }

    private void InitializeComponents()
    {
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (mainGrid == null && inventoryController != null)
        {
            mainGrid = inventoryController.GetComponentInChildren<ItemGrid>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (itemSpawnPoint == null)
        {
            // InventoryController���� spawnPoint ��������
            var field = typeof(InventoryController).GetField("itemSpawnPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                itemSpawnPoint = field.GetValue(inventoryController) as Transform;
            }

            if (itemSpawnPoint == null)
            {
                Debug.LogWarning("ItemSpawnPoint not found! Creating a default one.");
                GameObject spawnObj = new GameObject("DefaultItemSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.zero;
                itemSpawnPoint = spawnObj.transform;
            }
        }
        if (inventoryHighlight == null)
        {
            inventoryHighlight = FindAnyObjectByType<InventoryHighlight>();
        }
        if (weaponPrefab == null && inventoryController != null)
        {
            // InventoryController�� weaponPrefab �ʵ� ��������
            var field = typeof(InventoryController).GetField("weaponPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                weaponPrefab = field.GetValue(inventoryController) as GameObject;
            }
        }

        mainCamera = Camera.main;
    }

    /// <summary>
    /// ���� �κ��丮 �������� ���� ������Ʈ Ǯ �ʱ�ȭ
    /// </summary>
    private void InitializePhysicsItemPool()
    {
        Debug.Log("Initializing physics item pool...");

        // Ǯ �±� ���� - ���� �±׿� ��ġ�ϵ��� ��
        string poolTag = "PhysicsInventoryItem";

        // ������Ʈ Ǯ ���� Ȯ��
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool instance is missing! Cannot initialize physics item pool.");
            return;
        }

        // 1. Ǯ�� �̹� �����ϴ��� Ȯ��
        bool poolExists = ObjectPool.Instance.DoesPoolExist(poolTag);
        Debug.Log($"Pool '{poolTag}' exists: {poolExists}");

        // 2. ���� ������ �˻�
        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab reference is missing! Cannot initialize physics item pool.");
            return;
        }

        // 3. �ʿ��� ��� PhysicsInventoryItem ������Ʈ�� �ִ��� Ȯ��
        PhysicsInventoryItem testComponent = weaponPrefab.GetComponent<PhysicsInventoryItem>();
        if (testComponent == null)
        {
            Debug.LogWarning("PhysicsInventoryItem component not found on weapon prefab. It will be added at runtime.");
        }

        // 4. Ǯ ���� �Ǵ� Ȯ��
        if (!poolExists)
        {
            // �� Ǯ ����
            int initialPoolSize = 20;
            ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
            Debug.Log($"Created new physics item pool with size {initialPoolSize}");
        }
        else
        {
            // Ǯ ũ�� Ȯ�� �� �ʿ�� Ȯ��
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            int ensurePoolSize = 10;

            if (availableCount < ensurePoolSize)
            {
                int expandSize = ensurePoolSize - availableCount;
                ObjectPool.Instance.ExpandPool(poolTag, expandSize);
                Debug.Log($"Expanded physics item pool by {expandSize} items");
            }

            Debug.Log($"Using existing physics item pool. Available items: {availableCount}");
        }
    }

    private void SetupInputSystem()
    {
        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;

        touchActions.Enable();
    }
    #endregion

    #region Item Creation
    /// <summary>
    /// ���� ��� �κ��丮 ������ ���� - ������Ʈ Ǯ Ȱ��
    /// </summary>
    public InventoryItem CreatePhysicsItem(WeaponData weaponData)
    {
        if (weaponData == null || weaponPrefab == null) return null;

        // ������ ���� ��ġ ���
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnPosition += new Vector3(
            Random.Range(-physicsRandomVariance, physicsRandomVariance),
            physicsSpawnOffset + Random.Range(0, physicsRandomVariance),
            0
        );

        GameObject itemObj;

        // ������Ʈ Ǯ���� ��������
        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // ���� ������Ʈ�� ������� Ȯ��
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < 1)
            {
                // Ǯ Ȯ��
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize);
                Debug.Log($"Physics item pool expanded by {ensurePoolSize} items");
            }

            // Ǯ���� ������Ʈ ��������
            itemObj = ObjectPool.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);

            // �߿�: Ǯ���� ������ �� ��� ĵ������ �ڽ����� ����
            if (itemObj != null && parentCanvas != null)
            {
                itemObj.transform.SetParent(parentCanvas.transform, false);
            }
        }
        else
        {
            // ������Ʈ Ǯ�� ������� �ʰų� ����� �� ���� ��� ���� ����
            itemObj = Instantiate(weaponPrefab, spawnPosition, Quaternion.identity, parentCanvas.transform);
        }

        if (itemObj == null)
        {
            Debug.LogError("Failed to create physics inventory item");
            return null;
        }

        // �κ��丮 ������ ������Ʈ ��������
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            Debug.LogError("InventoryItem component not found on prefab");
            if (useObjectPool && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, itemObj);
            }
            else
            {
                Destroy(itemObj);
            }
            return null;
        }

        // ���� ������ �ʱ�ȭ
        inventoryItem.Initialize(weaponData);
        inventoryItem.SetGridPosition(new Vector2Int(-1, -1)); // �׸��� �ܺ� ǥ��

        // ���� ������Ʈ �߰� �Ǵ� ��������
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // ��ġ ���� �� ���� Ȱ��ȭ
        physicsItem.SetSpawnPosition(spawnPosition);
        physicsItem.ActivatePhysics();

        // ���� ��Ͽ� �߰�
        physicsItems.Add(physicsItem);

        return inventoryItem;
    }

    /// <summary>
    /// �κ��丮 �׸��忡 �� ������ ���� �� ���� ���������� ����
    /// </summary>
    public void HandleFullInventory(WeaponData weaponData)
    {
        if(inventoryController != null)
        {
            inventoryController.ToggleShopUI(false);
            inventoryController.ToggleInventoryUI(true);
        }

        CreatePhysicsItem(weaponData);

        // ȿ���� ��� (�ִ� ���)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemDrop_sfx", 0.8f, false);
        }
    }
    #endregion

    #region Touch Handling
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // ��ġ ��ġ ��������
        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        touchStartPosition = touchPos;

        // �׸��� �� ������ ���� üũ
        Vector2Int gridPosition = mainGrid?.GetGridPosition(touchPos) ?? new Vector2Int(-1, -1);
        if (mainGrid != null && mainGrid.IsValidPosition(gridPosition))
        {
            // �׸��� �� ������ ��ġ ó���� InventoryController�� ���
            Debug.Log("Touch is inside grid area, skipping physics item check");
            return;
        }

        // ���� ������ ��ġ üũ
        PhysicsInventoryItem touchedItem = GetPhysicsItemAtPosition(touchPos);
        if (touchedItem != null)
        {
            Debug.Log($"Found physics item to drag: {touchedItem.name}");

            // ���� ������ ����
            selectedPhysicsItem = touchedItem;

            // ���� ���� UI ������Ʈ 
            if (weaponInfoUI != null)
            {
                WeaponData weaponData = touchedItem.GetWeaponData();
                if (weaponData != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(weaponData);
                }
            }
            // Ȧ�� üũ ����
            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
            }
            holdCoroutine = StartCoroutine(CheckForHold(touchedItem, touchPos));
        }
    }

    public PhysicsInventoryItem GetDraggedPhysicsItem()
    {
        return isDragging ? selectedPhysicsItem : null;
    }

    public List<PhysicsInventoryItem> GetAllPhysicsItems()
    {
        return new List<PhysicsInventoryItem>(physicsItems);
    }
    private IEnumerator CheckForHold(PhysicsInventoryItem item, Vector2 startPosition)
    {
        if (item == null) yield break;

        float elapsedTime = 0f;
        float threshold = holdDelay;

        while (touchPress.IsPressed())
        {
            elapsedTime += Time.deltaTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float distance = Vector2.Distance(startPosition, currentPos);

            // Ȧ�� �ð��� �����ų�, ���� �Ÿ� �̻� �������� ���
            if (elapsedTime >= threshold || distance > dragThreshold)
            {
                Debug.Log($"Starting to drag: time={elapsedTime}, distance={distance}");
                StartDragging(item, currentPos);
                break;
            }

            yield return null;
        }

        holdCoroutine = null;
    }

    private void StartDragging(PhysicsInventoryItem item, Vector2 position)
    {
        if (item == null) return;

        isHolding = true;
        isDragging = true;
        selectedPhysicsItem = item;

        // Get the InventoryItem component
        InventoryItem inventoryItem = item.GetComponent<InventoryItem>();

        // Update the highlighter if we have one
        if (inventoryHighlight != null && inventoryItem != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(inventoryItem);

            // We need to set a reasonable position for the highlighter
            if (mainGrid != null)
            {
                Vector2Int gridPosition = mainGrid.GetGridPosition(position);
                if (mainGrid.IsValidPosition(gridPosition) &&
                    mainGrid.CanPlaceItem(inventoryItem, gridPosition))
                {
                    inventoryHighlight.SetPosition(mainGrid, inventoryItem, gridPosition.x, gridPosition.y);
                }
            }
        }

        // ���� ��Ȱ��ȭ�ϰ� �巡�� ����
        item.StartDrag(position);

        // ȿ���� ��� (SoundManager�� �ִ� ���)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemLift_sfx", 1f, false);
        }
    }
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 finalPosition = touchPosition.ReadValue<Vector2>();

            // �巡�� ���� ó��
            selectedPhysicsItem.EndDrag(mainGrid, finalPosition);

            
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }

            // ���� �ʱ�ȭ
            ResetDragState();
        }

        // Ȧ�� �ڷ�ƾ ����
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void ResetDragState()
    {
        isHolding = false;
        isDragging = false;
        selectedPhysicsItem = null;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Ư�� ��ġ�� �ִ� ���� ������ ã��
    /// </summary>
    private PhysicsInventoryItem GetPhysicsItemAtPosition(Vector2 screenPosition)
    {
        try
        {
            // ����ĳ��Ʈ ����
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // ��� ó�� - ���� ������ ���� ã��
            foreach (RaycastResult result in results)
            {
                // ������ ��ü���� ������Ʈ ã��
                PhysicsInventoryItem physicsItem = result.gameObject.GetComponent<PhysicsInventoryItem>();
                if (physicsItem != null)
                {
                    Debug.Log($"Found physics item: {physicsItem.name} at {screenPosition}");
                    return physicsItem;
                }

                // �θ� ��ü������ Ȯ��
                Transform parentTransform = result.gameObject.transform.parent;
                if (parentTransform != null)
                {
                    physicsItem = parentTransform.GetComponent<PhysicsInventoryItem>();
                    if (physicsItem != null)
                    {
                        Debug.Log($"Found physics item in parent: {physicsItem.name} at {screenPosition}");
                        return physicsItem;
                    }
                }

                // InventoryItem ã�� (PhysicsInventoryItem�� �����Ǿ� ���� �� ����)
                InventoryItem inventoryItem = result.gameObject.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    physicsItem = inventoryItem.GetComponent<PhysicsInventoryItem>();
                    if (physicsItem != null && physicsItem.IsPhysicsActive)
                    {
                        Debug.Log($"Found physics item via InventoryItem: {physicsItem.name} at {screenPosition}");
                        return physicsItem;
                    }
                }
            }

            // �������� ã�� ����
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in GetPhysicsItemAtPosition: {e.Message}");
            return null;
        }
    }



    public void ConvertToPhysicsItem(InventoryItem item, Vector2 position)
    {
        if (item == null) return;

        if (enableDebugLogs)
        {
            Debug.Log($"Converting {item.name} to physics item at position {position}");
        }

        try
        {
            // ��ġ üũ - ���������� �� üũ �� ����
            if (float.IsNaN(position.x) || float.IsNaN(position.y) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y) ||
                Mathf.Abs(position.x) > 10000f || Mathf.Abs(position.y) > 10000f)
            {
                Debug.LogWarning($"Abnormal position detected: {position}. Using safe position instead.");

                // ������ ��ġ�� ��ü (ȭ�� �߾�)
                position = new Vector2(Screen.width / 2, Screen.height / 2);
            }

            // 1. �׸��忡�� ������ ���� (���� �ڵ�)
            Vector2Int gridPos = item.GridPosition;
            if (mainGrid != null && item.OnGrid)
            {
                mainGrid.RemoveItem(gridPos);
                if (enableDebugLogs) Debug.Log($"Removed item from grid at position {gridPos}");
            }

            // 2. ĵ���� ���� Ȯ��
            if (parentCanvas == null)
            {
                InitializeCanvasReference();
            }

            // 3. �������� ĵ������ �ڽ����� ����
            if (parentCanvas != null)
            {
                item.transform.SetParent(parentCanvas.transform, false);

                // ���� �������� Grid�� �ڽ��� �ƴϹǷ� Scale�� ���������� ����
                item.transform.localScale = new Vector3(6, 6, 1);
            }
            else
            {
                Debug.LogWarning("No canvas found! Item may not display correctly.");
            }

            // 4. ������ ��ġ ���� - �հ��� ��ġ ���
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                // �߿�: ���� �������� �ƴ� UI ��ġ�� ����
                rt.position = new Vector3(position.x, position.y, rt.position.z);
                if (enableDebugLogs) Debug.Log($"Set item position to {rt.position}");
            }

            // 5. �׸��� ��ġ �ʱ�ȭ
            item.SetGridPosition(new Vector2Int(-1, -1));

            // 6. ���� ������Ʈ �߰� �Ǵ� ��������
            PhysicsInventoryItem physicsItem = item.GetComponent<PhysicsInventoryItem>();
            if (physicsItem == null)
            {
                physicsItem = item.gameObject.AddComponent<PhysicsInventoryItem>();

                // �߿�: �ʿ��� ��� PhysicsInventoryItem�� �ʱ�ȭ�� �����մϴ�
                physicsItem.ForceInitialize();
                if (enableDebugLogs) Debug.Log("Added new PhysicsInventoryItem component");
            }
            // ĳ�̵� �ٴ� ���� ����
            if (!isFloorPositionCached) CacheFloorPosition();
            physicsItem.FloorY = cachedFloorY;

            // 7. ������ ��ġ ���� �� ���� Ȱ��ȭ
            Vector2 initialVelocity = Vector2.up * 100f + new Vector2(Random.Range(-50f, 50f), 0f);
            physicsItem.SetSpawnPosition(rt.position);
            physicsItem.ActivatePhysics(initialVelocity, position);

            // 8. ���� ��Ͽ� �߰�
            if (!physicsItems.Contains(physicsItem))
            {
                physicsItems.Add(physicsItem);
                if (enableDebugLogs) Debug.Log($"Added physics item to managed list. Count: {physicsItems.Count}");
            }

            // 9. ���� �ð� ���� ������ ��ȣ
            StartCoroutine(ProtectPhysicsItem(physicsItem, 5f));

            // 10. �̹��� ������Ʈ�� Ȱ��ȭ�Ǿ� �ִ��� Ȯ��
            Image itemImage = item.GetComponent<Image>();
            if (itemImage != null && !itemImage.enabled)
            {
                itemImage.enabled = true;
                Debug.Log("Forced image component to be enabled");
            }

            // 11. ���� ������Ʈ�� Ȱ��ȭ�Ǿ� �ִ��� Ȯ��
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                Debug.Log("Forced game object to be active");
            }

            if (enableDebugLogs) Debug.Log($"Physics item conversion complete for {item.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ConvertToPhysicsItem: {e.Message}\n{e.StackTrace}");
        }
    }
    private IEnumerator ProtectPhysicsItem(PhysicsInventoryItem item, float protectionDuration)
    {
        if (item == null) yield break;

        // �ð� ��� ��ȣ ���
        item.SetProtected(true, protectionDuration);

        // ��ȣ �Ⱓ ���� �ֱ������� Ȱ�� ���� Ȯ��
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < protectionDuration && item != null)
        {
            // ��Ȱ��ȭ�ƴٸ� ������ Ȱ��ȭ
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                if (enableDebugLogs) Debug.Log("Forced protected item to stay active");
            }

            yield return new WaitForSeconds(0.5f); // 0.5�ʸ��� üũ
        }

        // ��ȣ �Ⱓ�� ���� �Ŀ��� �� �� �� Ȯ��
        if (item != null && !item.gameObject.activeSelf)
        {
            item.gameObject.SetActive(true);
        }
    }

    public void DebugObjectPoolStatus()
    {
        Debug.Log("===== Object Pool Debug Information =====");

        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool.Instance is null! Make sure ObjectPool is created.");
            return;
        }

        string poolTag = "PhysicsInventoryItem";
        bool poolExists = ObjectPool.Instance.DoesPoolExist(poolTag);
        Debug.Log($"Pool '{poolTag}' exists: {poolExists}");

        if (poolExists)
        {
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            Debug.Log($"Available items in pool: {availableCount}");
        }

        Debug.Log($"Weapon prefab reference: {(weaponPrefab != null ? weaponPrefab.name : "NULL")}");
        if (weaponPrefab != null)
        {
            PhysicsInventoryItem physComp = weaponPrefab.GetComponent<PhysicsInventoryItem>();
            Debug.Log($"PhysicsInventoryItem component on prefab: {(physComp != null ? "Found" : "Not Found")}");
        }

        Debug.Log("========================================");
    }

    public void LogCanvasHierarchy()
    {
        Debug.Log("===== Canvas Hierarchy =====");

        // CombatUI ã��
        GameObject combatUI = GameObject.Find("CombatUI");
        if (combatUI != null)
        {
            Debug.Log($"Found CombatUI: {combatUI.name}");
            LogChildHierarchy(combatUI.transform, 1);
        }
        else
        {
            Debug.Log("CombatUI not found");
        }

        // ��� ĵ���� ���
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"Total canvases in scene: {canvases.Length}");
        for (int i = 0; i < canvases.Length; i++)
        {
            Debug.Log($"Canvas[{i}]: {canvases[i].name}, RenderMode: {canvases[i].renderMode}");
        }

        Debug.Log("===========================");
    }

    private void LogChildHierarchy(Transform parent, int depth)
    {
        if (depth > 5) return; // �ʹ� ���� ���� �ʵ���

        string indent = new string(' ', depth * 2);

        foreach (Transform child in parent)
        {
            string typeInfo = "";

            if (child.GetComponent<Canvas>() != null)
                typeInfo += "[Canvas] ";
            if (child.GetComponent<InventoryController>() != null)
                typeInfo += "[InventoryController] ";
            if (child.GetComponent<PhysicsInventoryManager>() != null)
                typeInfo += "[PhysicsInventoryManager] ";
            if (child.GetComponent<ItemGrid>() != null)
                typeInfo += "[ItemGrid] ";

            Debug.Log($"{indent}- {child.name} {typeInfo}");

            // ��������� �ڽĵ� ���
            LogChildHierarchy(child, depth + 1);
        }
    }
    // Add this new method to PhysicsInventoryManager.cs
    private IEnumerator PreventImmediateRemoval(PhysicsInventoryItem item)
    {
        if (item == null) yield break;

        // Ignore this item in removal checks for 2 seconds
        float protectionTime = 2.0f;
        float startTime = Time.time;

        while (item != null && Time.time - startTime < protectionTime)
        {
            // Force item to stay active
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                Debug.Log("Forcing physics item to stay active");
            }

            yield return null;
        }
    }
    /// <summary>
    /// ��� ����� ������ ���� (�ڼ� ȿ�� � ���)
    /// </summary>
    public void CollectAllItems(Transform target)
    {
        foreach (var item in physicsItems.ToList())
        {
            if (item != null && !item.IsBeingDragged)
            {
                // ��� �������� �̵��ϴ� ���� ȿ��
                Vector2 direction = (target.position - item.transform.position).normalized;
                item.ActivatePhysics(direction * 500f);
            }
        }
    }

    /// <summary>
    /// �κ��丮 �׸��忡 Ư�� �������� ���� ������ �ִ��� Ȯ��
    /// </summary>
    public bool HasSpaceForItem(WeaponData weaponData)
    {
        if (mainGrid == null || weaponData == null || weaponPrefab == null)
        {
            return false;
        }

        // Ǯ���� ������ �ӽ� �뿩 �Ǵ� �ӽ� ����
        GameObject tempObj;
        bool fromPool = false;

        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag) &&
            ObjectPool.Instance.GetAvailableCount(poolTag) > 0)
        {
            // Ǯ���� ������ �������� (ȭ�� �� ��ġ)
            tempObj = ObjectPool.Instance.SpawnFromPool(poolTag, new Vector3(-10000, -10000, 0), Quaternion.identity);
            fromPool = true;
        }
        else
        {
            // �ӽ� ������ ����
            tempObj = Instantiate(weaponPrefab);
        }

        if (tempObj == null)
        {
            return false;
        }

        InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
        if (tempItem == null)
        {
            // ��� �� ó��
            if (fromPool)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
            }
            else
            {
                Destroy(tempObj);
            }
            return false;
        }

        // �ӽ÷� ���� ������ �ʱ�ȭ
        tempItem.Initialize(weaponData);

        // �׸��忡�� ���� ã��
        Vector2Int? freePosition = mainGrid.FindSpaceForObject(tempItem);

        // ��� �� ������ ó��
        if (fromPool)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
        }
        else
        {
            Destroy(tempObj);
        }

        return freePosition.HasValue;
    }




    private void UpdateActivePhysicsItems()
    {
        if (physicsItems.Count == 0) return;

        // ����� �α״� ���������� Ȱ��ȭ
        if (enableDebugLogs && Time.frameCount % 300 == 0) // 10�ʿ� �� ������ �α� ��� (30fps ����)
        {
            Debug.Log($"Active physics items: {physicsItems.Count}, Screen: ({Screen.width}, {Screen.height})");
        }

        List<PhysicsInventoryItem> itemsToRemove = new List<PhysicsInventoryItem>();

        foreach (var item in physicsItems)
        {
            if (item == null)
            {
                itemsToRemove.Add(item);
                continue;
            }

            // ��ȣ �����̰ų� �巡�� ���� �������� ����
            if (item.IsProtected || item.IsBeingDragged)
            {
                continue;
            }

            // �������� �ֱٿ� Ȱ��ȭ�Ǿ����� �������� ����
            float activationAge = Time.time - item.ActivationTime;
            if (activationAge < 10f) // Ȱ��ȭ �� 10�� ������ ��ȣ
            {
                continue;
            }

            // ��ġ Ȯ��
            Vector3 itemPosition = item.transform.position;

            // �ش������� ū ��ġ���� üũ
            bool isExtremePosition = Mathf.Abs(itemPosition.x) > 50000f ||
                                    Mathf.Abs(itemPosition.y) > 50000f ||
                                    Mathf.Abs(itemPosition.z) > 50000f;

            // NaN�̳� Infinity üũ
            bool hasInvalidValues = float.IsNaN(itemPosition.x) || float.IsInfinity(itemPosition.x) ||
                                   float.IsNaN(itemPosition.y) || float.IsInfinity(itemPosition.y) ||
                                   float.IsNaN(itemPosition.z) || float.IsInfinity(itemPosition.z);

            if (isExtremePosition || hasInvalidValues)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Item {item.name} at {itemPosition} has extreme position values. Marking for removal.");
                }
                itemsToRemove.Add(item);
            }
        }

        // ���� ��� ó��
        foreach (var item in itemsToRemove)
        {
            if (item != null)
            {
                ReturnItemToPool(item);
            }
        }

        // ��Ͽ��� null ���� ����
        physicsItems.RemoveAll(item => item == null);
    }
    /// �������� Ǯ�� ��ȯ
    /// </summary>
    private void ReturnItemToPool(PhysicsInventoryItem item)
    {
        if (item == null) return;

        physicsItems.Remove(item);

        // ���� ��Ȱ��ȭ
        item.DeactivatePhysics();

        // Ǯ�� ��ȯ
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, item.gameObject);
        }
        else
        {
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// ��� �������� Ǯ�� ��ȯ
    /// </summary>
    private void ReturnAllItemsToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        foreach (var item in physicsItems.ToList())
        {
            if (item != null)
            {
                ReturnItemToPool(item);
            }
        }

        physicsItems.Clear();
    }

    public void ReturnPhysicsItemToGrid(PhysicsInventoryItem physicsItem)
    {
        if (physicsItem == null || mainGrid == null) return;

        try
        {
            // ���� Ȱ��ȭ ���� ����
            physicsItem.DeactivatePhysics();

            // InventoryItem ������Ʈ ��������
            InventoryItem inventoryItem = physicsItem.GetComponent<InventoryItem>();
            if (inventoryItem != null)
            {
                // �׸��忡 �� ���� ã��
                Vector2Int? freePosition = mainGrid.FindSpaceForObject(inventoryItem);

                if (freePosition.HasValue)
                {
                    // �׸��忡 �� �� Scale�� 1,1,1�� ����
                    RectTransform rt = inventoryItem.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one;
                    }

                    // �׸��� �θ�� ����
                    inventoryItem.transform.SetParent(mainGrid.transform, false);

                    // �׸��忡 ��ġ
                    InventoryItem overlapItem = null;
                    mainGrid.PlaceItem(inventoryItem, freePosition.Value, ref overlapItem);

                    // ���� ������ ��Ͽ��� ����
                    physicsItems.Remove(physicsItem);

                    Debug.Log($"Successfully placed physics item {inventoryItem.name} in grid at {freePosition.Value}");
                    return;
                }
            }

            // �׸��忡 ������ ���ų� ������ ���, ���� ȿ�� �ٽ� Ȱ��ȭ
            physicsItem.ActivatePhysics();
            Debug.Log("No space in grid, reactivating physics");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ReturnPhysicsItemToGrid: {e.Message}");
            // ���� �� ���� ȿ�� �ٽ� Ȱ��ȭ
            physicsItem.ActivatePhysics();
        }
    }
   
    public void RemovePhysicsItem(PhysicsInventoryItem item)
    {
        if (item == null) return;

        try
        {
            // ��Ͽ��� ����
            physicsItems.Remove(item);

            // ���� ��Ȱ��ȭ
            item.DeactivatePhysics();
          
            // ��� ȿ�� ���� (�ʿ��� ���)
            InventoryItem inventoryItem = item.GetComponent<InventoryItem>();
            if (inventoryItem != null && inventoryItem.GetWeaponData() != null)
            {
                WeaponData weaponData = inventoryItem.GetWeaponData();
                if (weaponData.weaponType == WeaponType.Equipment)
                {
                    var weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();
                    weaponManager?.UnequipWeapon(weaponData);
                }
            }

            // ������Ʈ Ǯ�� ��ȯ �Ǵ� �ı�
            if (useObjectPool && ObjectPool.Instance != null)
            {
                string poolTag = "PhysicsInventoryItem";
                ObjectPool.Instance.ReturnToPool(poolTag, item.gameObject);
                if (enableDebugLogs) Debug.Log($"Returned {item.name} to object pool");
            }
            else
            {
                Destroy(item.gameObject);
                if (enableDebugLogs) Debug.Log($"Destroyed {item.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error removing physics item: {e.Message}");
        }
        // WeaponInfoUI ������Ʈ (���õ� �������� ���ŵ� ���)
        if (weaponInfoUI != null && selectedPhysicsItem == item)
        {
            selectedPhysicsItem = null;
            weaponInfoUI.gameObject.SetActive(false);
        }
    }  
    #endregion
}