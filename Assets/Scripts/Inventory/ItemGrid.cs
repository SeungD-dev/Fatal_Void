using UnityEngine;
using System;

/// <summary>
/// 그리드 기반 인벤토리 시스템을 관리하는 클래스
/// 아이템의 배치, 이동, 검증을 처리
/// </summary>
public class ItemGrid : MonoBehaviour
{
    #region Constants
    public const float TILE_SIZE = 32f;
    #endregion

    #region Serialized Fields
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    #endregion

    #region Events
    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action OnGridChanged;
    #endregion

    #region Properties
    public int Width => gridWidth;
    public int Height => gridHeight;
    public bool IsInitialized => isInitialized;
    #endregion

    #region Private Fields
    private InventoryItem[,] gridItems;
    private RectTransform rectTransform;
    private bool isInitialized = false;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
        InitializeGrid();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            InitializeGrid();
        }
        ValidateGridState();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform component missing!");
            return;
        }
    }

    private void InitializeGrid()
    {
        try
        {
            // gridItems가 null이거나 크기가 다르면 새로 초기화
            if (gridItems == null || gridItems.GetLength(0) != gridWidth || gridItems.GetLength(1) != gridHeight)
            {
                gridItems = new InventoryItem[gridWidth, gridHeight];
                Debug.Log($"Grid initialized with size: {gridWidth}x{gridHeight}");
            }
            UpdateGridSize();
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing grid: {e.Message}");
            isInitialized = false;
        }
    }
    private void UpdateGridSize()
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(
                gridWidth * TILE_SIZE,
                gridHeight * TILE_SIZE
            );
        }
    }
    #endregion

    #region Grid Operations
    /// <summary>
    /// 스크린 좌표를 그리드 좌표로 변환
    /// </summary>
    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 gridTopLeft = corners[1];

        Vector2 positionInGrid = screenPosition - gridTopLeft;
        positionInGrid /= rectTransform.lossyScale.x;

        return new Vector2Int(
            Mathf.FloorToInt(positionInGrid.x / TILE_SIZE),
            Mathf.FloorToInt(-positionInGrid.y / TILE_SIZE)
        );
    }

    /// <summary>
    /// 아이템을 지정된 위치에 배치
    /// </summary>
    public bool PlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!CanPlaceItem(item, position)) return false;

        // 기존 아이템 제거
        CleanupItemReferences(item);

        // 새 위치에 아이템 배치
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                Vector2Int gridPos = position + new Vector2Int(x, y);
                gridItems[gridPos.x, gridPos.y] = item;
            }
        }

        item.SetGridPosition(position);
        UpdateItemVisualPosition(item, position);

        OnItemAdded?.Invoke(item);
        OnGridChanged?.Invoke();

        return true;
    }

    // 오버로드된 메서드들
    public bool PlaceItem(InventoryItem item, int x, int y)
    {
        return PlaceItem(item, new Vector2Int(x, y));
    }

    public bool PlaceItem(InventoryItem item, Vector2Int position, ref InventoryItem overlapItem)
    {
        overlapItem = GetItem(position);
        return PlaceItem(item, position);
    }

    public bool PlaceItem(InventoryItem item, int x, int y, ref InventoryItem overlapItem)
    {
        return PlaceItem(item, new Vector2Int(x, y), ref overlapItem);
    }
    /// <summary>
    /// 지정된 위치에서 아이템 제거
    /// </summary>
    public InventoryItem RemoveItem(Vector2Int position)
    {
        InventoryItem item = GetItem(position);
        if (item == null) return null;

        // 아이템이 차지하는 모든 칸에서 참조 제거
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                Vector2Int gridPos = position + new Vector2Int(x, y);
                if (IsValidPosition(gridPos))
                {
                    gridItems[gridPos.x, gridPos.y] = null;
                }
            }
        }

        OnItemRemoved?.Invoke(item);
        OnGridChanged?.Invoke();

        return item;
    }

    public InventoryItem PickUpItem(Vector2Int position)
    {
        InventoryItem item = GetItem(position.x, position.y);
        if (item == null) return null;

        RemoveItem(position);
        return item;
    }

    /// <summary>
    /// 아이템을 배치할 수 있는지 검사
    /// </summary>
    public bool CanPlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!isInitialized)
        {
            Debug.LogError("Grid is not initialized!");
            return false;
        }

        if (item == null)
        {
            Debug.LogError("Attempted to check placement for null item!");
            return false;
        }

        try
        {
            // 그리드 범위 체크
            if (position.x < 0 || position.y < 0 ||
                position.x + item.Width > gridWidth ||
                position.y + item.Height > gridHeight)
            {
                return false;
            }

            // gridItems null 체크
            if (gridItems == null)
            {
                Debug.LogError("gridItems array is null!");
                return false;
            }

            // 겹침 체크
            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    Vector2Int checkPos = position + new Vector2Int(x, y);
                    if (gridItems[checkPos.x, checkPos.y] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CanPlaceItem: {e.Message}\nStackTrace: {e.StackTrace}");
            return false;
        }
    }
    public bool CanPlaceItem(InventoryItem item, int x, int y)
    {
        return CanPlaceItem(item, new Vector2Int(x, y));
    }
    /// <summary>
    /// 그리드 내 빈 공간 찾기
    /// </summary>
    public Vector2Int? FindSpaceForObject(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogError("Attempted to find space for null item!");
            return null;
        }

        try
        {
            for (int y = 0; y < gridHeight - item.Height + 1; y++)
            {
                for (int x = 0; x < gridWidth - item.Width + 1; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (CanPlaceItem(item, position))
                    {
                        return position;
                    }
                }
            }

            Debug.Log("No free space found in grid");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in FindSpaceForObject: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// 지정된 위치의 아이템 반환
    /// </summary>
    /// 
    public void ForceInitialize()
    {
        InitializeComponents();
        InitializeGrid();
    }
    public InventoryItem GetItem(Vector2Int position)
    {
        return GetItem(position.x, position.y);
    }

    public InventoryItem GetItem(int x, int y)
    {
        if (!IsValidPosition(new Vector2Int(x, y)))
        {
            return null;
        }
        return gridItems[x, y];
    }

    /// <summary>
    /// 위치가 그리드 범위 내인지 확인
    /// </summary>
    public bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridWidth &&
               position.y >= 0 && position.y < gridHeight;
    }

    /// <summary>
    /// 아이템의 그리드상 위치 계산
    /// </summary>
    public Vector2 CalculatePositionOnGrid(InventoryItem item, int posX, int posY)
    {
        if (item == null) return Vector2.zero;

        return new Vector2(
            posX * TILE_SIZE + (item.Width * TILE_SIZE / 2),
            -(posY * TILE_SIZE + (item.Height * TILE_SIZE / 2))
        );
    }

    /// <summary>
    /// 그리드 상태 검증
    /// </summary>
    public void ValidateGridState()
    {
        if (!IsInitialized)
        {
            ForceInitialize();
        }

        try
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    InventoryItem item = gridItems[x, y];
                    if (item != null)
                    {
                        Vector2Int itemPos = item.GridPosition;
                        // 아이템의 위치가 유효하지 않으면 재배치 시도
                        if (!IsValidPosition(itemPos))
                        {
                            Vector2Int? newPos = FindSpaceForObject(item);
                            if (newPos.HasValue)
                            {
                                PlaceItem(item, newPos.Value);
                            }
                            else
                            {
                                gridItems[x, y] = null;
                            }
                        }
                    }
                }
            }
            OnGridChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ValidateGridState: {e.Message}");
        }
    }

    private void CleanupItemReferences(InventoryItem item)
    {
        if (item == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridItems[x, y] == item)
                {
                    gridItems[x, y] = null;
                }
            }
        }
    }

    private void UpdateItemVisualPosition(InventoryItem item, Vector2Int position)
    {
        if (item == null) return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            Vector2 gridPosition = CalculatePositionOnGrid(item, position.x, position.y);
            itemRect.localPosition = gridPosition;
        }
    }
    #endregion



    #region Debug Methods
    /// <summary>
    /// 그리드 상태 디버그 출력
    /// </summary>
    public void DebugPrintGrid()
    {
        string debug = "Grid State:\n";
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                debug += gridItems[x, y] != null ? "X " : "- ";
            }
            debug += "\n";
        }
        Debug.Log(debug);
    }
    #endregion
}