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

    private readonly Vector2 itemLiftOffset;
    private readonly Vector2Int invalidPosition = new(-1,-1);

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
        itemLiftOffset = Vector2.up * ITEM_LIFT_OFFSET;
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

        if (item.OnGrid)
        {
            targetGrid.RemoveItem(item.GridPosition);
        }

        UpdateWeaponInfo(item);
        UpdateHighlight(item);

        // 캐시된 offset 사용
        selectedItemRect.position = touchPosition + itemLiftOffset;
    }
     public void EndDragging(Vector2 finalPosition)
    {
        if (!isDragging || selectedItem == null) return;

        Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);
        
        // 유효한 위치인 경우에만 배치 시도
        if (targetGrid.IsValidPosition(gridPosition) && TryPlaceItem(selectedItem, gridPosition))
        {
            // 아이템이 성공적으로 배치됨
            inventoryHighlight?.Show(false);
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
    private bool TryPlaceItem(InventoryItem item, Vector2Int position)
    {
        if (item == null) return false;

        InventoryItem overlapItem = null;
        if (targetGrid.PlaceItem(item, position, ref overlapItem))
        {
            Vector2 itemPosition = targetGrid.CalculatePositionOnGrid(
                item,
                position.x,
                position.y
            );
            item.GetComponent<RectTransform>().localPosition = itemPosition;
            return true;
        }

        return false;
    }
    private void ReturnToOriginalPosition(InventoryItem item)
    {
        if (item == null) return;

        if (item.OnGrid)
        {
            Vector2Int originalPosition = item.GridPosition;
            InventoryItem overlapItem = null;

            if (targetGrid.PlaceItem(item, originalPosition, ref overlapItem))
            {
                item.GetComponent<RectTransform>().localPosition =
                    targetGrid.CalculatePositionOnGrid(item, originalPosition.x, originalPosition.y);
            }
        }
        else if (spawnPoint != null)
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
        if (selectedItem == null || selectedItemRect == null) return;

        selectedItemRect.position = currentPosition + itemLiftOffset;
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
        inventoryHighlight?.Show(show);
    }
    #endregion
}