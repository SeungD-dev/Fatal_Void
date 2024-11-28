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

    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    public Vector2Int GetTileGridPosition(Vector2 touchPosition, Vector2 itemSize = default)
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

        Vector2 positionOnTheGrid = new Vector2();

        // 아이템 크기가 있는 경우 (아이템의 center pivot 고려)
        if (itemSize != Vector2.zero)
        {
            // 터치 위치에서 아이템 크기의 절반을 빼서 left-top 기준으로 변환
            touchPosition -= new Vector2(itemSize.x / 2, -itemSize.y / 2);
        }

        // 그리드의 로컬 좌표로 변환
        positionOnTheGrid.x = touchPosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - touchPosition.y;

        float scale = rectTransform.localScale.x;

        // 그리드 좌표 계산
        return new Vector2Int(
            (int)(positionOnTheGrid.x / (tileSizeWidth * scale)),
            (int)(positionOnTheGrid.y / (tileSizeHeight * scale))
        );
    }
    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) { return null; }

        CleanGridReference(toReturn);

        return toReturn;
    }

    private void CleanGridReference(InventoryItem item)
    {
        // 범위 체크 추가
        if (!BoundryCheck(item.onGridPositionX, item.onGridPositionY, item.WIDTH, item.HEIGHT))
        {
            Debug.LogWarning($"Attempted to clean grid reference outside bounds: pos({item.onGridPositionX}, {item.onGridPositionY}), size({item.WIDTH}, {item.HEIGHT})");
            return;
        }

        for (int ix = 0; ix < item.WIDTH; ix++)
        {
            for (int iy = 0; iy < item.HEIGHT; iy++)
            {
                if (item.onGridPositionX + ix < gridSizeWidth &&
                    item.onGridPositionY + iy < gridSizeHeight)
                {
                    inventoryItemSlot[item.onGridPositionX + ix, item.onGridPositionY + iy] = null;
                }
            }
        }
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        // 경계 체크
        if (!BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            Debug.LogWarning($"Cannot place item: position ({posX}, {posY}) is out of bounds");
            return false;
        }

        if (!OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem))
        {
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            // 겹친 아이템 제거 전에 범위 체크
            if (BoundryCheck(overlapItem.onGridPositionX, overlapItem.onGridPositionY,
                overlapItem.WIDTH, overlapItem.HEIGHT))
            {
                CleanGridReference(overlapItem);
            }
        }

        PlaceItem(inventoryItem, posX, posY);
        return true;
    }



    private void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        // 범위 체크 후 그리드에 아이템 설정
        if (BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
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
        }
    }
    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();

        // 그리드는 Left Top, 아이템은 Center 피벗이므로
        // 타일 크기의 절반을 더해서 중앙 정렬
        position.x = posX * tileSizeWidth + (tileSizeWidth * inventoryItem.WIDTH / 2);
        position.y = -(posY * tileSizeHeight + (tileSizeHeight * inventoryItem.HEIGHT / 2));

        return position;
    }
    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for(int x =0; x < width; x++)
        {
            for(int y= 0; y< height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    if(overlapItem == null)
                    {

                    overlapItem = inventoryItemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        if(overlapItem != inventoryItemSlot[posX + x, posY + y])
                        {                
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                   
                   return false;
                        
                    
                }
            }
        }

        return true;
    }

    bool PositionCheck(int posX, int posY)
    {
        if(posX < 0 || posY < 0) { return false; }

        if(posX >= gridSizeWidth || posY >= gridSizeHeight) {  return false; }

        return true;
    }

    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if(PositionCheck(posX, posY) == false) { return false;}

        posX += width -1;
        posY += height -1;
        if(PositionCheck(posX,posY) == false) { return false;}  

        return true;
    }

    public InventoryItem GetItem(int x, int y)
    {
        Debug.Log($"GetItem called for position ({x}, {y})");

        if (inventoryItemSlot == null || x < 0 || y < 0 || x >= gridSizeWidth || y >= gridSizeHeight)
        {
            Debug.Log($"Invalid grid access: slot array null or position out of bounds");
            return null;
        }

        InventoryItem item = inventoryItemSlot[x, y];
        Debug.Log($"Found item at position: {item != null}");

        if (item != null)
        {
            Debug.Log($"Item origin position: ({item.onGridPositionX}, {item.onGridPositionY})");
            return inventoryItemSlot[item.onGridPositionX, item.onGridPositionY];
        }

        return null;
    }
    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        int height = gridSizeHeight - itemToInsert.HEIGHT +1;
        int width = gridSizeWidth - itemToInsert.WIDTH +1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < gridSizeWidth; x++)
            {
                if(CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT) == true)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }
} 
