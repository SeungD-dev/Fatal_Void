using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector2Int gridPosition;
    private InventoryGrid grid;
    private bool isOccupied;

    public Vector2Int Position => gridPosition;
    public bool IsOccupied => isOccupied;

    public void Initialize(Vector2Int position, InventoryGrid parentGrid)
    {
        gridPosition = position;
        grid = parentGrid;
        isOccupied = false;

        // 필요한 경우 디버그용 시각적 표시
#if UNITY_EDITOR
        var image = gameObject.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0.1f);
#endif
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 드래그 중인 아이템이 있을 때 하이라이트 효과
        if (eventData.dragging)
        {
            // 하이라이트 효과 구현
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 하이라이트 효과 제거
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
}
