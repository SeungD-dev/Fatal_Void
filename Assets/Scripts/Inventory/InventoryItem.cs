using NUnit.Framework.Interfaces;
using UnityEngine.EventSystems;
using UnityEngine;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // WeaponData를 public 프로퍼티로 노출
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData;

    // CurrentShape를 public 프로퍼티로 노출
    private bool[,] currentShape;
    public bool[,] CurrentShape => currentShape;

    // CurrentPosition을 get, set 가능하게 수정
    private Vector2Int? currentPosition;
    public Vector2Int? CurrentPosition { get; set; }

    private RectTransform rectTransform;
    private InventoryGrid grid;
    private Vector2 dragOffset;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        grid = GetComponentInParent<InventoryGrid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작 위치 저장
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        dragOffset = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중 위치 업데이트
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rectTransform.localPosition = localPoint - dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드롭된 위치의 그리드 셀 확인
        Vector2Int gridPosition = grid.GetGridPosition(eventData.position);
        GridCell targetCell = grid.GetCell(gridPosition);

        if (targetCell != null && !targetCell.IsOccupied)
        {
            // 아이템을 셀에 맞춰 배치
            RectTransform cellRect = targetCell.GetComponent<RectTransform>();
            rectTransform.position = cellRect.position;
            targetCell.SetOccupied(true);
        }
        else
        {
            // 원래 위치로 돌아가기
            // ... 원위치 로직 구현
        }
    }
}