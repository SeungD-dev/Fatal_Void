using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// 물리 기반 아이템을 관리하는 매니저 클래스 - 오브젝트 풀 활용 버전
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
    [Header("Physics Settings")]
    [SerializeField] private float dragThreshold = 0.3f;
    [SerializeField] private float holdDelay = 0.3f;
    [SerializeField] private float physicsSpawnOffset = 50f;
    [SerializeField] private float physicsRandomVariance = 30f;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int ensurePoolSize = 10; // 최소한 유지할 풀 크기
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
    private bool enableDebugLogs = true; // 배포 시 false로 설정 권장
    private int updateCounter = 0;
    private float lastPerformanceCheck = 0f;
    private bool hasPerformanceWarning = false;
    private RectTransform canvasRectTransform;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Debug.Log("PhysicsInventoryManager Awake called");

        InitializeComponents();
        InitializeCanvasReference();
        InitializePhysicsItemPool();

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

        // 진행 중인 코루틴 정지
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

        // 물리 아이템 개수 기반 성능 관리
        if (Time.time - lastPerformanceCheck > 5f) // 5초마다 성능 체크
        {
            lastPerformanceCheck = Time.time;
            MonitorPerformance();
        }

        // 물리 아이템 정상 상태 확인 (이미지가 누락된 경우 체크)
        // 이 검사는 프레임 비용이 높으므로 간헐적으로만 실행
        updateCounter++;
        if (updateCounter % 120 == 0) // 약 4초마다 한 번씩 (30fps 기준)
        {
            EnsurePhysicsItemsVisible();
            updateCounter = 0;
        }

        // 선택된 아이템이 있고 드래그 중이면 위치 업데이트
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

        // 성능 최적화: 활성 물리 아이템 업데이트
        // 물리 아이템 개수가 많아지면 검사 빈도 감소
        int checkInterval = DetermineUpdateInterval();
        if (Time.frameCount % checkInterval == 0)
        {
            UpdateActivePhysicsItems();
        }
    }

    private void MonitorPerformance()
    {
        int itemCount = physicsItems.Count;

        if (enableDebugLogs)
        {
            Debug.Log($"Active physics items: {itemCount}");
        }

        // 아이템 수가 너무 많으면 경고 및 최적화 조치
        if (itemCount > 30 && !hasPerformanceWarning)
        {
            Debug.LogWarning($"High number of physics items ({itemCount}) may impact performance");
            hasPerformanceWarning = true;

            // 오래된 아이템 부터 일부 제거 (선택적)
            if (itemCount > 50) // 아이템이 50개 이상이면 가장 오래된 것부터 제거
            {
                int itemsToRemove = itemCount - 40; // 40개까지만 유지
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

        if (itemCount < 10) return 10;      // 10개 미만: 매 10프레임마다
        else if (itemCount < 20) return 20; // 10-20개: 매 20프레임마다
        else if (itemCount < 30) return 30; // 20-30개: 매 30프레임마다
        else return 60;                     // 30개 이상: 매 60프레임마다
    }
    private void RemoveOldestItems(int count)
    {
        if (physicsItems.Count <= count) return;

        // 활성화 시간 기준 정렬
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

            // 게임 오브젝트 활성화 상태 확인
            if (!item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                if (enableDebugLogs) Debug.Log($"Re-activated game object: {item.name}");
            }

            // 이미지 컴포넌트가 비활성화된 경우 다시 활성화
            Image image = item.GetComponent<Image>();
            if (image != null && !image.enabled)
            {
                image.enabled = true;
                if (enableDebugLogs) Debug.Log($"Restored visibility for physics item: {item.name}");
            }

            // 불투명도가 낮은 경우 복구
            if (image != null && image.color.a < 0.5f)
            {
                Color color = image.color;
                color.a = 1.0f;
                image.color = color;
                if (enableDebugLogs) Debug.Log($"Restored opacity for physics item: {item.name}");
            }

            // Canvas Group이 비활성화된 경우
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

        // 1. 직접 계층 구조를 통해 찾기
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

        // 2. 명시적인 경로 탐색
        if (parentCanvas == null)
        {
            // CombatUI 하위 캔버스 찾기
            GameObject combatUI = GameObject.Find("CombatUI");
            if (combatUI != null)
            {
                // Canvas 하위 객체 찾기
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

                // 직접 컴포넌트 참조
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

        // 3. 씬에서 직접 찾기
        if (parentCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length > 0)
            {
                // 첫 번째로 ScreenSpaceOverlay 모드인 캔버스 찾기
                foreach (var canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        parentCanvas = canvas;
                        Debug.Log($"Found ScreenSpaceOverlay canvas: {canvas.name}");
                        break;
                    }
                }

                // 못 찾았다면 첫 번째 캔버스 사용
                if (parentCanvas == null && canvases.Length > 0)
                {
                    parentCanvas = canvases[0];
                    Debug.Log($"Using first available canvas: {parentCanvas.name}");
                }
            }
        }

        // 찾은 캔버스 설정
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
            touchActions.Dispose();
        }

        // 풀에 아이템 반환
        ReturnAllItemsToPool();
    }
    #endregion

    #region Initialization
    private IEnumerator DelayedInitialization()
    {
        // 첫 프레임은 건너뛰어 다른 초기화가 완료되도록 함
        yield return null;

        // 오브젝트 풀 사용시 미리 초기화
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
            // InventoryController에서 spawnPoint 가져오기
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
            // InventoryController의 weaponPrefab 필드 가져오기
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
    /// 물리 인벤토리 아이템을 위한 오브젝트 풀 초기화
    /// </summary>
    private void InitializePhysicsItemPool()
    {
        Debug.Log("Initializing physics item pool...");

        // 풀 태그 정의 - 씬의 태그와 일치하도록 함
        string poolTag = "PhysicsInventoryItem";

        // 오브젝트 풀 참조 확인
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool instance is missing! Cannot initialize physics item pool.");
            return;
        }

        // 1. 풀이 이미 존재하는지 확인
        bool poolExists = ObjectPool.Instance.DoesPoolExist(poolTag);
        Debug.Log($"Pool '{poolTag}' exists: {poolExists}");

        // 2. 웨폰 프리팹 검사
        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab reference is missing! Cannot initialize physics item pool.");
            return;
        }

        // 3. 필요한 경우 PhysicsInventoryItem 컴포넌트가 있는지 확인
        PhysicsInventoryItem testComponent = weaponPrefab.GetComponent<PhysicsInventoryItem>();
        if (testComponent == null)
        {
            Debug.LogWarning("PhysicsInventoryItem component not found on weapon prefab. It will be added at runtime.");
        }

        // 4. 풀 생성 또는 확장
        if (!poolExists)
        {
            // 새 풀 생성
            int initialPoolSize = 20;
            ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
            Debug.Log($"Created new physics item pool with size {initialPoolSize}");
        }
        else
        {
            // 풀 크기 확인 및 필요시 확장
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
    /// 물리 기반 인벤토리 아이템 생성 - 오브젝트 풀 활용
    /// </summary>
    public InventoryItem CreatePhysicsItem(WeaponData weaponData)
    {
        if (weaponData == null || weaponPrefab == null) return null;

        // 랜덤한 스폰 위치 계산
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnPosition += new Vector3(
            Random.Range(-physicsRandomVariance, physicsRandomVariance),
            physicsSpawnOffset + Random.Range(0, physicsRandomVariance),
            0
        );

        GameObject itemObj;

        // 오브젝트 풀에서 가져오기
        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // 가용 오브젝트가 충분한지 확인
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < 1)
            {
                // 풀 확장
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize);
                Debug.Log($"Physics item pool expanded by {ensurePoolSize} items");
            }

            // 풀에서 오브젝트 가져오기
            itemObj = ObjectPool.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        }
        else
        {
            // 오브젝트 풀을 사용하지 않거나 사용할 수 없는 경우 직접 생성
            itemObj = Instantiate(weaponPrefab, spawnPosition, Quaternion.identity, parentCanvas.transform);
        }

        if (itemObj == null)
        {
            Debug.LogError("Failed to create physics inventory item");
            return null;
        }

        // 인벤토리 아이템 컴포넌트 가져오기
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

        // 무기 데이터 초기화
        inventoryItem.Initialize(weaponData);
        inventoryItem.SetGridPosition(new Vector2Int(-1, -1)); // 그리드 외부 표시

        // 물리 컴포넌트 추가 또는 가져오기
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // 위치 설정 및 물리 활성화
        physicsItem.SetSpawnPosition(spawnPosition);
        physicsItem.ActivatePhysics();

        // 관리 목록에 추가
        physicsItems.Add(physicsItem);

        return inventoryItem;
    }

    /// <summary>
    /// 인벤토리 그리드에 빈 공간이 없을 때 물리 아이템으로 생성
    /// </summary>
    public void HandleFullInventory(WeaponData weaponData)
    {
        CreatePhysicsItem(weaponData);

        // 효과음 재생 (있는 경우)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemDrop_sfx", 0.8f, false);
        }
    }
    #endregion

    #region Touch Handling
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // 터치 위치 가져오기
        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        touchStartPosition = touchPos;

        Debug.Log($"Touch started at {touchPos}");

        // 그리드 내 아이템 먼저 체크
        Vector2Int gridPosition = mainGrid?.GetGridPosition(touchPos) ?? new Vector2Int(-1, -1);
        if (mainGrid != null && mainGrid.IsValidPosition(gridPosition))
        {
            // 그리드 내 아이템 터치 처리는 InventoryController가 담당
            Debug.Log("Touch is inside grid area, skipping physics item check");
            return;
        }

        // 물리 아이템 터치 체크
        PhysicsInventoryItem touchedItem = GetPhysicsItemAtPosition(touchPos);
        if (touchedItem != null)
        {
            Debug.Log($"Found physics item to drag: {touchedItem.name}");

            // 물리 아이템 선택
            selectedPhysicsItem = touchedItem;

            // 홀드 체크 시작
            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
            }
            holdCoroutine = StartCoroutine(CheckForHold(touchedItem, touchPos));
        }
        else
        {
            Debug.Log("No physics item found at touch position");
        }
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

            // 홀드 시간이 지났거나, 일정 거리 이상 움직였을 경우
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

        // 물리 비활성화하고 드래그 시작
        item.StartDrag(position);

        // 효과음 재생 (SoundManager가 있는 경우)
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

            // 드래그 종료 처리
            selectedPhysicsItem.EndDrag(mainGrid, finalPosition);

            
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }

            // 상태 초기화
            ResetDragState();
        }

        // 홀드 코루틴 중지
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
    /// 특정 위치에 있는 물리 아이템 찾기
    /// </summary>
    private PhysicsInventoryItem GetPhysicsItemAtPosition(Vector2 screenPosition)
    {
        try
        {
            // 레이캐스트 수행
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // 결과 처리 - 물리 아이템 직접 찾기
            foreach (RaycastResult result in results)
            {
                // 아이템 자체에서 컴포넌트 찾기
                PhysicsInventoryItem physicsItem = result.gameObject.GetComponent<PhysicsInventoryItem>();
                if (physicsItem != null)
                {
                    Debug.Log($"Found physics item: {physicsItem.name} at {screenPosition}");
                    return physicsItem;
                }

                // 부모 객체에서도 확인
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

                // InventoryItem 찾기 (PhysicsInventoryItem이 부착되어 있을 수 있음)
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

            // 아이템을 찾지 못함
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
            // 위치 체크 - 비정상적인 값 체크 및 보정
            if (float.IsNaN(position.x) || float.IsNaN(position.y) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y) ||
                Mathf.Abs(position.x) > 10000f || Mathf.Abs(position.y) > 10000f)
            {
                Debug.LogWarning($"Abnormal position detected: {position}. Using safe position instead.");

                // 안전한 위치로 대체 (화면 중앙)
                position = new Vector2(Screen.width / 2, Screen.height / 2);
            }

            // 1. 그리드에서 아이템 제거 (기존 코드)
            Vector2Int gridPos = item.GridPosition;
            if (mainGrid != null && item.OnGrid)
            {
                mainGrid.RemoveItem(gridPos);
                if (enableDebugLogs) Debug.Log($"Removed item from grid at position {gridPos}");
            }

            // 2. 캔버스 참조 확인
            if (parentCanvas == null)
            {
                InitializeCanvasReference();
            }

            // 3. 아이템을 캔버스의 자식으로 설정
            if (parentCanvas != null)
            {
                item.transform.SetParent(parentCanvas.transform, false);

                // 물리 아이템은 Grid의 자식이 아니므로 Scale을 명시적으로 설정
                item.transform.localScale = new Vector3(6, 6, 1);
            }
            else
            {
                Debug.LogWarning("No canvas found! Item may not display correctly.");
            }

            // 4. 아이템 위치 설정 - 손가락 위치 사용
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 중요: 월드 포지션이 아닌 UI 위치로 설정
                rt.position = new Vector3(position.x, position.y, rt.position.z);
                if (enableDebugLogs) Debug.Log($"Set item position to {rt.position}");
            }

            // 5. 그리드 위치 초기화
            item.SetGridPosition(new Vector2Int(-1, -1));

            // 6. 물리 컴포넌트 추가 또는 가져오기
            PhysicsInventoryItem physicsItem = item.GetComponent<PhysicsInventoryItem>();
            if (physicsItem == null)
            {
                physicsItem = item.gameObject.AddComponent<PhysicsInventoryItem>();

                // 중요: 필요한 경우 PhysicsInventoryItem의 초기화를 강제합니다
                physicsItem.ForceInitialize();
                if (enableDebugLogs) Debug.Log("Added new PhysicsInventoryItem component");
            }

            // 7. 아이템 위치 설정 및 물리 활성화
            Vector2 initialVelocity = Vector2.up * 100f + new Vector2(Random.Range(-50f, 50f), 0f);
            physicsItem.SetSpawnPosition(rt.position);
            physicsItem.ActivatePhysics(initialVelocity, position);

            // 8. 관리 목록에 추가
            if (!physicsItems.Contains(physicsItem))
            {
                physicsItems.Add(physicsItem);
                if (enableDebugLogs) Debug.Log($"Added physics item to managed list. Count: {physicsItems.Count}");
            }

            // 9. 일정 시간 동안 아이템 보호
            StartCoroutine(ProtectPhysicsItem(physicsItem, 5f));

            // 10. 이미지 컴포넌트가 활성화되어 있는지 확인
            Image itemImage = item.GetComponent<Image>();
            if (itemImage != null && !itemImage.enabled)
            {
                itemImage.enabled = true;
                Debug.Log("Forced image component to be enabled");
            }

            // 11. 게임 오브젝트가 활성화되어 있는지 확인
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

        // 시간 기반 보호 사용
        item.SetProtected(true, protectionDuration);

        // 보호 기간 동안 주기적으로 활성 상태 확인
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < protectionDuration && item != null)
        {
            // 비활성화됐다면 강제로 활성화
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                if (enableDebugLogs) Debug.Log("Forced protected item to stay active");
            }

            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
        }

        // 보호 기간이 끝난 후에도 한 번 더 확인
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

        // CombatUI 찾기
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

        // 모든 캔버스 출력
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
        if (depth > 5) return; // 너무 깊이 들어가지 않도록

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

            // 재귀적으로 자식들 출력
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
    /// 모든 드랍된 아이템 수집 (자석 효과 등에 사용)
    /// </summary>
    public void CollectAllItems(Transform target)
    {
        foreach (var item in physicsItems.ToList())
        {
            if (item != null && !item.IsBeingDragged)
            {
                // 대상 방향으로 이동하는 물리 효과
                Vector2 direction = (target.position - item.transform.position).normalized;
                item.ActivatePhysics(direction * 500f);
            }
        }
    }

    /// <summary>
    /// 인벤토리 그리드에 특정 아이템을 위한 공간이 있는지 확인
    /// </summary>
    public bool HasSpaceForItem(WeaponData weaponData)
    {
        if (mainGrid == null || weaponData == null || weaponPrefab == null)
        {
            return false;
        }

        // 풀에서 아이템 임시 대여 또는 임시 생성
        GameObject tempObj;
        bool fromPool = false;

        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag) &&
            ObjectPool.Instance.GetAvailableCount(poolTag) > 0)
        {
            // 풀에서 아이템 가져오기 (화면 밖 위치)
            tempObj = ObjectPool.Instance.SpawnFromPool(poolTag, new Vector3(-10000, -10000, 0), Quaternion.identity);
            fromPool = true;
        }
        else
        {
            // 임시 아이템 생성
            tempObj = Instantiate(weaponPrefab);
        }

        if (tempObj == null)
        {
            return false;
        }

        InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
        if (tempItem == null)
        {
            // 사용 후 처리
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

        // 임시로 무기 데이터 초기화
        tempItem.Initialize(weaponData);

        // 그리드에서 공간 찾기
        Vector2Int? freePosition = mainGrid.FindSpaceForObject(tempItem);

        // 사용 후 아이템 처리
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

        // 디버깅 로그는 선택적으로 활성화
        if (enableDebugLogs && Time.frameCount % 300 == 0) // 10초에 한 번씩만 로그 출력 (30fps 기준)
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

            // 보호 상태이거나 드래그 중인 아이템은 무시
            if (item.IsProtected || item.IsBeingDragged)
            {
                continue;
            }

            // 아이템이 최근에 활성화되었으면 제거하지 않음
            float activationAge = Time.time - item.ActivationTime;
            if (activationAge < 10f) // 활성화 후 10초 동안은 보호
            {
                continue;
            }

            // 위치 확인
            Vector3 itemPosition = item.transform.position;

            // 극단적으로 큰 위치값만 체크
            bool isExtremePosition = Mathf.Abs(itemPosition.x) > 50000f ||
                                    Mathf.Abs(itemPosition.y) > 50000f ||
                                    Mathf.Abs(itemPosition.z) > 50000f;

            // NaN이나 Infinity 체크
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

        // 제거 대상 처리
        foreach (var item in itemsToRemove)
        {
            if (item != null)
            {
                ReturnItemToPool(item);
            }
        }

        // 목록에서 null 참조 제거
        physicsItems.RemoveAll(item => item == null);
    }
    /// 아이템을 풀로 반환
    /// </summary>
    private void ReturnItemToPool(PhysicsInventoryItem item)
    {
        if (item == null) return;

        physicsItems.Remove(item);

        // 물리 비활성화
        item.DeactivatePhysics();

        // 풀로 반환
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
    /// 모든 아이템을 풀로 반환
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
            // 물리 활성화 상태 해제
            physicsItem.DeactivatePhysics();

            // InventoryItem 컴포넌트 가져오기
            InventoryItem inventoryItem = physicsItem.GetComponent<InventoryItem>();
            if (inventoryItem != null)
            {
                // 그리드에 빈 공간 찾기
                Vector2Int? freePosition = mainGrid.FindSpaceForObject(inventoryItem);

                if (freePosition.HasValue)
                {
                    // 그리드에 들어갈 때 Scale을 1,1,1로 변경
                    RectTransform rt = inventoryItem.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one;
                    }

                    // 그리드 부모로 설정
                    inventoryItem.transform.SetParent(mainGrid.transform, false);

                    // 그리드에 배치
                    InventoryItem overlapItem = null;
                    mainGrid.PlaceItem(inventoryItem, freePosition.Value, ref overlapItem);

                    // 물리 아이템 목록에서 제거
                    physicsItems.Remove(physicsItem);

                    Debug.Log($"Successfully placed physics item {inventoryItem.name} in grid at {freePosition.Value}");
                    return;
                }
            }

            // 그리드에 공간이 없거나 실패한 경우, 물리 효과 다시 활성화
            physicsItem.ActivatePhysics();
            Debug.Log("No space in grid, reactivating physics");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ReturnPhysicsItemToGrid: {e.Message}");
            // 실패 시 물리 효과 다시 활성화
            physicsItem.ActivatePhysics();
        }
    }

    public List<PhysicsInventoryItem> GetAllPhysicsItems()
    {
        // 리스트를 복사하여 반환 (원본 목록 변경 방지)
        return new List<PhysicsInventoryItem>(physicsItems);
    }
    public void RemovePhysicsItem(PhysicsInventoryItem item)
    {
        if (item == null) return;

        try
        {
            // 목록에서 제거
            physicsItems.Remove(item);

            // 물리 비활성화
            item.DeactivatePhysics();
          
            // 장비 효과 제거 (필요한 경우)
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

            // 오브젝트 풀로 반환 또는 파괴
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
    }
    public PhysicsInventoryItem GetDraggedPhysicsItem()
    {
        return isDragging ? selectedPhysicsItem : null;
    }
    #endregion
}