using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    public static float tileSizeWidth = 32f;
    public static float tileSizeHeight = 32f;

    InventoryItem[,] inventoryItemSlot;

    RectTransform rectTransform;

    [SerializeField] int gridSizeWidth;
    [SerializeField] int gridSizeHeight;
    public int Width => gridSizeWidth;
    public int Height => gridSizeHeight;

    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action OnGridChanged;

    public void NotifyGridChanged()
    {
        OnGridChanged?.Invoke();
    }
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Init(gridSizeWidth, gridSizeHeight);
    }

    private void OnEnable()
    {
        // UI가 활성화될 때마다 RectTransform 확인
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            Init(gridSizeWidth, gridSizeHeight);
        }
    }

    private void Init(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"Invalid grid size: {width}x{height}");
            return;
        }

        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
        else
        {
            Debug.LogError("RectTransform is null!");
        }
    }
    public Vector2Int GetTileGridPosition(Vector2 touchPosition)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform still null on {gameObject.name}");
                return Vector2Int.zero;
            }
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 gridTopLeft = corners[1];
        float scale = rectTransform.localScale.x;

        Vector2 positionFromTopLeft = touchPosition - gridTopLeft;
        positionFromTopLeft += new Vector2(tileSizeWidth * scale * 0.5f, tileSizeHeight * scale * 0.5f);
        Vector2 positionOnTheGrid = positionFromTopLeft / scale;

        Vector2Int rawGridPosition = new Vector2Int(
            Mathf.FloorToInt(positionOnTheGrid.x / tileSizeWidth),
            Mathf.FloorToInt(-positionOnTheGrid.y / tileSizeHeight)
        );

        // 터치한 위치에서 아이템을 찾음
        InventoryItem touchedItem = GetItemAtPosition(rawGridPosition);
        if (touchedItem != null)
        {
            // 아이템의 원점 위치를 반환
            return new Vector2Int(touchedItem.onGridPositionX, touchedItem.onGridPositionY);
        }

        return rawGridPosition;
    }

    private InventoryItem GetItemAtPosition(Vector2Int position)
    {
        // 경계 체크
        if (position.x < 0 || position.y < 0 || position.x >= gridSizeWidth || position.y >= gridSizeHeight)
        {
            return null;
        }

        return inventoryItemSlot[position.x, position.y];
    }

    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) return null;

        // 아이템 참조 정리
        CleanupItemReferences(toReturn);

        // 픽업 후 그리드 상태 검증
        ValidateGridState();

        OnItemRemoved?.Invoke(toReturn);
        NotifyGridChanged();

        return toReturn;
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (!BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            return false;
        }

        // 기존 참조 정리
        CleanupItemReferences(inventoryItem);

        if (!OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem))
        {
            return false;
        }

        if (overlapItem != null && overlapItem != inventoryItem)
        {
            CleanupItemReferences(overlapItem);
        }

        // 실제 배치는 private 메서드에 위임
        PlaceItem(inventoryItem, posX, posY);

        // 배치 후 그리드 상태 검증
        ValidateGridState();

        return true;
    }

    private void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        for (int x = 0; x < inventoryItem.WIDTH; x++)
        {
            for (int y = 0; y < inventoryItem.HEIGHT; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem;
            }
        }

        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;

        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);
        rectTransform.localPosition = position;

        OnItemAdded?.Invoke(inventoryItem);
        NotifyGridChanged();
    }
    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();


        position.x = posX * tileSizeWidth + (tileSizeWidth * inventoryItem.WIDTH / 2);
        position.y = -(posY * tileSizeHeight + (tileSizeHeight * inventoryItem.HEIGHT / 2));

        return position;
    }
    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        // 경계 체크
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        overlapItem = null;
        bool hasOverlap = false;

        // 아이템을 놓으려는 영역 전체를 검사
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem currentSlot = inventoryItemSlot[posX + x, posY + y];

                if (currentSlot != null)
                {
                    // 첫 번째 겹침을 발견한 경우
                    if (!hasOverlap)
                    {
                        overlapItem = currentSlot;
                        hasOverlap = true;
                    }
                    // 다른 아이템과 겹치는 경우
                    else if (currentSlot != overlapItem)
                    {
                        overlapItem = null;
                        return false;
                    }
                }
            }
        }

        // hasOverlap이 false인 경우 = 완전히 빈 공간
        // hasOverlap이 true인 경우 = 단일 아이템과만 겹침
        return true;
    }
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        // 경계 체크
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        // 해당 영역의 모든 칸이 비어있는지 확인
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 현재 검사하는 위치
                int checkX = posX + x;
                int checkY = posY + y;

                // 추가 경계 체크
                if (checkX >= gridSizeWidth || checkY >= gridSizeHeight)
                {
                    return false;
                }

                // 다른 아이템이 있는지 확인
                if (inventoryItemSlot[checkX, checkY] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void ValidateGridState()
    {
        // 모든 아이템의 참조를 임시로 저장
        HashSet<InventoryItem> processedItems = new HashSet<InventoryItem>();
        List<InventoryItem> invalidItems = new List<InventoryItem>();

        // 전체 그리드 순회하면서 상태 체크
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                InventoryItem item = inventoryItemSlot[x, y];
                if (item != null)
                {
                    // 아이템의 등록된 위치가 실제 위치와 일치하는지 확인
                    if (item.onGridPositionX != x || item.onGridPositionY != y)
                    {
                        // 이미 다른 위치에서 발견된 아이템인 경우
                        if (!processedItems.Contains(item))
                        {
                            invalidItems.Add(item);
                        }
                    }
                    processedItems.Add(item);
                }
            }
        }

        // 잘못된 참조를 가진 아이템들 처리
        foreach (var item in invalidItems)
        {
            CleanupItemReferences(item);
        }
    }

    private void CleanupItemReferences(InventoryItem item)
    {
        if (item == null) return;

        // 전체 그리드에서 해당 아이템의 참조 제거
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (inventoryItemSlot[x, y] == item)
                {
                    inventoryItemSlot[x, y] = null;
                }
            }
        }
    }
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (posX < 0 || posY < 0)
        {
            //Debug.Log($"BoundryCheck failed: position ({posX}, {posY}) is negative");
            return false;
        }

        if (posX + width > gridSizeWidth || posY + height > gridSizeHeight)
        {
            //Debug.Log($"BoundryCheck failed: item size {width}x{height} at ({posX}, {posY}) exceeds grid bounds {gridSizeWidth}x{gridSizeHeight}");
            return false;
        }

        return true;
    }

    public InventoryItem GetItem(int x, int y)
    {
        if (inventoryItemSlot == null)
        {
            //Debug.LogError($"inventoryItemSlot is null! Grid might not be initialized properly.");
            return null;
        }

        // 기본 경계 체크
        if (x < 0 || y < 0 || x >= gridSizeWidth || y >= gridSizeHeight)
        {
            //Debug.Log($"GetItem: Position ({x}, {y}) is outside grid bounds {gridSizeWidth}x{gridSizeHeight}");
            return null;
        }

        InventoryItem item = inventoryItemSlot[x, y];
        if (item == null)
        {
            return null;
        }

        // 아이템의 원점 위치가 유효한지 확인
        if (item.onGridPositionX < 0 || item.onGridPositionY < 0 ||
            item.onGridPositionX >= gridSizeWidth || item.onGridPositionY >= gridSizeHeight)
        {
            //Debug.LogWarning($"Item at ({x}, {y}) has invalid origin position: ({item.onGridPositionX}, {item.onGridPositionY})");
            return null;
        }

        // 아이템의 크기가 그리드를 벗어나는지 확인
        if (!BoundryCheck(item.onGridPositionX, item.onGridPositionY, item.WIDTH, item.HEIGHT))
        {
           // Debug.LogWarning($"Item at ({x}, {y}) extends beyond grid bounds");
            return null;
        }

        // 원점 위치의 아이템을 반환
        return inventoryItemSlot[item.onGridPositionX, item.onGridPositionY];
    }
    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        // itemToInsert의 크기가 유효한지 먼저 확인
        if (itemToInsert.WIDTH <= 0 || itemToInsert.HEIGHT <= 0)
        {
           // Debug.LogError($"Invalid item size: {itemToInsert.WIDTH}x{itemToInsert.HEIGHT}");
            return null;
        }

        // 그리드 내에서 아이템이 들어갈 수 있는 최대 범위 계산
        int maxY = gridSizeHeight - itemToInsert.HEIGHT + 1;
        int maxX = gridSizeWidth - itemToInsert.WIDTH + 1;

        if (maxX <= 0 || maxY <= 0)
        {
            //Debug.LogWarning($"Item size {itemToInsert.WIDTH}x{itemToInsert.HEIGHT} is too large for grid {gridSizeWidth}x{gridSizeHeight}");
            return null;
        }

        // 왼쪽 상단부터 순차적으로 검색
        for (int y = 0; y < maxY; y++)
        {
            for (int x = 0; x < maxX; x++)
            {
                // 각 위치에서 아이템의 전체 영역이 비어있는지 확인
                if (CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }
} 
