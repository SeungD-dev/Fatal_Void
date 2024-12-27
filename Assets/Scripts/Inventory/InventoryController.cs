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
        // Grid ���� üũ
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected! Cannot start game.");
            return;
        }

        bool hasEquippedItem = false;
        InventoryItem equippedItem = null;

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
            // ���� ���� �� ���� ����
            OnWeaponEquipped(equippedItem);

            // UI ��ȯ
            inventoryHighlight?.Show(false);
            inventoryUI?.SetActive(false);
            playerControlUI?.SetActive(true);
            playerStatsUI?.SetActive(true);

            // ���� ���� ����
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }
        else
        {
            // ��� �޽��� ǥ��
            Debug.LogWarning("No item equipped! Please place a weapon in the grid.");
            // ���⿡ ����ڿ��� �˸��� ǥ���ϴ� UI ������ �߰��� �� �ֽ��ϴ�.
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
    
    if (IsPositionWithinGrid(gridPosition))
    {
        InventoryItem touchedItem = selectedItemGrid.GetItem(gridPosition.x, gridPosition.y);
        if (touchedItem != null)
        {
            HandleItemSelection(touchedItem);
        }
    }

    holdStartTime = Time.time;
    holdStartPosition = touchPos;
    StartCoroutine(CheckForHold());
}

private void HandleItemSelection(InventoryItem item)
{
    if (item != null && weaponInfoUI != null)
    {
        weaponInfoUI.UpdateWeaponInfo(item.weaponData);
    }
}
    private IEnumerator CheckForHold()
    {
        float startTime = Time.realtimeSinceStartup;
        bool holdChecking = true;

        
        Vector2 rawTouchPos = holdStartPosition;
        InventoryItem touchedItem = null;

        
        if (selectedItem != null)
        {
            RectTransform itemRect = selectedItem.GetComponent<RectTransform>();
            
            if (RectTransformUtility.RectangleContainsScreenPoint(itemRect, rawTouchPos))
            {
                touchedItem = selectedItem;
                Debug.Log("Touched spawned item");
            }
        }

        
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
                        
                        isDragging = true;
                        isHolding = true;
                        rectTransform = touchedItem.GetComponent<RectTransform>();
                    }
                    else
                    {
                        
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

                
                if (touchedItem.onGridPositionX >= 0 && touchedItem.onGridPositionY >= 0)
                {
                    selectedItemGrid.PickUpItem(touchedItem.onGridPositionX, touchedItem.onGridPositionY);
                }

                
                Vector2 liftedPosition = touchPosition.ReadValue<Vector2>() + Vector2.up * ITEM_LIFT_OFFSET;
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
        Vector2Int gridPosition = freePosition ?? Vector2Int.zero;

        // 아이템 배치
        InventoryItem overlapItem = null;
        selectedItemGrid.PlaceItem(inventoryItem, gridPosition.x, gridPosition.y, ref overlapItem);

        // UI 위치 설정
        Vector2 position = selectedItemGrid.CalculatePositionOnGrid(inventoryItem, gridPosition.x, gridPosition.y);
        rectTransform.localPosition = position;
    }

    // InventoryController.cs
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

        // 지정된 위치에 아이템 배치 시도
        InventoryItem overlapItem = null;
        bool placed = selectedItemGrid.PlaceItem(inventoryItem, position.x, position.y, ref overlapItem);

        if (!placed)
        {
            // 지정된 위치에 배치 실패시 새로운 위치 찾기
            Vector2Int? freePosition = selectedItemGrid.FindSpaceForObject(inventoryItem);
            if (freePosition.HasValue)
            {
                selectedItemGrid.PlaceItem(inventoryItem, freePosition.Value.x, freePosition.Value.y, ref overlapItem);
                position = freePosition.Value;
            }
            else
            {
                Debug.LogError("No space available for upgraded item!");
                Destroy(itemObj);
                return;
            }
        }

        // UI 위치 설정
        Vector2 uiPosition = selectedItemGrid.CalculatePositionOnGrid(inventoryItem, position.x, position.y);
        rectTransform.localPosition = uiPosition;

        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(weaponData);
        }
    }

    // 기존 CreatePurchasedItem 메서드는 그대로 유지
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
                selectedItem = overlapItem;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
                isDragging = false;
                isHolding = false;
            }
            else
            {
                isDragging = false;
                isHolding = false;
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
            isDragging = false;
            isHolding = false;
        }

        if (weaponInfoUI != null && selectedItem != null)
        {
            weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
        }
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

}