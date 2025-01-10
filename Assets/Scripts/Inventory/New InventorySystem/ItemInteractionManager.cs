using UnityEngine;

/// <summary>
/// 인벤토리 아이템의 상호작용을 관리하는 매니저 클래스
/// </summary>
public class ItemInteractionManager
{
    #region Fields
    private readonly ItemGrid targetGrid;
    private readonly WeaponInfoUI weaponInfoUI;
    private readonly Transform spawnPoint;
    private readonly InventoryHighlight inventoryHighlight;

    private InventoryItem selectedItem;
    private RectTransform selectedItemRect;
    private bool isDragging;

    // 기존 코드와 일치시키기 위한 상수
    private const float ITEM_LIFT_OFFSET = 350f;
    #endregion

    #region Properties
    public bool HasSelectedItem => selectedItem != null;
    #endregion

    #region Constructor
    public ItemInteractionManager(
        ItemGrid grid,
        WeaponInfoUI infoUI,
        Transform spawn,
        InventoryHighlight highlight)
    {
        targetGrid = grid;
        weaponInfoUI = infoUI;
        spawnPoint = spawn;
        inventoryHighlight = highlight;
    }
    #endregion

    #region Public Methods
    public void SetGrid(ItemGrid newGrid)
    {
        inventoryHighlight?.SetParent(newGrid);
    }

    public void StartDragging(InventoryItem item, Vector2 touchPosition)
    {
        if (item == null) return;

        selectedItem = item;
        selectedItemRect = item.GetComponent<RectTransform>();
        isDragging = true;

        Vector2Int currentPosition = item.GridPosition;
        if (item.OnGrid)
        {
            targetGrid.RemoveItem(currentPosition);
        }

        UpdateWeaponInfo(item);
        UpdateHighlight(item);  // 하이라이트 업데이트

        Vector2 liftedPosition = touchPosition + Vector2.up * ITEM_LIFT_OFFSET;
        selectedItemRect.position = liftedPosition;
    }
    public void EndDragging(Vector2 finalPosition)
    {
        if (!isDragging || selectedItem == null) return;

        Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);
        if (targetGrid.IsValidPosition(gridPosition))
        {
            TryPlaceItem(selectedItem, gridPosition);
        }
        else
        {
            ReturnToOriginalPosition(selectedItem);
        }

        ClearSelection();
    }

    public InventoryItem GetSelectedItem() => selectedItem;

    public void ClearSelection()
    {
        selectedItem = null;
        selectedItemRect = null;
        isDragging = false;
        inventoryHighlight?.Show(false);
    }
    #endregion

    #region Private Methods
    private void TryPlaceItem(InventoryItem item, Vector2Int position)
    {
        InventoryItem overlapItem = null;
        bool placed = targetGrid.PlaceItem(item, position, ref overlapItem);

        if (placed)
        {
            Vector2 itemPosition = targetGrid.CalculatePositionOnGrid(
                item,
                position.x,
                position.y
            );
            item.GetComponent<RectTransform>().localPosition = itemPosition;
        }
        else
        {
            ReturnToOriginalPosition(item);
        }

        UpdateHighlight(item);
    }

    private void ReturnToOriginalPosition(InventoryItem item)
    {
        if (item == null) return;

        if (item.OnGrid)
        {
            Vector2Int originalPosition = item.GridPosition;
            InventoryItem overlapItem = null;
            targetGrid.PlaceItem(item, originalPosition, ref overlapItem);

            Vector2 position = targetGrid.CalculatePositionOnGrid(
                item,
                originalPosition.x,
                originalPosition.y
            );
            item.GetComponent<RectTransform>().localPosition = position;
        }
        else
        {
            item.GetComponent<RectTransform>().position = spawnPoint.position;
        }
    }
    private void UpdateWeaponInfo(InventoryItem item)
    {
        if (weaponInfoUI == null || item == null) return;

        WeaponData weaponData = item.GetWeaponData();
        if (weaponData != null)
        {
            weaponInfoUI.UpdateWeaponInfo(weaponData);
        }
    }
    public void UpdateDraggedItemPosition(Vector2 currentPosition)
    {
        if (selectedItem != null)
        {
            // 선택된 아이템의 위치를 실시간으로 업데이트
            Vector2 liftedPosition = currentPosition + Vector2.up * ITEM_LIFT_OFFSET;
            selectedItemRect.position = liftedPosition;
        }
    }

    private void UpdateHighlight(InventoryItem item)
    {
        if (inventoryHighlight == null || item == null) return;

        inventoryHighlight.Show(true);
        inventoryHighlight.SetSize(item);
        inventoryHighlight.SetPosition(targetGrid, item);
    }
    private void UpdateHighlight(bool show)
    {
        if (inventoryHighlight != null)
        {
            inventoryHighlight.Show(show);
        }
    }
    #endregion
}