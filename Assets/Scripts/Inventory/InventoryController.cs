using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Linq;

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
 
    #endregion

    #region Private Fields
    private ItemInteractionManager itemInteractionManager;
    private WeaponManager weaponManager;
    private InventoryHighlight inventoryHighlight;
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;   
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
        InitializeComponents();
        SetupInputSystem();
        InitializeGrid();
    }

    private void Start()
    {
        if (itemSpawnPoint != null)
        {
            originalSpawnPosition = itemSpawnPoint.position;
        }

        if (progressButton != null)
        {
            progressButton.onClick.AddListener(StartGame);
        }
    }

    private void Update()
    {
        if (!inventoryUI.activeSelf || mainInventoryGrid == null) return;

        // 드래그 중인 아이템에 대한 처리
        if (isDragging && selectedItem != null && isHolding)
        {
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            HandleItemRotation();
            // 아이템 위치 업데이트
            if (selectedItemRectTransform != null)
            {
                // ITEM_LIFT_OFFSET을 적용하여 들어올린 효과 구현
                Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
                selectedItemRectTransform.position = liftedPosition;
            }

            // 하이라이트 및 그리드 위치 계산
            Vector2Int gridPosition = GetTileGridPosition(currentPos);

            if (IsPositionWithinGrid(gridPosition))
            {
                bool isValidPosition = IsValidItemPlacement(gridPosition);

                if (isValidPosition)
                {
                    inventoryHighlight?.Show(true);
                    inventoryHighlight?.SetSize(selectedItem);
                    inventoryHighlight?.SetPosition(mainInventoryGrid, selectedItem, gridPosition.x, gridPosition.y);
                }
                else
                {
                    inventoryHighlight?.Show(false);
                }
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
    }

    private void HandleItemRotation()
    {
        if (!isDragging || !isHolding || selectedItem == null) return;

        // 현재 활성화된 터치들을 확인
        var activeTouches = Touchscreen.current.touches.Where(t =>
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ||
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved ||
            t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary
        ).ToList();

        // 터치가 1개일 때 쿨다운 초기화 (드래그 중인 터치)
        if (activeTouches.Count == 1)
        {
            lastRotationTime = Time.time - ROTATION_COOLDOWN;
        }

        // 추가 터치가 발생했을 때 회전
        if (activeTouches.Count >= 2)
        {
            foreach (var touch in activeTouches.Skip(1)) // 첫 번째 터치(드래그)를 제외한 터치들 확인
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began && IsRotationAllowed())
                {
                    PerformItemRotation();
                    break;
                }
            }
        }
    }
    private bool IsRotationAllowed()
    {
        float currentTime = Time.time;
        return currentTime - lastRotationTime >= ROTATION_COOLDOWN;
    }

    private void PerformItemRotation()
    {
        // 아이템 회전
        selectedItem.Rotate();

        // 회전 시간 갱신
        lastRotationTime = Time.time;

        // 현재 터치 위치 기준 그리드 포지션 계산
        Vector2 currentPos = touchPosition.ReadValue<Vector2>();
        Vector2Int gridPosition = GetTileGridPosition(currentPos);

        // 하이라이트 업데이트
        UpdateHighlightAfterRotation(gridPosition);
    }

    private void UpdateHighlightAfterRotation(Vector2Int gridPosition)
    {
        if (IsPositionWithinGrid(gridPosition))
        {
            bool isValidPosition = IsValidItemPlacement(gridPosition);

            if (isValidPosition)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(mainInventoryGrid, selectedItem, gridPosition.x, gridPosition.y);
            }
            else
            {
                inventoryHighlight?.Show(false);
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
    }

    private void OnDisable()
    {
        touchActions?.Disable();
    }

    private void OnDestroy()
    {
        CleanupEventListeners();
    }
    #endregion

    #region Initialization
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
        Vector2Int gridPosition = GetTileGridPosition(touchPos);

        if (IsPositionWithinGrid(gridPosition))
        {
            InventoryItem touchedItem = mainInventoryGrid.GetItem(gridPosition.x, gridPosition.y);
            if (touchedItem != null)
            {
                StartHoldCheck(touchedItem, touchPos);
            }
        }
    }

    private void StartHoldCheck(InventoryItem item, Vector2 position)
    {
        StartCoroutine(CheckForHold(item, position));
    }

    private IEnumerator CheckForHold(InventoryItem item, Vector2 position)
    {
        float holdTime = 0f;
        bool holdComplete = false;

        while (!holdComplete && touchPress.IsPressed())
        {
            holdTime += Time.deltaTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float moveDistance = Vector2.Distance(position, currentPos);

            if (moveDistance > HOLD_MOVE_THRESHOLD || holdTime >= HOLD_THRESHOLD)
            {
                selectedItem = item;
                selectedItemRectTransform = item.GetComponent<RectTransform>();
                isDragging = true;
                isHolding = true;

                itemInteractionManager.StartDragging(item, currentPos);
                holdComplete = true;
                PlayLiftSound();
            }

            yield return null;
        }
    }
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf) return;

        Vector2 finalPosition = touchPosition.ReadValue<Vector2>();

        itemInteractionManager.EndDragging(finalPosition);    
        // 상태 초기화
        ResetDragState();
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

        StartCoroutine(CreatePurchasedItemDelayed(weaponData));
    }

    private IEnumerator CreatePurchasedItemDelayed(WeaponData weaponData)
    {
        yield return new WaitForEndOfFrame();
        CreateInventoryItem(weaponData);
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
        SoundManager.Instance.PlaySound("Button_sfx", 1f, false);
        ToggleInventoryUI(true);
        StartCoroutine(CleanupInvalidItemsDelayed());

        if (GameManager.Instance.currentGameState != GameState.Paused)
        {
            GameManager.Instance.SetGameState(GameState.Paused);
        }
    }
    public void ToggleInventoryUI(bool isActive)
    {
        inventoryUI.SetActive(isActive);
        playerControlUI.SetActive(!isActive);
        playerStatsUI.SetActive(!isActive);
    }
    public void ToggleShopUI(bool isActive)
    {
        shopUI.SetActive(isActive);
        playerControlUI.SetActive(!isActive);
        playerStatsUI.SetActive(!isActive);
    }

    #endregion

    #region Game Flow
    public void StartGame()
    {
        // 게임 시작 전 유효성 검사
        if (!ValidateGameStart())
        {
            Debug.LogError("Failed to start game: Validation failed!");
            return;
        }

        PlayButtonSound();
        EquipAllGridItems();
        TransitionToGameplay();
    }
    private bool ValidateGameStart()
    {
        // 초기화 및 유효성 검사
        if (mainInventoryGrid == null || !mainInventoryGrid.IsInitialized)
        {
            Debug.LogError("Main Inventory Grid is not initialized!");
            return false;
        }

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

        for (int x = 0; x < mainInventoryGrid.Width; x++)
        {
            for (int y = 0; y < mainInventoryGrid.Height; y++)
            {
                if (mainInventoryGrid.GetItem(x, y) != null)
                {
                    return true;
                }
            }
        }
        return false;
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