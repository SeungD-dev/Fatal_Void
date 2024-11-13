using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler
{
    InventoryController inventoryController;
    ItemGrid itemGrid;

    private void Awake()
    {
        inventoryController = FindFirstObjectByType(typeof(InventoryController)) as InventoryController;
        itemGrid = GetComponent<ItemGrid>();

        // 시작할 때 바로 Grid 설정
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

    private void OnEnable()
    {
        // UI가 활성화될 때마다 Grid 설정
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 추가 Grid가 있을 경우를 위해 남겨둠
        inventoryController.SelectedItemGrid = itemGrid;
    }

    // OnPointerExit 제거
}
