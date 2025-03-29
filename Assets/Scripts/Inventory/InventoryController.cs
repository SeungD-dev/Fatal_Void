using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 인벤토리 시스템의 메인 컨트롤러
/// 아이템 상호작용과 UI 관리를 담당
/// </summary>
public class InventoryController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Grid Settings")]
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private WeaponInfoUI weaponInfoUI;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Button progressButton;

    [Header("UI References")]
    [SerializeField] private GameObject shopUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private PhysicsInventoryManager physicsManager;
    [Header("Notice UI")]
    [SerializeField] private GameObject noticeUI;
    [SerializeField] private TMPro.TextMeshProUGUI noticeText;
    [SerializeField] private float noticeDisplayTime = 2f;
    [Header("Transition Effect")]
    [SerializeField] private ScreenTransitionEffect transitionEffect;

    private Coroutine currentNoticeCoroutine;
    #endregion

     #region Private Fields
    private ItemInteractionManager itemInteractionManager;
    private WeaponManager weaponManager;
    private InventoryHighlight inventoryHighlight;
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private PlayerStats playerStats;

    private static readonly Vector2 ITEM_LIFT_OFFSET_VECTOR = Vector2.up * ITEM_LIFT_OFFSET;
    private static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    private Vector2 originalSpawnPosition;
    private const float HOLD_THRESHOLD = 0.3f;
    private const float HOLD_MOVE_THRESHOLD = 20f;
    public const float ITEM_LIFT_OFFSET = 350f;
    private InventoryItem selectedItem;
    private bool isDragging = false;
    private bool isHolding = false;
    private RectTransform selectedItemRectTransform;
    private float lastRotationTime = -999f;
    private const float ROTATION_COOLDOWN = 0.3f;
    private int lastTouchCount = 0;
    private bool lastPressState = false;
    private float lastTouchTime = 0f;
    private bool isInventoryInitialized;
    private readonly HashSet<InventoryItem> activeItems = new HashSet<InventoryItem>(16);
    private TouchState touchState = new TouchState();
    private bool isInputSystemInitialized;
    private int lastRotatingTouchId = -1;

    private struct TouchState
    {
        public bool isDragging;
        public bool isHolding;
        public float lastTouchTime;
        public int lastTouchCount;
        public bool lastPressState;
        public float lastRotationTime;
        public int lastRotatingTouchId;

        public void Reset()
        {
            isDragging = false;
            isHolding = false;
            lastTouchCount = 0;
            lastRotatingTouchId = -1;
        }
    }

    #endregion

    #region Properties
    private ItemGrid selectedItemGrid;
    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight?.SetParent(value);
        }
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (!TryInitializeComponents())
        {
            Debug.LogError("Failed to initialize essential components");
            return;
        }

        SetupInputSystem();
        InitializeGrid();
    }
    private bool TryInitializeComponents()
    {
        try
        {
            inventoryHighlight = GetComponentInChildren<InventoryHighlight>();
            weaponManager = GameObject.FindWithTag("Player")?.GetComponent<WeaponManager>();

            if (mainInventoryGrid == null || weaponInfoUI == null || itemSpawnPoint == null)
            {
                return false;
            }

            itemInteractionManager = new ItemInteractionManager(
                mainInventoryGrid,
                weaponInfoUI,
                itemSpawnPoint,
                inventoryHighlight
            );

            SubscribeToGridEvents();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Component initialization failed: {e.Message}");
            return false;
        }
    }

    private void SubscribeToGridEvents()
    {
        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.OnItemAdded += OnItemAddedToGrid;
            mainInventoryGrid.OnItemRemoved += OnItemRemovedFromGrid;
        }
    }
    private void Start()
    {
        // PlayerStats 참조 가져오기
        if (playerStats == null && GameManager.Instance != null)
        {
            playerStats = GameManager.Instance.PlayerStats;
        }

        if (itemSpawnPoint != null)
        {
            originalSpawnPosition = itemSpawnPoint.position;
        }

        if (progressButton != null)
        {
            progressButton.onClick.AddListener(StartGame);
        }

        // 필요한 초기화 수행
        InitializeInventory();
    }

    private void Update()
    {
        if (!inventoryUI.activeSelf || mainInventoryGrid == null) return;

        // 드래그 중인 아이템 처리
        if (isDragging && selectedItem != null && isHolding)  // 원래의 상태 체크 사용
        {
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();

            // 아이템 위치 업데이트
            if (selectedItemRectTransform != null)
            {
                Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
                selectedItemRectTransform.position = liftedPosition;
            }

            // 하이라이트 및 그리드 위치 계산
            Vector2Int gridPosition = GetTileGridPosition(currentPos);
            ProcessGridPosition(gridPosition);

            // 회전 처리
            HandleItemRotation();
        }
    }
    private void ProcessGridPosition(Vector2Int gridPosition)
    {
        if (IsPositionWithinGrid(gridPosition))
        {
            bool isValidPosition = IsValidItemPlacement(gridPosition);
            inventoryHighlight?.Show(isValidPosition);

            if (isValidPosition)
            {
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(mainInventoryGrid, selectedItem, gridPosition.x, gridPosition.y);
            }
        }
        else
        {
            inventoryHighlight?.Show(false);
        }
    }
    private void HandleItemRotation()
    {
        if (!isDragging || !isHolding || selectedItem == null) return;

        // 활성화된 터치들 필터링
        var activeTouches = Touchscreen.current.touches.Where(t =>
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ||
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved ||
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary
        ).ToList();

        // 두 개 이상의 터치가 있을 때 회전 처리
        if (activeTouches.Count >= 2)
        {
            // 첫 번째 드래그 터치와 다른 터치들 분리
            var dragTouch = activeTouches[0];
            var secondaryTouches = activeTouches.Skip(1);

            foreach (var touch in secondaryTouches)
            {
                var phase = touch.phase.ReadValue();
                bool isNewTouch = phase == UnityEngine.InputSystem.TouchPhase.Began;

                if (isNewTouch)
                {
                    // 기존 회전 중인 터치와 다른 새로운 터치인 경우에만 회전 수행
                    if (touch.touchId.ReadValue() != lastRotatingTouchId)
                    {
                        selectedItem.Rotate();
                        lastRotatingTouchId = touch.touchId.ReadValue();

                        // 회전 후 그리드 포지션 업데이트
                        Vector2 currentPos = touchPosition.ReadValue<Vector2>();
                        Vector2Int gridPosition = GetTileGridPosition(currentPos);
                        UpdateHighlightAfterRotation(gridPosition);
                        break;
                    }
                }
            }
        }
        else if (activeTouches.Count <= 1)
        {
            // 터치가 하나 이하로 떨어졌을 때 회전 상태 초기화
            lastRotatingTouchId = -1;
        }
    }


    private void UpdateHighlightAfterRotation(Vector2Int gridPosition)
    {
        if (IsPositionWithinGrid(gridPosition))
        {
            bool isValidPosition = IsValidItemPlacement(gridPosition);
            inventoryHighlight?.Show(isValidPosition);
            if (isValidPosition)
            {
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(mainInventoryGrid, selectedItem, gridPosition.x, gridPosition.y);
            }
        }
        else
        {
            inventoryHighlight?.Show(false);
        }
    }
    private bool IsValidItemPlacement(Vector2Int gridPosition)
    {
        return mainInventoryGrid.CanPlaceItem(selectedItem, gridPosition.x, gridPosition.y);
    }

    private void OnEnable()
    {
        touchActions?.Enable();

        // PlayerStats 참조가 없는 경우 다시 가져오기 시도
        if (playerStats == null && GameManager.Instance != null)
        {
            playerStats = GameManager.Instance.PlayerStats;
        }
    }

    private void OnDisable()
    {  // Notice 코루틴 정리
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
            currentNoticeCoroutine = null;
        }

        // Notice UI가 활성화된 상태로 남아있지 않도록 보장
        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        touchActions?.Touch.Disable();
        SaveGridState();
    }

    private void OnDestroy()
    {
        if (isInputSystemInitialized)
        {
            touchPress.started -= OnTouchStarted;
            touchPress.canceled -= OnTouchEnded;

            touchActions.Touch.Disable();
            touchActions?.Dispose();
            isInputSystemInitialized = false;
        }
        activeItems.Clear();
        CleanupEventListeners();
    }
    #endregion

    #region Initialization

    public void InitializeInventory()
    {
        if (isInventoryInitialized) return;

        try
        {
            if (playerStats == null && GameManager.Instance != null)
            {
                playerStats = GameManager.Instance.PlayerStats;
            }

            InitializeComponents();
            InitializeInputSystem();
            InitializeGrid();
            isInventoryInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize inventory: {e.Message}");
            isInventoryInitialized = false;
        }
    }

    private void InitializeInputSystem()
    {
        if (isInputSystemInitialized) return;

        try
        {
            touchActions = new TouchActions();
            touchPosition = touchActions.Touch.Position;
            touchPress = touchActions.Touch.Press;

            touchPress.started += OnTouchStarted;
            touchPress.canceled += OnTouchEnded;

            touchActions.Enable();
            isInputSystemInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize input system: {e.Message}");
            isInputSystemInitialized = false;
        }
    }
    private void SaveGridState()
    {
        if (mainInventoryGrid == null) return;

        activeItems.Clear();
        // 그리드 순회하면서 아이템 저장
        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                InventoryItem item = mainInventoryGrid.GetItem(x, y);
                if (item != null)
                {
                    activeItems.Add(item);
                }
            }
        }
    }
    private void RestoreGridItems()
    {
        if (mainInventoryGrid == null) return;

        foreach (var item in activeItems)
        {
            if (item != null)
            {
                Vector2Int gridPos = item.GridPosition;
                if (mainInventoryGrid.IsValidPosition(gridPos))
                {
                    mainInventoryGrid.PlaceItem(item, gridPos);
                }
            }
        }
    }
    private void InitializeComponents()
    {
        inventoryHighlight = GetComponentInChildren<InventoryHighlight>();
        weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();

        itemInteractionManager = new ItemInteractionManager(
            mainInventoryGrid,
            weaponInfoUI,
            itemSpawnPoint,
            inventoryHighlight
        );

        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.OnItemAdded += OnItemAddedToGrid;
            mainInventoryGrid.OnItemRemoved += OnItemRemovedFromGrid;
        }
    }
    private void OnItemAddedToGrid(InventoryItem item)
    {
        if (GameManager.Instance.IsPlaying() && weaponManager != null && item != null)
        {
            WeaponData weaponData = item.GetWeaponData();
            if (weaponData != null)
            {
                weaponManager.EquipWeapon(weaponData);
            }
        }
    }
    private void OnItemRemovedFromGrid(InventoryItem item)
    {
        if (weaponManager != null && item != null)
        {
            WeaponData weaponData = item.GetWeaponData(); // 수정된 부분
            if (weaponData != null)
            {
                weaponManager.UnequipWeapon(weaponData);
            }
        }
    }


    private void SetupSellPopup(GameObject popup, WeaponData weaponData)
    {
        var priceText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (priceText != null)
        {
            int sellPrice = weaponData.SellPrice;
            priceText.text = $"Sell: {sellPrice}";
        }
    }
    private void SetupInputSystem()
    {
        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;
    }

    private void InitializeGrid()
    {
        if (mainInventoryGrid == null)
        {
            Debug.LogError("mainInventoryGrid is not assigned!");
            return;
        }

        itemInteractionManager.SetGrid(mainInventoryGrid);
        inventoryHighlight?.SetParent(mainInventoryGrid);
    }
    #endregion

    #region Touch Input Handling
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf) return;

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        Vector2Int gridPosition = mainInventoryGrid.GetGridPosition(touchPos);

        if (!mainInventoryGrid.IsValidPosition(gridPosition)) return;

        InventoryItem touchedItem = mainInventoryGrid.GetItem(gridPosition.x, gridPosition.y);
        if (touchedItem != null)
        {
            // 그리드 아이템 터치 시 즉시 WeaponInfoUI 업데이트
            if (weaponInfoUI != null)
            {
                WeaponData weaponData = touchedItem.GetWeaponData();
                if (weaponData != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(weaponData);

                    // 필요시 UI 표시 전환
                    if (!weaponInfoUI.gameObject.activeSelf)
                    {
                        weaponInfoUI.gameObject.SetActive(true);
                    }
                }
            }

            // 기존 홀드 체크 시작
            StartHoldCheck(touchedItem, touchPos);
        }
    }

    private void StartHoldCheck(InventoryItem item, Vector2 position)
    {
        StartCoroutine(CheckForHold(item, position));
    }

    private IEnumerator CheckForHold(InventoryItem item, Vector2 position)
    {
        float holdTime = 0f;
        Vector2 currentPos;
        float moveDistance;

        while (touchPress.IsPressed())
        {
            holdTime += Time.deltaTime;
            currentPos = touchPosition.ReadValue<Vector2>();
            moveDistance = Vector2.Distance(position, currentPos);

            if (moveDistance > HOLD_MOVE_THRESHOLD || holdTime >= HOLD_THRESHOLD)
            {
                StartDragging(item, currentPos);
                break;
            }

            yield return null;
        }
    }
    private void StartDragging(InventoryItem item, Vector2 position)
    {
        selectedItem = item;
        selectedItemRectTransform = item.GetComponent<RectTransform>();
        isDragging = true;
        isHolding = true;
        itemInteractionManager.StartDragging(item, position);
        SoundManager.Instance?.PlaySound("ItemLift_sfx", 1f, false);
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf) return;

        // 이벤트 발생 시 로그 추가 (디버깅용)
        Debug.Log("Touch ended. Checking for physics interactions...");

        Vector2 finalPosition = touchPosition.ReadValue<Vector2>();

        // 선택된 아이템이 있는지 확인
        if (selectedItem != null)
        {
            try
            {
                // 터치 위치가 그리드 밖인지 확인
                Vector2Int gridPosition = GetTileGridPosition(finalPosition);
                bool isOutsideGrid = !IsPositionWithinGrid(gridPosition) || !IsValidItemPlacement(gridPosition);

                Debug.Log($"Selected item: {selectedItem.name}, Outside grid: {isOutsideGrid}");

                // 그리드 밖이거나 해당 위치에 놓을 수 없으면 물리 아이템으로 변환
                if (isOutsideGrid && physicsManager != null)
                {
                    Debug.Log("Converting to physics item...");
                    // 물리 아이템으로 변환 (명시적으로 try-catch로 감싸기)
                    physicsManager.ConvertToPhysicsItem(selectedItem, finalPosition);
                    Debug.Log("Successfully converted to physics item");

                    // 하이라이터 숨기기
                    inventoryHighlight?.Show(false);

                    // 효과음 재생
                    SoundManager.Instance?.PlaySound("ItemDrop_sfx", 0.7f, false);
                }
                else
                {
                    // 그리드 내부라면 기존 방식으로 처리
                    Debug.Log("Using normal placement logic");
                    itemInteractionManager.EndDragging(finalPosition);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling item placement: {e.Message}\n{e.StackTrace}");

                // 에러 발생 시 안전하게 상태 초기화
                inventoryHighlight?.Show(false);
            }

            // 상태 초기화
            ResetDragState();
        }
        else
        {
            // 선택된 아이템이 없으면 기존 드래그 종료 로직만 실행
            itemInteractionManager.EndDragging(finalPosition);
            ResetDragState();
        }


    }
    private void ResetDragState()
    {
        selectedItem = null;
        selectedItemRectTransform = null;
        isDragging = false;
        isHolding = false;

    }
   

    

   

    
    #endregion

    private IEnumerator CleanupInvalidItemsDelayed()
    {
        yield return new WaitForEndOfFrame();
        CleanupInvalidItems();
    }

    private void CleanupInvalidItems()
    {
        if (mainInventoryGrid == null || !mainInventoryGrid.gameObject.activeInHierarchy)
        {
            return;
        }

        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                InventoryItem item = mainInventoryGrid.GetItem(x, y);
                if (item != null)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!mainInventoryGrid.CanPlaceItem(item, position))
                    {
                        mainInventoryGrid.RemoveItem(position);
                    }
                }
            }
        }
    }

    #region Item Management
    public void OnPurchaseItem(WeaponData weaponData)
    {
        if (!inventoryUI.activeSelf)
        {
            ToggleInventoryUI(true);
        }

        // 구매 후 그리드 초기화 및 상태 검증
        if (!isInventoryInitialized || !mainInventoryGrid.IsInitialized)
        {
            InitializeInventory();
        }

        StartCoroutine(CreatePurchasedItemDelayed(weaponData));
    }

    private IEnumerator CreatePurchasedItemDelayed(WeaponData weaponData)
    {
        yield return new WaitForEndOfFrame();
        CreateInventoryItem(weaponData);

        // 아이템 생성 후 그리드 상태 검증
        StartCoroutine(ValidateGridStateAfterPurchase());

        // 추가: 새로 구매한 무기를 선택하고 업그레이드 가능성 체크
        yield return new WaitForEndOfFrame();
        SelectNewlyPurchasedItem(weaponData);
    }
    private void SelectNewlyPurchasedItem(WeaponData weaponData)
    {
        if (weaponInfoUI == null || mainInventoryGrid == null) return;

        // 그리드 내에서 같은 종류와 같은 티어의 무기 찾기
        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                InventoryItem item = mainInventoryGrid.GetItem(x, y);
                if (item != null && item.GetWeaponData() != null)
                {
                    WeaponData itemData = item.GetWeaponData();

                    if (itemData.weaponType == weaponData.weaponType &&
                        itemData.currentTier == weaponData.currentTier &&
                        (weaponData.weaponType != WeaponType.Equipment ||
                         itemData.equipmentType == weaponData.equipmentType))
                    {
                        weaponInfoUI.UpdateWeaponInfo(itemData);
                        // 이벤트 직접 호출 대신 메서드 호출
                        mainInventoryGrid.NotifyGridChanged();
                        return;
                    }
                }
            }
        }

        // 같은 티어를 못 찾았으면 일반 검색으로 돌아감
        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                InventoryItem item = mainInventoryGrid.GetItem(x, y);
                if (item != null && item.GetWeaponData() != null)
                {
                    WeaponData itemData = item.GetWeaponData();
                    if (itemData.weaponType == weaponData.weaponType &&
                        (weaponData.weaponType != WeaponType.Equipment ||
                         itemData.equipmentType == weaponData.equipmentType))
                    {
                        weaponInfoUI.UpdateWeaponInfo(itemData);
                        // 이벤트 직접 호출 대신 메서드 호출
                        mainInventoryGrid.NotifyGridChanged();
                        return;
                    }
                }
            }
        }
    }
    private IEnumerator ValidateGridStateAfterPurchase()
    {
        yield return new WaitForEndOfFrame();
        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.ValidateGridState();
        }
    }

    private void CreateInventoryItem(WeaponData weaponData)
    {
        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();

        inventoryItem.Initialize(weaponData);

        Vector2Int? freePosition = mainInventoryGrid.FindSpaceForObject(inventoryItem);
        if (freePosition.HasValue)
        {
            PlaceItemInGrid(inventoryItem, freePosition.Value);
        }
        else
        {
            PlaceItemAtSpawnPoint(inventoryItem);
        }
    }

    private void PlaceItemInGrid(InventoryItem item, Vector2Int position)
    {
        item.transform.SetParent(mainInventoryGrid.transform, false);
        InventoryItem overlapItem = null;
        mainInventoryGrid.PlaceItem(item, position, ref overlapItem);
    }
    private void PlaceItemAtSpawnPoint(InventoryItem item)
    {
        item.transform.SetParent(canvasTransform, false);
        item.transform.position = itemSpawnPoint.position;
        item.SetGridPosition(new Vector2Int(-1, -1));
    }
    #endregion

    #region UI Management
    private void ShowNotice(string message)
    {
        if (noticeUI == null || noticeText == null) return;

        // 이전 Notice 코루틴이 실행 중이라면 중지
        if (currentNoticeCoroutine != null)
        {
            StopCoroutine(currentNoticeCoroutine);
        }

        noticeText.text = message;
        noticeUI.SetActive(true);

        // 새로운 코루틴 시작
        currentNoticeCoroutine = StartCoroutine(HideNoticeAfterDelay());
    }

    private IEnumerator HideNoticeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(noticeDisplayTime);

        if (noticeUI != null)
        {
            noticeUI.SetActive(false);
        }
        currentNoticeCoroutine = null;
    }
    public void OpenShop()
    {
        SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        ToggleShopUI(true);

        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }
    }

    public void OpenInventory()
    {
        try
        {
            SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);

            if (GameManager.Instance.currentGameState != GameState.Paused)
            {
                GameManager.Instance.SetGameState(GameState.Paused);
            }

            // UI 활성화 전에 필요한 초기화 수행
            if (!isInventoryInitialized || !mainInventoryGrid.IsInitialized)
            {
                InitializeInventory();
            }

            // UI 활성화
            ToggleInventoryUI(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in OpenInventory: {e.Message}");
        }
    }

    public void ToggleInventoryUI(bool isActive)
    {
        try
        {
            // 1. UI를 활성화하는 경우의 초기화 처리
            if (isActive)
            {
                // 인벤토리 시스템 초기화 체크
                if (!isInventoryInitialized || !mainInventoryGrid.IsInitialized)
                {
                    InitializeInventory();
                }

                // 입력 시스템 초기화 체크
                if (!isInputSystemInitialized)
                {
                    InitializeInputSystem();
                }
                if (mainInventoryGrid != null)
                {
                    // 강제 업데이트를 위한 메서드 호출
                    mainInventoryGrid.ForceUpdateGrid();

                    // 아이템 위치도 함께 업데이트
                    UpdateItemPositionsInGrid(mainInventoryGrid);
                }
            }

            // 2. UI 상태 변경
            inventoryUI.SetActive(isActive);
            playerControlUI.SetActive(!isActive);
            playerStatsUI.SetActive(!isActive);

            // 3. UI가 활성화된 경우 추가 검증 진행
            if (isActive && inventoryUI.activeSelf)
            {
                ValidateGridStateImmediate();

                // 그리드 상태가 유효한 경우에만 추가 검증 실행
                if (mainInventoryGrid != null && mainInventoryGrid.IsInitialized)
                {
                    StartCoroutine(ValidateGridStateDelayed());

                    // 추가: 인벤토리 열릴 때 WeaponInfoUI UI 초기화
                    if (weaponInfoUI != null)
                    {
                        weaponInfoUI.RefreshUpgradeUI();
                    }
                }
            }

            // 4. UI가 비활성화되는 경우 정리 작업
            if (!isActive)
            {
                SaveGridState();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ToggleInventoryUI: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    private void UpdateItemPositionsInGrid(ItemGrid grid)
    {
        if (grid == null) return;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                InventoryItem item = grid.GetItem(x, y);
                if (item != null)
                {
                    Vector2Int gridPos = item.GridPosition;
                    RectTransform rt = item.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        // 아이템 위치 다시 계산하여 설정
                        rt.localPosition = grid.CalculatePositionOnGrid(item, gridPos.x, gridPos.y);
                    }
                }
            }
        }
    }
    private void ValidateGridStateImmediate()
    {
        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.ValidateGridState();
        }
    }
    private IEnumerator ValidateGridStateDelayed()
    {
        yield return new WaitForEndOfFrame();

        if (mainInventoryGrid != null && inventoryUI.activeSelf)
        {
            mainInventoryGrid.ValidateGridState();
        }
    }

    public void ToggleShopUI(bool isActive)
    {
        shopUI.SetActive(isActive);
        playerControlUI.SetActive(!isActive);
        playerStatsUI.SetActive(!isActive);
    }

    #endregion

    #region Game Flow

    public System.Action OnProgressButtonClicked;

    public void OnProgressButtonClick()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (!ValidateGameStart())
        {
            string message = "Please equip at least one weapon before starting!";
            ShowNotice(message);
            return;
        }

        PlayButtonSound();

        // 물리 아이템 처리 - 판매 로직 추가
        if (physicsManager != null)
        {
            SellAllPhysicsItems();
        }

        // 먼저 인벤토리 UI를 비활성화
        inventoryUI.SetActive(false);

        // 그 다음 트랜지션 이펙트 실행
        if (transitionEffect != null)
        {
            // 바깥에서 안으로 효과 (reverseEffect = false)
            transitionEffect.reverseEffect = false;
            transitionEffect.gameObject.SetActive(true);
            transitionEffect.PlayTransition(() => {
                // 트랜지션이 완료된 후 게임 시작
                CompleteGameStart();
            });
        }
        else
        {
            // 트랜지션 이펙트가 없으면 바로 게임 시작
            CompleteGameStart();
        }
    }

    private void SellAllPhysicsItems()
    {
        if (physicsManager == null) return;

        // PlayerStats 참조 확인 및 얻기
        if (playerStats == null && GameManager.Instance != null)
        {
            playerStats = GameManager.Instance.PlayerStats;
        }

        // PlayerStats가 없으면 판매 불가
        if (playerStats == null)
        {
            Debug.LogError("Cannot sell items: PlayerStats reference is missing");
            return;
        }

        try
        {
            List<PhysicsInventoryItem> itemsToSell = new List<PhysicsInventoryItem>();

            // 판매할 아이템 목록 수집
            foreach (var physicsItem in physicsManager.GetAllPhysicsItems())
            {
                if (physicsItem != null && !physicsItem.IsBeingDragged)
                {
                    itemsToSell.Add(physicsItem);
                }
            }

            if (itemsToSell.Count == 0) return;

            // 아이템 판매 처리
            int totalCoins = 0;

            foreach (var item in itemsToSell)
            {
                InventoryItem inventoryItem = item.GetComponent<InventoryItem>();
                if (inventoryItem != null)
                {
                    WeaponData weaponData = inventoryItem.GetWeaponData();
                    if (weaponData != null)
                    {
                        totalCoins += weaponData.SellPrice;
                    }
                }

                // 물리 아이템 제거
                physicsManager.RemovePhysicsItem(item);
            }

            // 코인 지급
            if (totalCoins > 0)
            {
                playerStats.AddCoins(totalCoins);

                // 판매 효과음
                SoundManager.Instance?.PlaySound("Coin_sfx", 1f, false);

                // 판매 알림 표시
                ShowNotice($"Sold all items for {totalCoins} coins!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error selling physics items: {e.Message}");
        }
    }
    private void CompleteGameStart()
    {
        EquipAllGridItems();

        // 웨이브 매니저에 진행 알림
        OnProgressButtonClicked?.Invoke();

        TransitionToGameplay();
    }
    private bool ValidateGameStart()
    {
        if (!isInventoryInitialized)
        {
            InitializeInventory();
        }

        if (mainInventoryGrid == null)
        {
            Debug.LogError("Main Inventory Grid is null");
            return false;
        }

        if (!mainInventoryGrid.IsInitialized)
        {
            mainInventoryGrid.ForceInitialize();
        }

        // 그리드 상태 검증 후 아이템 존재 여부 확인
        return HasAnyItemInGrid();
    }

    private void EquipAllGridItems()
    {
        if (mainInventoryGrid == null || weaponManager == null) return;

        try
        {
            for (int x = 0; x < mainInventoryGrid.Width; x++)
            {
                for (int y = 0; y < mainInventoryGrid.Height; y++)
                {
                    InventoryItem item = mainInventoryGrid.GetItem(x, y);
                    if (item != null)
                    {
                        WeaponData weaponData = item.GetWeaponData();
                        if (weaponData != null)
                        {
                            weaponManager.EquipWeapon(weaponData);
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error equipping items: {e.Message}");
        }
    }


    public void CreateUpgradedItem(WeaponData weaponData, Vector2Int position)
    {
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected!");
            return;
        }

        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        RectTransform rectTransform = itemObj.GetComponent<RectTransform>();

        // UI 계층 설정
        rectTransform.SetParent(selectedItemGrid.GetComponent<RectTransform>(), false);

        // 아이템 초기화
        inventoryItem.Initialize(weaponData);

        // 지정된 위치에 배치 시도
        TryPlaceUpgradedItem(inventoryItem, position);
    }

    /// <summary>
    /// 업그레이드된 아이템을 그리드에 배치 시도합니다.
    /// </summary>
    private void TryPlaceUpgradedItem(InventoryItem item, Vector2Int targetPosition)
    {
        InventoryItem overlapItem = null;

        // 먼저 지정된 위치에 배치 시도
        bool placed = selectedItemGrid.PlaceItem(item, targetPosition, ref overlapItem);

        if (!placed)
        {
            // 실패시 다른 빈 공간 찾기
            Vector2Int? freePosition = selectedItemGrid.FindSpaceForObject(item);

            if (freePosition.HasValue)
            {
                selectedItemGrid.PlaceItem(item, freePosition.Value, ref overlapItem);
                Vector2 uiPosition = selectedItemGrid.CalculatePositionOnGrid(item, freePosition.Value.x, freePosition.Value.y);
                item.GetComponent<RectTransform>().localPosition = uiPosition;
            }
            else
            {
                // 빈 공간이 없는 경우 스폰 포인트에 배치
                if (itemSpawnPoint != null)
                {
                    item.transform.SetParent(canvasTransform, false);
                    item.transform.position = itemSpawnPoint.position;
                    item.SetGridPosition(new Vector2Int(-1, -1));
                }
                else
                {
                    Debug.LogWarning("ItemSpawnPoint not found! Using default position.");
                    item.GetComponent<RectTransform>().localPosition = Vector2.zero;
                }
            }
        }
    }

    private void TransitionToGameplay()
    {
        // UI 전환 전에 모든 상태 초기화
        inventoryHighlight?.Show(false);
        itemInteractionManager?.ClearSelection();

        // UI 상태 전환
        inventoryUI.SetActive(false);
        shopUI.SetActive(false);
        playerControlUI.SetActive(true);
        playerStatsUI.SetActive(true);

        // 게임 상태를 Playing으로 변경
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
        }
    }

    #endregion

    #region Helper Methods
    private Vector2Int GetTileGridPosition(Vector2 position)
    {
        // 추가 로직 없이 그대로 계산
        return mainInventoryGrid.GetGridPosition(position);
    }


    private bool IsPositionWithinGrid(Vector2Int position)
    {
        return mainInventoryGrid.IsValidPosition(position);
    }


    private bool HasAnyItemInGrid()
    {
        if (mainInventoryGrid == null) return false;

        bool hasItems = false;
        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                var item = mainInventoryGrid.GetItem(x, y);
                if (item != null)
                {
                    hasItems = true;
                }
            }
        }

        return hasItems;
    }


    private void PlayLiftSound()
    {
        SoundManager.Instance?.PlaySound("ItemLift_sfx", 1f, false);
    }

    private void PlayButtonSound()
    {
        SoundManager.Instance?.PlaySound("Button_sfx", 1f, false);
    }

    private void CleanupEventListeners()
    {
        if (progressButton != null)
        {
            progressButton.onClick.RemoveListener(StartGame);
        }

        if (touchPress != null)
        {
            touchPress.started -= OnTouchStarted;
            touchPress.canceled -= OnTouchEnded;
        }

        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.OnItemAdded -= OnItemAddedToGrid;
            mainInventoryGrid.OnItemRemoved -= OnItemRemovedFromGrid;
        }

        touchActions?.Dispose();
    }
    #endregion
}