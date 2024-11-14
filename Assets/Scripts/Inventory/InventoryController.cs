using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class InventoryController : MonoBehaviour
{
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;

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
    [SerializeField] private GameObject playerUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private Button progressButton;

    InventoryHighlight inventoryHighlight;
    private bool isDragging = false;
    private Vector2 originalSpawnPosition;

    private const float DOUBLE_TAP_THRESHOLD = 0.3f;
    private const float DOUBLE_TAP_RANGE = 300f;
    private float lastTapTime;
    private Vector2 lastTapPosition;

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

    private void ResetItemToSpawnPoint(GameObject itemObj)
    {
        if (rectTransform == null || itemSpawnPoint == null) return;

        Vector2 spawnPos = itemSpawnPoint.position;
        Vector2 itemSize = rectTransform.sizeDelta;

        // 아이템 크기의 절반만큼 오프셋 적용
        spawnPos.x += itemSize.x * 0.5f;
        spawnPos.y -= itemSize.y * 0.5f;

        rectTransform.position = spawnPos;
        rectTransform.SetAsLastSibling();

        // 아이템은 선택된 상태로 유지하고 드래그 가능하도록 설정
        isDragging = true;

        // 하이라이트 위치도 업데이트
        if (inventoryHighlight != null && selectedItem != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(selectedItem);
        }
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

        if (playerUI != null)
        {
            playerUI.SetActive(!isActive);
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
    private void HandleItemOutOfBounds()
    {
        if (selectedItem != null && isDragging)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();

            // 화면 크기 가져오기
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // 화면 밖으로 나갔는지 체크
            if (touchPos.x < 0 || touchPos.x > screenSize.x ||
                touchPos.y < 0 || touchPos.y > screenSize.y)
            {
                ResetItemToSpawnPoint(selectedItem.gameObject);
                isDragging = false;
                if (inventoryHighlight != null)
                {
                    inventoryHighlight.Show(false);
                }
            }
        }
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
            playerUI?.SetActive(true);

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

    private void ProcessDoubleTap(Vector2 currentTapPosition)
    {
        float timeSinceLastTap = Time.time - lastTapTime;
        float tapDistance = Vector2.Distance(currentTapPosition, lastTapPosition);

        if (timeSinceLastTap <= DOUBLE_TAP_THRESHOLD && tapDistance <= DOUBLE_TAP_RANGE)
        {
            Debug.Log($"Double tap detected! Distance: {tapDistance}");

            if (selectedItem != null)
            {
                RotateItem();

                if (inventoryHighlight != null)
                {
                    inventoryHighlight.SetSize(selectedItem);
                }

                // Grid 내부에 있는 경우만 재배치 시도
                if (!isDragging)
                {
                    Vector2Int currentPos = new Vector2Int(selectedItem.onGridPositionX, selectedItem.onGridPositionY);
                    if (IsPositionWithinGrid(currentPos))
                    {
                        bool canPlace = selectedItemGrid.BoundryCheck(
                            currentPos.x,
                            currentPos.y,
                            selectedItem.WIDTH,
                            selectedItem.HEIGHT
                        );

                        if (canPlace)
                        {
                            PutDownItem(currentPos);
                        }
                        else
                        {
                            isDragging = true;
                        }
                    }
                }
            }

            lastTapTime = 0;
        }
        else
        {
            lastTapTime = Time.time;
            lastTapPosition = currentTapPosition;
        }
    }
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();

        // 선택된 아이템이 있을 때는 항상 더블 탭 체크
        if (selectedItem != null)
        {
            ProcessDoubleTap(touchPos);
        }

        Vector2Int tileGridPosition = GetTileGridPosition(touchPos);

        // Grid 내부 터치 처리
        if (IsPositionWithinGrid(tileGridPosition))
        {
            if (!isDragging)
            {
                InventoryItem touchedItem = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

                if (touchedItem != null)
                {
                    tileGridPosition = new Vector2Int(touchedItem.onGridPositionX, touchedItem.onGridPositionY);
                }

                PickUpItem(tileGridPosition);
                if (selectedItem != null)
                {
                    isDragging = true;
                    if (inventoryHighlight != null)
                    {
                        inventoryHighlight.SetSize(selectedItem);
                    }
                }
            }
        }
        // Grid 바깥 터치 처리
        else if (!isDragging && selectedItem == null)
        {
            // 스폰 포인트 근처의 아이템 선택을 위한 처리
            if (itemSpawnPoint != null && selectedItem == null)
            {
                float distance = Vector2.Distance(touchPos, itemSpawnPoint.position);
                if (distance < 100f) // 스폰 포인트 주변 영역
                {
                    isDragging = true;
                }
            }
        }
    }
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        if (selectedItem != null && isDragging)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();
            Vector2Int tileGridPosition = GetTileGridPosition(touchPos);

            bool isWithinGrid = IsPositionWithinGrid(tileGridPosition) &&
                              selectedItemGrid.BoundryCheck(tileGridPosition.x, tileGridPosition.y,
                                  selectedItem.WIDTH, selectedItem.HEIGHT);

            if (isWithinGrid)
            {
                PutDownItem(tileGridPosition);
            }
            else
            {
                ResetItemToSpawnPoint(selectedItem.gameObject);
            }
        }
    }
    private void Update()
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null)
        {
            return;
        }

        // 드래그 처리
        if (isDragging && selectedItem != null)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();
            rectTransform.position = touchPos;

            Vector2Int positionOnGrid = GetTileGridPosition(touchPos);
            bool isWithinGrid = IsPositionWithinGrid(positionOnGrid) &&
                              selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                                  selectedItem.WIDTH, selectedItem.HEIGHT);

            if (inventoryHighlight != null)
            {
                if (isWithinGrid)
                {
                    // Grid 내부에 있을 때만 하이라이트 표시
                    inventoryHighlight.Show(true);
                    inventoryHighlight.SetSize(selectedItem);
                    inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
                }
                else
                {
                    // Grid 바깥에 있을 때는 하이라이트 숨김
                    inventoryHighlight.Show(false);
                }
            }
        }
        else if (selectedItem != null && !isDragging)
        {
            // 드래그 중이 아닐 때는 선택된 아이템이 Grid 내부에 있을 때만 하이라이트 표시
            Vector2Int itemPosition = new Vector2Int(selectedItem.onGridPositionX, selectedItem.onGridPositionY);
            bool isWithinGrid = IsPositionWithinGrid(itemPosition) &&
                              selectedItemGrid.BoundryCheck(itemPosition.x, itemPosition.y,
                                  selectedItem.WIDTH, selectedItem.HEIGHT);

            if (inventoryHighlight != null)
            {
                if (isWithinGrid)
                {
                    inventoryHighlight.Show(true);
                    inventoryHighlight.SetSize(selectedItem);
                    inventoryHighlight.SetPosition(selectedItemGrid, selectedItem);
                }
                else
                {
                    inventoryHighlight.Show(false);
                }
            }
        }
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

        // Grid 바깥 영역 체크
        if (!IsPositionWithinGrid(positionOnGrid))
        {
            inventoryHighlight?.Show(false);
            return;
        }

        // 아이템을 들고 있지 않은 경우
        if (selectedItem == null)
        {
            InventoryItem itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(itemToHighlight);
                inventoryHighlight?.SetPosition(selectedItemGrid, itemToHighlight,
                    itemToHighlight.onGridPositionX, itemToHighlight.onGridPositionY);

                if (weaponInfoUI != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(itemToHighlight.weaponData);
                }
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
        else
        {
            // 아이템을 들고 있는 경우
            if (weaponInfoUI != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }

            // 현재 위치가 유효한지 확인
            bool isValidPosition = selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT);

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
        selectedItem.Rotate();

        if(inventoryHighlight != null)
        {
            inventoryHighlight.SetSize(selectedItem);
        }
    }

    private Vector2Int GetTileGridPosition(Vector2 position)
    {
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
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
        if (itemSpawnPoint == null)
        {
            Debug.LogWarning("Item spawn point is not assigned!");
            return;
        }

        // 아이템 생성
        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        selectedItem = inventoryItem;
        rectTransform = itemObj.GetComponent<RectTransform>();

        // 아이템 데이터 설정
        inventoryItem.Set(weaponData);

        // Canvas의 자식으로 설정
        rectTransform.SetParent(canvasTransform, false);

        // 스폰 위치 설정
        ResetItemToSpawnPoint(itemObj);

        // 자동으로 드래그 모드 시작
        isDragging = true;

        // 하이라이트 상태 업데이트
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
    }

    private void PickUpItem(Vector2Int tileGridPosition)
    {
        // 해당 위치에서 시작하는 아이템 찾기
        InventoryItem itemToPickup = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        if (itemToPickup != null)
        {
            // 기존 선택된 아이템이 있다면 해제
            if (selectedItem != null && selectedItem != itemToPickup)
            {
                selectedItem = null;
                isDragging = false;
            }

            // 아이템의 실제 시작 위치에서 집어올리기
            selectedItem = selectedItemGrid.PickUpItem(itemToPickup.onGridPositionX, itemToPickup.onGridPositionY);
            if (selectedItem != null)
            {
                rectTransform = selectedItem.GetComponent<RectTransform>();
                isDragging = true;

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
                isDragging = true;
            }

            // 하이라이트 유지 (아이템이 선택된 상태 유지)
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(selectedItem);
                inventoryHighlight.SetPosition(selectedItemGrid, selectedItem);
            }
            isDragging = false;
        }
        else
        {
            ResetItemToSpawnPoint(selectedItem.gameObject);
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
                            ResetItemToSpawnPoint(item.gameObject);
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
        if (playerUI != null)
        {
            playerUI.SetActive(false);
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
