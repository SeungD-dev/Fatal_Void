using UnityEngine;

public class InventoryHighlight : MonoBehaviour, IPooledObject
{
    [SerializeField] private RectTransform highlighter;
    private ItemGrid currentGrid; // 현재 활성 그리드 추적

    private static readonly Vector3 defaultScale = Vector3.one;
    private static readonly Vector2 defaultPosition = Vector2.zero;

    private InventoryItem associatedItem;
    private void Awake()
    {
        if (highlighter == null)
        {
            highlighter = GetComponent<RectTransform>();
        }     
        InitializeHighlighter();
    }

    private void InitializeHighlighter()
    {
        if (highlighter != null)
        {
            highlighter.localScale = defaultScale;
        }
        Show(false);
    }

    public void OnObjectSpawn()
    {
        
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        if (highlighter != null)
        {
            highlighter.localScale = Vector3.one;
            highlighter.localPosition = Vector3.zero;
        }
        associatedItem = null;
    }

    public void Show(bool visible)
    {
        if (highlighter != null && highlighter.gameObject.activeSelf != visible)
        {
            highlighter.gameObject.SetActive(visible);
        }
    }


    public void SetSize(InventoryItem targetItem)
    {
        if (targetItem == null || highlighter == null) return;

        highlighter.sizeDelta = new Vector2(
            targetItem.Width * ItemGrid.TILE_SIZE,
            targetItem.Height * ItemGrid.TILE_SIZE
        );

        if (highlighter.localScale != defaultScale)
        {
            highlighter.localScale = defaultScale;
        }
    }
    public void SetParent(ItemGrid targetGrid)
    {
        if (targetGrid == null || highlighter == null || currentGrid == targetGrid) return;

        var gridRectTransform = targetGrid.GetComponent<RectTransform>();
        if (gridRectTransform != null)
        {
            highlighter.SetParent(gridRectTransform, false);
            highlighter.localPosition = defaultPosition;
            currentGrid = targetGrid;
        }
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX = -1, int posY = -1)
    {
        if (targetGrid == null || targetItem == null || highlighter == null) return;

        highlighter.localPosition = targetGrid.CalculatePositionOnGrid(
            targetItem,
            posX >= 0 ? posX : targetItem.onGridPositionX,
            posY >= 0 ? posY : targetItem.onGridPositionY
        );
    }

    public InventoryItem GetAssociatedItem() => associatedItem;
}