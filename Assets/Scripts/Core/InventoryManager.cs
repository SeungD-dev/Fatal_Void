using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;
    public static InventoryManager Instance => instance;

    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    private bool[,] occupiedSpaces;
    private Dictionary<Vector2Int, InventoryItem> itemPositions;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    private void Awake()
    {
        instance = this;
        occupiedSpaces = new bool[gridWidth, gridHeight];
        itemPositions = new Dictionary<Vector2Int, InventoryItem>();
    }

    // 아이템을 놓을 수 있는지 확인
    public bool CanPlaceItem(WeaponData weapon, Vector2Int position, bool[,] rotatedShape)
    {
        int shapeWidth = rotatedShape.GetLength(0);
        int shapeHeight = rotatedShape.GetLength(1);

        // 경계 체크
        if (position.x < 0 || position.y < 0 ||
            position.x + shapeWidth > gridWidth ||
            position.y + shapeHeight > gridHeight)
            return false;

        // 겹침 체크
        for (int x = 0; x < shapeWidth; x++)
        {
            for (int y = 0; y < shapeHeight; y++)
            {
                if (rotatedShape[x, y])
                {
                    if (occupiedSpaces[position.x + x, position.y + y])
                        return false;
                }
            }
        }

        return true;
    }

    // 아이템 배치
    public bool PlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!CanPlaceItem(item.WeaponData, position, item.CurrentShape))
            return false;

        // 이전 위치에서 제거
        if (item.CurrentPosition != null)
            RemoveItem(item);

        // 새 위치에 배치
        int shapeWidth = item.CurrentShape.GetLength(0);
        int shapeHeight = item.CurrentShape.GetLength(1);

        for (int x = 0; x < shapeWidth; x++)
        {
            for (int y = 0; y < shapeHeight; y++)
            {
                if (item.CurrentShape[x, y])
                {
                    Vector2Int gridPos = position + new Vector2Int(x, y);
                    occupiedSpaces[gridPos.x, gridPos.y] = true;
                    itemPositions[gridPos] = item;
                }
            }
        }

        item.CurrentPosition = position;
        return true;
    }

    // 아이템 제거
    public void RemoveItem(InventoryItem item)
    {
        if (item.CurrentPosition == null)
            return;

        int shapeWidth = item.CurrentShape.GetLength(0);
        int shapeHeight = item.CurrentShape.GetLength(1);
        Vector2Int position = item.CurrentPosition.Value;

        for (int x = 0; x < shapeWidth; x++)
        {
            for (int y = 0; y < shapeHeight; y++)
            {
                if (item.CurrentShape[x, y])
                {
                    Vector2Int gridPos = position + new Vector2Int(x, y);
                    occupiedSpaces[gridPos.x, gridPos.y] = false;
                    itemPositions.Remove(gridPos);
                }
            }
        }
    }
}
