using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler
{
    InventoryController inventoryController;
    ItemGrid itemGrid;
    WeaponInfoUI weaponInfoUI;

    private void Awake()
    {
        inventoryController = FindFirstObjectByType(typeof(InventoryController)) as InventoryController;
        itemGrid = GetComponent<ItemGrid>();
        weaponInfoUI = FindFirstObjectByType<WeaponInfoUI>();

        if (itemGrid != null)
        {
            itemGrid.OnGridChanged += OnGridStateChanged;
        }

        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

    private void OnDestroy()
    {
        if (itemGrid != null)
        {
            itemGrid.OnGridChanged -= OnGridStateChanged;
        }
    }

    private void OnGridStateChanged()
    {
        if (weaponInfoUI != null)
        {
            weaponInfoUI.RefreshUpgradeUI();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryController.SelectedItemGrid = itemGrid;
    }
}