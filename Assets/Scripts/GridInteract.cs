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
        itemGrid = GetComponent<ItemGrid>();
        inventoryController = FindAnyObjectByType<InventoryController>();
        weaponInfoUI = FindAnyObjectByType<WeaponInfoUI>();

        if (itemGrid != null)
        {
            itemGrid.OnGridChanged += OnGridStateChanged;
            if (inventoryController != null)
            {
                inventoryController.SelectedItemGrid = itemGrid;
            }
        }
    }

    private void OnDestroy()
    {
        if (itemGrid != null)
        {
            itemGrid.OnGridChanged -= OnGridStateChanged;
        }
    }

    private void OnGridStateChanged() => weaponInfoUI?.RefreshUpgradeUI();
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

}