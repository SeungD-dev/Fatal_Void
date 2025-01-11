using UnityEngine;

public class InventoryHighlight : MonoBehaviour, IPooledObject
{
    [SerializeField] private RectTransform highlighter;
    private ItemGrid currentGrid; // 현재 활성 그리드 추적

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
        // 스케일을 1로 초기화
        if (highlighter != null)
        {
            highlighter.localScale = Vector3.one;
        }

        // 초기에는 비활성화
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
        if (highlighter != null)
        {
            highlighter.gameObject.SetActive(visible);
        }
    }

    public void SetSize(InventoryItem targetItem)
    {
        if (targetItem == null || highlighter == null) return;

        Vector2 size = new Vector2(
            targetItem.Width * ItemGrid.TILE_SIZE,
            targetItem.Height * ItemGrid.TILE_SIZE
        );

        highlighter.sizeDelta = size;

        // 스케일이 1이 아닐 경우 강제로 1로 설정
        if (highlighter.localScale != Vector3.one)
        {
            highlighter.localScale = Vector3.one;
            Debug.LogWarning("Highlighter scale was not 1. Resetting to default.");
        }
    }
    public void SetParent(ItemGrid targetGrid)
    {
        if (targetGrid == null || highlighter == null) return;

        RectTransform gridRectTransform = targetGrid.GetComponent<RectTransform>();
        if (gridRectTransform != null)
        {
            highlighter.SetParent(gridRectTransform, false);
            // 부모 변경 시 로컬 포지션 초기화 방지
            highlighter.localPosition = Vector2.zero;
        }
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX = -1, int posY = -1)
    {
        if (targetGrid == null || targetItem == null || highlighter == null) return;

        int x = posX >= 0 ? posX : targetItem.onGridPositionX;
        int y = posY >= 0 ? posY : targetItem.onGridPositionY;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, x, y);
        highlighter.localPosition = pos;
    }

    public InventoryItem GetAssociatedItem() => associatedItem;
}