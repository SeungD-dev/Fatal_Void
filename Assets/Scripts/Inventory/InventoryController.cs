using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

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

    [SerializeField] List<WeaponData> weapons;
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform canvasTransform;
    [SerializeField] private WeaponInfoUI weaponInfoUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private GameObject playerControlUI;
    [SerializeField] private GameObject playerStatsUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private Button progressButton;

    InventoryHighlight inventoryHighlight;
    private bool isDragging = false;
    private Vector2 originalSpawnPosition;

    public bool isHolding = false;
    private float holdStartTime;
    private Vector2 holdStartPosition;
    private const float HOLD_THRESHOLD = 0.3f; // 홀드 인식 시간
    private const float HOLD_MOVE_THRESHOLD = 20f; // 홀드 중 이동 허용 범위
    public static float ITEM_LIFT_OFFSET = 0f; // 아이템을 들어올릴 높이
    private float lastRotationTime = -999f;
    private const float ROTATION_COOLDOWN = 0.3f; // 회전 간 최소 시간
    private bool isRotating = false;
    private int lastRotatingTouchId = -1;


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

            // CleanupInvalidItems는 UI가 완전히 활성화된 후에 호출
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
        // Grid 상태 체크
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected! Cannot start game.");
            return;
        }

        bool hasEquippedItem = false;
        InventoryItem equippedItem = null;

        // 현재 그리드에서 장착된 아이템 찾기
        for (int x = 0; x < selectedItemGrid.Width && !hasEquippedItem; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height && !hasEquippedItem; y++)
            {
                InventoryItem item = selectedItemGrid.GetItem(x, y);
                if (item != null)
                {
                    equippedItem = item;
                    hasEquippedItem = true;
                    break;
                }
            }
        }

        if (hasEquippedItem && equippedItem != null)
        {
            // 무기 장착 및 게임 시작
            OnWeaponEquipped(equippedItem);

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
            // 경고 메시지 표시
            Debug.LogWarning("No item equipped! Please place a weapon in the grid.");
            // 여기에 사용자에게 알림을 표시하는 UI 로직을 추가할 수 있습니다.
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
        holdStartTime = Time.time;
        holdStartPosition = touchPos;

        //Debug.Log($"Touch Started at: {holdStartTime}, Position: {touchPos}");
        StartCoroutine(CheckForHold());
    }
    private IEnumerator CheckForHold()
    {
        float startTime = Time.realtimeSinceStartup;
        bool holdChecking = true;

        // 터치한 위치 확인
        Vector2 rawTouchPos = holdStartPosition;
        InventoryItem touchedItem = null;

        // 1. 먼저 선택된 아이템(스폰된 아이템)이 있는지 확인
        if (selectedItem != null)
        {
            RectTransform itemRect = selectedItem.GetComponent<RectTransform>();
            // 터치 위치가 아이템 영역 내에 있는지 확인
            if (RectTransformUtility.RectangleContainsScreenPoint(itemRect, rawTouchPos))
            {
                touchedItem = selectedItem;
                Debug.Log("Touched spawned item");
            }
        }

        // 2. 선택된 아이템이 없다면 그리드 내 아이템 확인
        if (touchedItem == null)
        {
            Vector2Int initialGridPosition = GetTileGridPosition(holdStartPosition);
            if (IsPositionWithinGrid(initialGridPosition))
            {
                touchedItem = selectedItemGrid.GetItem(initialGridPosition.x, initialGridPosition.y);
                Debug.Log($"Checking grid slot at: ({initialGridPosition.x}, {initialGridPosition.y})");
            }
        }

        while (holdChecking && touchPress.IsPressed())
        {
            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - startTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float moveDistance = Vector2.Distance(holdStartPosition, currentPos);

            //Debug.Log($"Elapsed: {elapsedTime}, HOLD_THRESHOLD: {HOLD_THRESHOLD}, touchedItem: {touchedItem != null}");

            if (moveDistance > HOLD_MOVE_THRESHOLD)
            {
                if (touchedItem != null && !isDragging)
                {
                    if (touchedItem == selectedItem)
                    {
                        // 스폰된 아이템을 집는 경우
                        isDragging = true;
                        isHolding = true;
                        rectTransform = touchedItem.GetComponent<RectTransform>();
                    }
                    else
                    {
                        // 그리드의 아이템을 집는 경우
                        PickUpItem(new Vector2Int(touchedItem.onGridPositionX, touchedItem.onGridPositionY));
                    }
                }
                holdChecking = false;
                break;
            }

            if (elapsedTime >= HOLD_THRESHOLD && touchedItem != null)
            {
                Debug.Log("Hold threshold reached!");
                selectedItem = touchedItem;
                rectTransform = selectedItem.GetComponent<RectTransform>();

                isDragging = true;
                isHolding = true;

                // 그리드에 있는 아이템인 경우에만 PickUpItem 호출
                if (touchedItem.onGridPositionX >= 0 && touchedItem.onGridPositionY >= 0)
                {
                    selectedItemGrid.PickUpItem(touchedItem.onGridPositionX, touchedItem.onGridPositionY);
                }

                Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
                rectTransform.position = liftedPosition;

                Debug.Log("Hold successful!");
                holdChecking = false;
                break;
            }

            yield return new WaitForEndOfFrame();
        }
    }
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

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
                // Grid 밖에 있을 때는 드래깅 상태는 유지하되 홀드 상태만 해제
                isHolding = false;
                // isDragging은 true로 유지
            }
        }
        else
        {
            // 드래깅 중이 아닐 때만 모든 상태 초기화
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

            // 터치가 1개일 때 쿨다운 초기화
            if (activeTouches.Count == 1)
            {
                lastRotationTime = Time.time - ROTATION_COOLDOWN; // 쿨다운 초기화
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
        Vector2Int positionOnGrid;

        // 아이템을 들고 있을 때
        if (isHolding)
        {
            // 상승 높이를 고려한 위치 계산
            touchPos -= Vector2.up * ITEM_LIFT_OFFSET;
        }

        positionOnGrid = GetTileGridPosition(touchPos);

        // Grid 바깥 영역 체크
        if (!IsPositionWithinGrid(positionOnGrid))
        {
            inventoryHighlight?.Show(false);
            return;
        }

        if (selectedItem == null)
        {
            InventoryItem itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(itemToHighlight);

                // 하이라이트도 상승 높이를 고려하여 위치 설정
                Vector2 highlightPos = selectedItemGrid.CalculatePositionOnGrid(
                    itemToHighlight,
                    itemToHighlight.onGridPositionX,
                    itemToHighlight.onGridPositionY);

                if (isHolding)
                {
                    highlightPos += Vector2.up * ITEM_LIFT_OFFSET;
                }

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
            // 현재 위치가 유효한지 확인
            bool isValidPosition = selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT);

            if (isValidPosition)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(selectedItem);

                // 하이라이트 위치에도 상승 높이 적용
                Vector2 highlightPos = selectedItemGrid.CalculatePositionOnGrid(
                    selectedItem, positionOnGrid.x, positionOnGrid.y);

                if (isHolding)
                {
                    highlightPos += Vector2.up * ITEM_LIFT_OFFSET;
                }

                inventoryHighlight?.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }

        // WeaponInfo UI 업데이트
        if (selectedItem != null && weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
        }
    }

    private void RotateItem()
    {
        if (selectedItem == null) return;

        // 회전 전 현재 위치 저장
        Vector2Int currentGridPos = GetTileGridPosition(rectTransform.position);

        selectedItem.Rotate();

        if (inventoryHighlight != null)
        {
            inventoryHighlight.SetSize(selectedItem);

            // Grid 내부에 있을 경우 하이라이트 업데이트
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
            // 상승 높이만큼 위치 조정
            position -= Vector2.up * ITEM_LIFT_OFFSET;
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

        touchActions?.Dispose();
    }


    public void CreatePurchasedItem(WeaponData weaponData)
    {
        if (itemSpawnPoint == null || selectedItemGrid == null)
        {
            Debug.LogWarning("Required references not set!");
            return;
        }

        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        rectTransform = itemObj.GetComponent<RectTransform>();

        // 먼저 아이템을 그리드의 자식으로 설정
        rectTransform.SetParent(selectedItemGrid.GetComponent<RectTransform>(), false);

        inventoryItem.Set(weaponData);

        // 스폰 위치를 그리드 좌표로 변환
        Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(itemSpawnPoint.position);

        // 유효한 그리드 위치 확인 및 조정
        if (!selectedItemGrid.BoundryCheck(gridPosition.x, gridPosition.y,
            inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            // 범위를 벗어나면 기본 위치(0,0)로 설정
            gridPosition = new Vector2Int(0, 0);
        }

        // 그리드에 아이템 등록
        inventoryItem.onGridPositionX = gridPosition.x;
        inventoryItem.onGridPositionY = gridPosition.y;

        // 그리드 상의 실제 위치 계산
        Vector2 position = selectedItemGrid.CalculatePositionOnGrid(inventoryItem,
            gridPosition.x, gridPosition.y);
        rectTransform.localPosition = position;

        selectedItem = inventoryItem;

        // 하이라이트 설정
        if (inventoryHighlight != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(selectedItem);
        }

        // 무기 정보 UI 업데이트
        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(weaponData);
        }

        // 상태 초기화
        isDragging = false;
        isHolding = false;
    }
    private void PickUpItem(Vector2Int tileGridPosition)
    {
        InventoryItem itemToPickup = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (itemToPickup != null)
        {
            if (selectedItem != null && selectedItem != itemToPickup)
            {
                selectedItem = null;
                isDragging = false;
            }

            selectedItem = selectedItemGrid.PickUpItem(itemToPickup.onGridPositionX, itemToPickup.onGridPositionY);
            if (selectedItem != null)
            {
                rectTransform = selectedItem.GetComponent<RectTransform>();
                isDragging = true;

                // 터치 위치보다 위로 들어올림
                Vector2 currentPos = touchPosition.ReadValue<Vector2>();
                Vector2 liftedPosition = currentPos + Vector2.up * ITEM_LIFT_OFFSET;
                rectTransform.position = liftedPosition;

                if (weaponInfoUI != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
                }
            }
        }
    }

    private void PutDownItem(Vector2Int tileGridPosition)
    {
        if (selectedItem == null) return;

        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);

        if (complete)
        {
            if (overlapItem != null)
            {
                // 겹친 아이템과 위치 교환
                selectedItem = overlapItem;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
                // 상태는 초기화하여 다시 조작 가능하게 함
                isDragging = false;
                isHolding = false;
            }
            else
            {
                // 성공적으로 배치되었을 때도 상태만 초기화
                isDragging = false;
                isHolding = false;
                // selectedItem은 null로 설정하지 않음
            }

            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(selectedItem);
                inventoryHighlight.SetPosition(selectedItemGrid, selectedItem);
            }
        }
        else
        {
            // 배치 실패시에도 상태만 초기화
            isDragging = false;
            isHolding = false;
        }

        if (weaponInfoUI != null && selectedItem != null)
        {
            weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
        }
    }


    // Grid 상태 체크 및 정리 메서드 추가
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

        // 인벤토리 UI를 먼저 활성화
        inventoryUI.SetActive(true);
        if (playerControlUI != null && playerStatsUI != null)
        {
            playerControlUI.SetActive(false);
            playerStatsUI.SetActive(false);
        }

        // 약간의 지연 후 아이템 생성
        StartCoroutine(CreatePurchasedItemDelayed(weaponData));
    }

    private IEnumerator CreatePurchasedItemDelayed(WeaponData weaponData)
    {
        // UI가 완전히 활성화될 때까지 대기
        yield return new WaitForEndOfFrame();

        CreatePurchasedItem(weaponData);
    }


}