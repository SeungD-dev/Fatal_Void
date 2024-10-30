using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 4;
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private GameObject cellContainer; // 셀들을 담을 새 컨테이너

    private GridCell[,] cells;
    private float cellSize;

    private void Awake()
    {
        // 셀 컨테이너 생성
        cellContainer = new GameObject("CellContainer");
        var containerRect = cellContainer.AddComponent<RectTransform>();
        cellContainer.transform.SetParent(transform, false);

        // 컨테이너의 RectTransform 설정
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        containerRect.localScale = Vector3.one;
    }

    private void Start()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<RectTransform>();

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        cells = new GridCell[gridSize, gridSize];

        // 배경 이미지 크기를 기준으로 각 셀의 크기 계산
        cellSize = (backgroundImage.rect.width / gridSize);

        // 그리드 셀 생성
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CreateCell(x, y);
            }
        }
    }

    private void CreateCell(int x, int y)
    {
        GameObject cellObj = new GameObject($"Cell [{x},{y}]", typeof(RectTransform));
        RectTransform rectTransform = cellObj.GetComponent<RectTransform>();

        // 셀을 컨테이너의 자식으로 설정
        rectTransform.SetParent(cellContainer.transform, false);

        // 셀의 앵커 설정
        float startX = (float)x / gridSize;
        float endX = (float)(x + 1) / gridSize;
        float startY = 1 - (float)(y + 1) / gridSize;
        float endY = 1 - (float)y / gridSize;

        rectTransform.anchorMin = new Vector2(startX, startY);
        rectTransform.anchorMax = new Vector2(endX, endY);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 스케일 강제로 1로 설정
        rectTransform.localScale = Vector3.one;

        // GridCell 컴포넌트 추가
        var cell = cellObj.AddComponent<GridCell>();
        cell.Initialize(new Vector2Int(x, y), this);
        cells[x, y] = cell;
    }

    // 특정 위치의 셀 가져오기
    public GridCell GetCell(Vector2Int position)
    {
        if (position.x >= 0 && position.x < gridSize &&
            position.y >= 0 && position.y < gridSize)
        {
            return cells[position.x, position.y];
        }
        return null;
    }

    // 마우스/터치 위치를 그리드 좌표로 변환
    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundImage, screenPosition, null, out localPoint);

        // 로컬 좌표를 0~1 범위로 정규화
        Vector2 normalizedPos = new Vector2(
            (localPoint.x + backgroundImage.rect.width * 0.5f) / backgroundImage.rect.width,
            (localPoint.y + backgroundImage.rect.height * 0.5f) / backgroundImage.rect.height
        );

        // 그리드 좌표 계산
        int x = Mathf.Clamp(Mathf.FloorToInt(normalizedPos.x * gridSize), 0, gridSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((1 - normalizedPos.y) * gridSize), 0, gridSize - 1);

        return new Vector2Int(x, y);
    }
}
