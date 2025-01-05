using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro;

public class InventoryController : MonoBehaviour
{
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private InputAction touchDelta;

    [HideInInspector]
    private ItemGrid selectedItemGrid;
    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        }
    }
    WeaponManager weaponManager;
    InventoryItem selectedItem;
    InventoryItem overlapItem;

    RectTransform rectTransform;

    //[SerializeField] List<WeaponData> weapons;
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform canvasTransform;
    [SerializeField] private WeaponInfoUI weaponInfoUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private Button progressButton;

    [Header("Sell Zone")]
    [SerializeField] private RectTransform sellZoneRect;
    [SerializeField] private GameObject sellPopupPrefab;
    private GameObject currentSellPopup;
    private bool isOverSellZone = false;

    InventoryHighlight inventoryHighlight;
    private bool isDragging = false;
    private Vector2 originalSpawnPosition;

    public bool isHolding = false;
    private float holdStartTime;
    private Vector2 holdStartPosition;
    private const float HOLD_THRESHOLD = 0.3f;
    private const float HOLD_MOVE_THRESHOLD = 20f; 
    public static float ITEM_LIFT_OFFSET = 350f; 
    private float lastRotationTime = -999f;
    private const float ROTATION_COOLDOWN = 0.3f; 


    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
        if (inventoryHighlight == null)
        {
            Debug.LogError("InventoryHighlight component not found!");
        }

        weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();

        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;
        touchDelta = touchActions.Touch.Delta;

        if (mainInventoryGrid != null)
        {
            mainInventoryGrid.OnItemAdded += OnItemAddedToGrid;
        }

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;

        if (itemSpawnPoint != null)
        {
            originalSpawnPosition = itemSpawnPoint.position;
        }

        InitializeGrid();
        ToggleInventoryUI(false);
    }
    private void InitializeGrid()
    {
        if (mainInventoryGrid == null)
        {
            Debug.LogError("mainInventoryGrid is not assigned in inspector!");
            return;
        }

        selectedItemGrid = mainInventoryGrid;
        if (inventoryHighlight != null)
        {
            inventoryHighlight.SetParent(selectedItemGrid);
        }
    }
    private void OnEnable()
    {
        touchActions.Enable();
    }
    private void OnDisable()
    {
        touchActions.Disable();
    }
    public void ToggleInventoryUI(bool isActive)
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isActive);

            if (isActive && selectedItemGrid == null)
            {
                InitializeGrid();
            }

            
            if (isActive)
            {
                StartCoroutine(CleanupInvalidItemsDelayed());
            }
        }

        if (playerControlUI != null && playerStatsUI != null)
        {
            playerControlUI.SetActive(!isActive);
            playerStatsUI.SetActive(!isActive);
        }

        if (!isActive)
        {
            if (selectedItem != null)
            {
                Destroy(selectedItem.gameObject);
                selectedItem = null;
            }
            isDragging = false;
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }
        }
    }
    private IEnumerator CleanupInvalidItemsDelayed()
    {
        yield return new WaitForEndOfFrame();
        CleanupInvalidItems();
    }
    public void StartGame()
    {
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected! Cannot start game.");
            return;
        }

        bool hasAnyItem = false;

        // 그리드의 모든 아이템을 확인
        for (int x = 0; x < selectedItemGrid.Width; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height; y++)
            {
                InventoryItem item = selectedItemGrid.GetItem(x, y);
                if (item != null)
                {
                    hasAnyItem = true;
                    // 각 아이템을 무기로 장착
                    OnWeaponEquipped(item);
                }
            }
        }

        if (hasAnyItem)
        {
            // UI 전환
            inventoryHighlight?.Show(false);
            inventoryUI?.SetActive(false);
            playerControlUI?.SetActive(true);
            playerStatsUI?.SetActive(true);

            // 게임 상태 변경
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }
        else
        {
            Debug.LogWarning("No item equipped! Please place a weapon in the grid.");
        }
    }
    private void OnWeaponEquipped(InventoryItem item)
    {
        if (item != null && item.weaponData != null && weaponManager != null)
        {
            Debug.Log($"Equipping weapon: {item.weaponData.weaponName}");
            weaponManager.EquipWeapon(item.weaponData);
        }
    }
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        Vector2Int gridPosition = GetTileGridPosition(touchPos);

        // 이전 선택 상태 초기화
        if (selectedItem != null)
        {
            // 이전에 선택된 아이템이 있다면 원래 위치로 되돌림
            if (selectedItem.onGridPositionX >= 0 && selectedItem.onGridPositionY >= 0)
            {
                Vector2Int originalPosition = new Vector2Int(selectedItem.onGridPositionX, selectedItem.onGridPositionY);
                selectedItemGrid.PlaceItem(selectedItem, originalPosition.x, originalPosition.y, ref overlapItem);
                Vector2 position = selectedItemGrid.CalculatePositionOnGrid(selectedItem, originalPosition.x, originalPosition.y);
                selectedItem.GetComponent<RectTransform>().localPosition = position;
            }

            selectedItem = null;
            isDragging = false;
            isHolding = false;
        }

        // 그리드 내 위치 확인
        if (IsPositionWithinGrid(gridPosition))
        {
            InventoryItem touchedItem = selectedItemGrid.GetItem(gridPosition.x, gridPosition.y);

            if (touchedItem != null)
            {
                // WeaponInfo UI 업데이트
                HandleItemSelection(touchedItem);

                // 선택된 아이템의 RectTransform 확인
                RectTransform itemRect = touchedItem.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    // 터치 위치가 아이템 내부인지 확인
                    if (RectTransformUtility.RectangleContainsScreenPoint(itemRect, touchPos))
                    {
                        // 홀드 체크 시작을 위한 상태 설정
                        holdStartTime = Time.time;
                        holdStartPosition = touchPos;
                        StartCoroutine(CheckForHold());
                    }
                }
            }
        }
        else
        {
            // 그리드 외부 터치 처리
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }
        }
    }
    private void HandleItemSelection(InventoryItem item)
    {
        if (item == null) return;

        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(item.weaponData);
        }

        // 하이라이트 업데이트
        if (inventoryHighlight != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(item);
            inventoryHighlight.SetPosition(selectedItemGrid, item);
        }
    }
    private IEnumerator CheckForHold()
    {
        float startTime = Time.realtimeSinceStartup;
        bool holdChecking = true;
        Vector2 rawTouchPos = holdStartPosition;
        InventoryItem touchedItem = null;

        // 현재 터치한 위치의 그리드 좌표 계산
        Vector2Int initialGridPosition = GetTileGridPosition(holdStartPosition);

        // 그리드 내의 아이템 확인
        if (IsPositionWithinGrid(initialGridPosition))
        {
            touchedItem = selectedItemGrid.GetItem(initialGridPosition.x, initialGridPosition.y);
            if (touchedItem != null)
            {
                Debug.Log($"Found item at grid position: ({initialGridPosition.x}, {initialGridPosition.y})");
            }
        }

        // 터치한 아이템이 없다면 종료
        if (touchedItem == null)
        {
            Debug.Log("No item found at touch position");
            yield break;
        }

        while (holdChecking && touchPress.IsPressed())
        {
            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - startTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float moveDistance = Vector2.Distance(holdStartPosition, currentPos);

            // 드래그 시작 조건 체크
            if (moveDistance > HOLD_MOVE_THRESHOLD)
            {
                Debug.Log("Move threshold reached - starting drag");
                StartItemInteraction(touchedItem, currentPos);
                holdChecking = false;
                break;
            }

            // 홀드 시간 조건 체크
            if (elapsedTime >= HOLD_THRESHOLD)
            {
                Debug.Log("Hold threshold reached - picking up item");
                StartItemInteraction(touchedItem, currentPos);
                holdChecking = false;
                break;
            }

            yield return new WaitForEndOfFrame();
        }
    }
    private void StartItemInteraction(InventoryItem item, Vector2 currentPos)
    {
        if (item == null) return;

        // 이전 선택 상태 정리
        if (selectedItem != null && selectedItem != item)
        {
            ClearPreviousSelection();
        }

        // 새 아이템 선택 및 상태 설정
        selectedItem = item;
        rectTransform = selectedItem.GetComponent<RectTransform>();
        isDragging = true;
        isHolding = true;

        // 그리드에서 아이템 제거
        if (item.onGridPositionX >= 0 && item.onGridPositionY >= 0)
        {
            selectedItemGrid.PickUpItem(item.onGridPositionX, item.onGridPositionY);
        }

        // 아이템 위치 업데이트
        Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
        rectTransform.position = liftedPosition;

        // 무기 정보 UI 업데이트
        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
        }
    }
    private void ClearPreviousSelection()
    {
        if (selectedItem != null)
        {
            // 이전 선택된 아이템이 유효한 그리드 위치를 가지고 있다면
            if (selectedItem.onGridPositionX >= 0 && selectedItem.onGridPositionY >= 0)
            {
                // 원래 위치로 되돌림
                Vector2Int originalPosition = new Vector2Int(selectedItem.onGridPositionX, selectedItem.onGridPositionY);
                selectedItemGrid.PlaceItem(selectedItem, originalPosition.x, originalPosition.y, ref overlapItem);
                Vector2 position = selectedItemGrid.CalculatePositionOnGrid(selectedItem, originalPosition.x, originalPosition.y);
                selectedItem.GetComponent<RectTransform>().localPosition = position;
            }
        }

        // 상태 초기화
        selectedItem = null;
        isDragging = false;
        isHolding = false;
        rectTransform = null;
    }
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        HandleTouchEnd();

        if (isDragging && selectedItem != null)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();
            Vector2Int tileGridPosition = GetTileGridPosition(touchPos);

            if (IsPositionWithinGrid(tileGridPosition) &&
                selectedItemGrid.BoundryCheck(tileGridPosition.x, tileGridPosition.y,
                    selectedItem.WIDTH, selectedItem.HEIGHT))
            {
                PutDownItem(tileGridPosition);
            }
            else
            {
                isHolding = false;  
            }
        }
        else
        {
            
            isHolding = false;
            isDragging = false;
        }
    }
    private void Update()
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        if (isDragging && selectedItem != null && isHolding)
        {
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            currentPos += Vector2.up * ITEM_LIFT_OFFSET;
            rectTransform.position = currentPos;

            var activeTouches = Touchscreen.current.touches.Where(t =>
                t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ||
                t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved ||
                t.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary
            ).ToList();

            Debug.Log($"Active touches count: {activeTouches.Count}");

            
            if (activeTouches.Count == 1)
            {
                lastRotationTime = Time.time - ROTATION_COOLDOWN;
            }

            if (activeTouches.Count >= 2)
            {
                var primaryTouch = activeTouches[0];
                var secondaryTouches = activeTouches.Skip(1);

                foreach (var touch in secondaryTouches)
                {
                    Debug.Log($"Secondary touch phase: {touch.phase.ReadValue()}");

                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        float currentTime = Time.time;
                        if (currentTime - lastRotationTime >= ROTATION_COOLDOWN)
                        {
                            Debug.Log("Rotating item");
                            RotateItem();
                            lastRotationTime = currentTime;
                            break;
                        }
                        else
                        {
                            Debug.Log($"Cooldown not passed. Remaining time: {ROTATION_COOLDOWN - (currentTime - lastRotationTime)}");
                        }
                    }
                }
            }

            isOverSellZone = IsTouchOverSellZone(currentPos);
            UpdateSellPopup(currentPos);
        }

        HandleHighlight();
    }
    private void HandleHighlight()
    {
        if (selectedItemGrid == null || !inventoryUI.activeSelf)
        {
            inventoryHighlight?.Show(false);
            return;
        }

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        Vector2Int positionOnGrid = GetTileGridPosition(touchPos);

        Debug.Log($"HandleHighlight - Touch Position: {touchPos}, Grid Position: {positionOnGrid}");

        if (!IsPositionWithinGrid(positionOnGrid))
        {
            Debug.Log($"Position {positionOnGrid} is not within grid bounds: {selectedItemGrid.Width}x{selectedItemGrid.Height}");
            inventoryHighlight?.Show(false);
            return;
        }

        if (selectedItem == null)
        {
            InventoryItem itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            Debug.Log($"Attempting to highlight item at ({positionOnGrid.x}, {positionOnGrid.y}): {(itemToHighlight != null ? "Found" : "Not found")}");

            if (itemToHighlight != null)
            {
                Debug.Log($"Highlighting item at original position: ({itemToHighlight.onGridPositionX}, {itemToHighlight.onGridPositionY})");
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(itemToHighlight);
                inventoryHighlight?.SetPosition(selectedItemGrid, itemToHighlight,
                    itemToHighlight.onGridPositionX, itemToHighlight.onGridPositionY);
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
        else
        {
            bool isValidPosition = selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT);

            Debug.Log($"Selected item position check: {isValidPosition} at ({positionOnGrid.x}, {positionOnGrid.y})");

            if (isValidPosition)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
    }
    private void RotateItem()
    {
        if (selectedItem == null) return;

        Vector2Int currentGridPos = GetTileGridPosition(rectTransform.position);

        selectedItem.Rotate();

        if (inventoryHighlight != null)
        {
            inventoryHighlight.SetSize(selectedItem);

            
            if (IsPositionWithinGrid(currentGridPos) &&
                selectedItemGrid.BoundryCheck(currentGridPos.x, currentGridPos.y,
                    selectedItem.WIDTH, selectedItem.HEIGHT))
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, currentGridPos.x, currentGridPos.y);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
    }

    private Vector2Int GetTileGridPosition(Vector2 position)
    {
        
        if (selectedItem != null && isHolding)
        {
            position += Vector2.up * ITEM_LIFT_OFFSET;
        }
        return selectedItemGrid.GetTileGridPosition(position);
    }

    private InventoryItem itemToHighlight;
    Vector2Int oldPosition;

    private void OnDestroy()
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
        }

        touchActions?.Dispose();
    }
    public InventoryItem GetInventoryItemPrefab()
    {
        return weaponPrefab.GetComponent<InventoryItem>();
    }

    public void CreatePurchasedItem(WeaponData weaponData)
    {
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected!");
            return;
        }

        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        RectTransform rectTransform = itemObj.GetComponent<RectTransform>();

        rectTransform.SetParent(selectedItemGrid.GetComponent<RectTransform>(), false);
        inventoryItem.Set(weaponData);

        // 빈 공간 찾기
        Vector2Int? freePosition = selectedItemGrid.FindSpaceForObject(inventoryItem);

        if (freePosition.HasValue)
        {
            // 그리드에 빈 공간이 있는 경우
            Vector2Int gridPosition = freePosition.Value;
            InventoryItem overlapItem = null;
            selectedItemGrid.PlaceItem(inventoryItem, gridPosition.x, gridPosition.y, ref overlapItem);

            // UI 위치 설정
            Vector2 position = selectedItemGrid.CalculatePositionOnGrid(inventoryItem, gridPosition.x, gridPosition.y);
            rectTransform.localPosition = position;
        }
        else
        {
            // 그리드에 빈 공간이 없는 경우
            if (itemSpawnPoint != null)
            {
                rectTransform.SetParent(canvasTransform, false); // 캔버스의 직계 자식으로 변경
                rectTransform.position = itemSpawnPoint.position;

                // 아이템이 그리드에 속하지 않음을 표시
                inventoryItem.onGridPositionX = -1;
                inventoryItem.onGridPositionY = -1;
            }
            else
            {
                Debug.LogWarning("ItemSpawn point not set! Using default position.");
                rectTransform.localPosition = Vector2.zero;
            }
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

        rectTransform.SetParent(selectedItemGrid.GetComponent<RectTransform>(), false);
        inventoryItem.Set(weaponData);

        // 우선 원래 위치에 배치 시도
        InventoryItem overlapItem = null;
        bool placed = selectedItemGrid.PlaceItem(inventoryItem, position.x, position.y, ref overlapItem);

        if (!placed)
        {
            // 원래 위치에 배치 실패시 새로운 위치 찾기
            Vector2Int? freePosition = selectedItemGrid.FindSpaceForObject(inventoryItem);

            if (freePosition.HasValue)
            {
                // 새로운 빈 공간을 찾은 경우
                selectedItemGrid.PlaceItem(inventoryItem, freePosition.Value.x, freePosition.Value.y, ref overlapItem);
                Vector2 uiPosition = selectedItemGrid.CalculatePositionOnGrid(inventoryItem, freePosition.Value.x, freePosition.Value.y);
                rectTransform.localPosition = uiPosition;
            }
            else
            {
                // 빈 공간이 없는 경우
                if (itemSpawnPoint != null)
                {
                    rectTransform.SetParent(canvasTransform, false); // 캔버스의 직계 자식으로 변경
                    rectTransform.position = itemSpawnPoint.position;

                    // 아이템이 그리드에 속하지 않음을 표시
                    inventoryItem.onGridPositionX = -1;
                    inventoryItem.onGridPositionY = -1;
                }
                else
                {
                    Debug.LogWarning("ItemSpawn point not set! Using default position.");
                    rectTransform.localPosition = Vector2.zero;
                }
            }
        }
        else
        {
            // 원래 위치에 배치 성공
            Vector2 uiPosition = selectedItemGrid.CalculatePositionOnGrid(inventoryItem, position.x, position.y);
            rectTransform.localPosition = uiPosition;
        }
    }
    //private void PickUpItem(Vector2Int tileGridPosition)
    //{
    //    if (selectedItem != null)
    //    {
    //        // 이미 선택된 아이템이 있다면 원래 위치로 돌려놓기
    //        Vector2Int originalPosition = new Vector2Int(selectedItem.onGridPositionX, selectedItem.onGridPositionY);
    //        selectedItemGrid.PlaceItem(selectedItem, originalPosition.x, originalPosition.y, ref overlapItem);
    //        Vector2 position = selectedItemGrid.CalculatePositionOnGrid(selectedItem, originalPosition.x, originalPosition.y);
    //        selectedItem.GetComponent<RectTransform>().localPosition = position;
    //        selectedItem = null;
    //        isDragging = false;
    //        isHolding = false;
    //    }

    //    InventoryItem itemToPickup = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);
    //    if (itemToPickup != null)
    //    {
    //        selectedItem = selectedItemGrid.PickUpItem(itemToPickup.onGridPositionX, itemToPickup.onGridPositionY);
    //        if (selectedItem != null)
    //        {
    //            rectTransform = selectedItem.GetComponent<RectTransform>();
    //            isDragging = true;
    //            isHolding = true;

    //            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
    //            Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
    //            rectTransform.position = liftedPosition;

    //            if (weaponInfoUI != null)
    //            {
    //                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
    //            }
    //        }
    //    }
    //}
    private void PutDownItem(Vector2Int tileGridPosition)
    {
        if (selectedItem == null) return;

        // 현재 선택된 아이템과 그 위치 저장
        InventoryItem itemToPlace = selectedItem;
        Vector2Int originalPosition = new Vector2Int(itemToPlace.onGridPositionX, itemToPlace.onGridPositionY);

        // 먼저 선택 상태와 드래그 상태를 초기화
        selectedItem = null;
        isDragging = false;
        isHolding = false;

        // 아이템 배치 시도
        bool complete = selectedItemGrid.PlaceItem(itemToPlace, tileGridPosition.x, tileGridPosition.y, ref overlapItem);

        if (complete)
        {
            if (overlapItem != null)
            {
                // 겹치는 아이템이 있는 경우, 원래 위치로 되돌림
                selectedItemGrid.PlaceItem(itemToPlace, originalPosition.x, originalPosition.y, ref overlapItem);
                Vector2 position = selectedItemGrid.CalculatePositionOnGrid(itemToPlace, originalPosition.x, originalPosition.y);
                itemToPlace.GetComponent<RectTransform>().localPosition = position;
            }

            // 하이라이트 업데이트
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToPlace);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToPlace);
            }
        }
        else
        {
            // 배치 실패 시 원래 위치로 되돌림
            selectedItemGrid.PlaceItem(itemToPlace, originalPosition.x, originalPosition.y, ref overlapItem);
            Vector2 position = selectedItemGrid.CalculatePositionOnGrid(itemToPlace, originalPosition.x, originalPosition.y);
            itemToPlace.GetComponent<RectTransform>().localPosition = position;

            // 실패 시에도 하이라이트 업데이트
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToPlace);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToPlace, originalPosition.x, originalPosition.y);
            }
        }

        // 무기 정보 UI 업데이트 - 마지막으로 상호작용한 아이템 정보를 표시
        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(itemToPlace.weaponData);
        }

        // 이벤트 통지가 필요한 경우 여기서 처리
         selectedItemGrid.NotifyGridChanged(); // 필요한 경우 주석 해제
    }
    private void CleanupInvalidItems()
    {
        if (selectedItemGrid == null || !selectedItemGrid.gameObject.activeInHierarchy)
        {
            return;
        }

        for (int x = 0; x < selectedItemGrid.Width; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height; y++)
            {
                InventoryItem item = selectedItemGrid.GetItem(x, y);
                if (item != null)
                {
                    if (!selectedItemGrid.BoundryCheck(x, y, item.WIDTH, item.HEIGHT))
                    {
                        selectedItemGrid.PickUpItem(x, y);
                        if (item.gameObject != null)
                        {
                            selectedItem = item;
                            rectTransform = item.GetComponent<RectTransform>();

                        }
                    }
                }
            }
        }
    }

    private bool IsPositionWithinGrid(Vector2Int position)
    {
        return position.x >= 0 && position.x < selectedItemGrid.Width &&
               position.y >= 0 && position.y < selectedItemGrid.Height;
    }

    public void OnPurchaseItem(WeaponData weaponData)
    {
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid available for item placement!");
            InitializeGrid();
            if (selectedItemGrid == null)
            {
                return;
            }
        }

        
        inventoryUI.SetActive(true);
        if (playerControlUI != null && playerStatsUI != null)
        {
            playerControlUI.SetActive(false);
            playerStatsUI.SetActive(false);
        }

        
        StartCoroutine(CreatePurchasedItemDelayed(weaponData));
    }

    private IEnumerator CreatePurchasedItemDelayed(WeaponData weaponData)
    {
        
        yield return new WaitForEndOfFrame();

        CreatePurchasedItem(weaponData);
    }

    private void HandleTouchEnd()
    {
        if (isDragging && selectedItem != null)
        {
            if (isOverSellZone)
            {
                SellItem(selectedItem);
            }
            else
            {
                Vector2Int tileGridPosition = GetTileGridPosition(touchPosition.ReadValue<Vector2>());
                if (IsPositionWithinGrid(tileGridPosition) &&
                    selectedItemGrid.BoundryCheck(tileGridPosition.x, tileGridPosition.y,
                        selectedItem.WIDTH, selectedItem.HEIGHT))
                {
                    PutDownItem(tileGridPosition);
                }
            }
        }

        isHolding = false;
        isDragging = false;
    }

    private bool IsTouchOverSellZone(Vector2 touchPos)
    {
        if (sellZoneRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(sellZoneRect, touchPos);
    }

    private void UpdateSellPopup(Vector2 position)
    {
        if (isOverSellZone && selectedItem != null)
        {
            if (currentSellPopup == null)
            {
                currentSellPopup = Instantiate(sellPopupPrefab, canvasTransform);
                SetupSellPopup(currentSellPopup, selectedItem.weaponData);
            }
            currentSellPopup.transform.position = position + Vector2.up * 150f;
        }
        else
        {
            if (currentSellPopup != null)
            {
                Destroy(currentSellPopup);
                currentSellPopup = null;
            }
        }
    }

    private void SetupSellPopup(GameObject popup, WeaponData weaponData)
    {
        var priceText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (priceText != null)
        {
            priceText.text = $"Sell: {weaponData.SellPrice}";
        }
    }

    private void SellItem(InventoryItem item)
    {
        if (item == null || item.weaponData == null) return;

        
        int itemCount = 0;
        for (int x = 0; x < selectedItemGrid.Width; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height; y++)
            {
                if (selectedItemGrid.GetItem(x, y) != null)
                {
                    itemCount++;
                }
            }
        }

        
        if (itemCount <= 1)
        {
            Debug.Log("Cannot sell the last item in inventory!");
            
            Vector2Int tileGridPosition = GetTileGridPosition(touchPosition.ReadValue<Vector2>());
            if (IsPositionWithinGrid(tileGridPosition) &&
                selectedItemGrid.BoundryCheck(tileGridPosition.x, tileGridPosition.y,
                    selectedItem.WIDTH, selectedItem.HEIGHT))
            {
                PutDownItem(tileGridPosition);
            }
            return;
        }

        
        GameManager.Instance.PlayerStats.AddCoins(item.weaponData.SellPrice);
        Destroy(item.gameObject);

        if (currentSellPopup != null)
        {
            Destroy(currentSellPopup);
            currentSellPopup = null;
        }
        selectedItem = null;
        isDragging = false;
    }

    private void OnItemAddedToGrid(InventoryItem item)
    {
        if (GameManager.Instance.IsPlaying())
        {
            OnWeaponEquipped(item);
        }
    }

}