using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    [HideInInspector]
    private ItemGrid selectedItemGrid;
    public ItemGrid SelectedItemGrid 
    { 
        get => selectedItemGrid; 
        set {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        } 
    }
    WeaponManager weaponManager;
    InventoryItem selectedItem;
    InventoryItem overlapItem;

    RectTransform rectTransform;

    [SerializeField] List<WeaponData> weapons;
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform canvasTransform;
    [SerializeField] private WeaponInfoUI weaponInfoUI;

    InventoryHighlight inventoryHighlight;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
        weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();

    }

    private void Update()
    {
        ItemIconDrag();

       

        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            if (selectedItem == null)
            {
                CreateRandomItem();
            }         
        }

        if(Input.GetKeyDown(KeyCode.W))
        {
            InsertRandomItem();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }

        if (selectedItemGrid == null) 
        {
            inventoryHighlight.Show(false);
            return; 
        }
        HandleHighlight();
        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButtonPress();
        }

    }

    private void RotateItem()
    {
        if(selectedItemGrid == null) { return; }

        selectedItem.Rotate();
    }

    private void InsertRandomItem()
    {
        if (selectedItemGrid == null) { return; }
        CreateRandomItem();
        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;
        InsertItem(itemToInsert);
    }

    private void InsertItem(InventoryItem itemToInsert)
    {
        

       Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);
        if(posOnGrid == null) { return; }


        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    Vector2Int oldPosition;
    InventoryItem itemToHighlight;



    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();
        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);

                // WeaponInfo UI 업데이트
                if (weaponInfoUI != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(itemToHighlight.weaponData);
                }
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            // 아이템을 들고 있을 때도 WeaponInfo UI 업데이트
            if (weaponInfoUI != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }

            inventoryHighlight.Show(selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT));
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }


    private void CreateRandomItem()
    {
        InventoryItem inventoryItem = Instantiate(weaponPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;
        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        int selectedItemID = UnityEngine.Random.Range(0, weapons.Count);
        inventoryItem.Set(weapons[selectedItemID]);
    }

    private void LeftMouseButtonPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();

        if (selectedItem == null)
        {
            PickUpItem(tileGridPosition);
        }
        else
        {
            PutDownItem(tileGridPosition);
        }
    }

    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }

      return selectedItemGrid.GetTileGridPosition(position);
      
    }

    private void PutDownItem(Vector2Int tileGridPosition)
    {
        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);

        if (complete)
        {
            // 성공적으로 아이템을 배치했을 때
            if (overlapItem != null)
            {
                // 겹친 아이템이 있는 경우
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
            }
            else
            {
                // 겹친 아이템이 없고 성공적으로 배치된 경우
                // 이 시점에서 무기가 장착되었다고 판단
                OnWeaponEquipped(selectedItem);
                selectedItem = null;
            }

            // WeaponInfo UI 업데이트
            if (weaponInfoUI != null && selectedItem != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }
        }
    }



    private void PickUpItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();

            // WeaponInfo UI 업데이트
            if (weaponInfoUI != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }
        }
    }
    private void ItemIconDrag()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    public void CreatePurchasedItem(WeaponData weaponData)
    {
        InventoryItem inventoryItem = Instantiate(weaponPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;
        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        inventoryItem.Set(weaponData);
    }

    private void OnWeaponEquipped(InventoryItem item)
    {
        if (item != null && item.weaponData != null && weaponManager != null)
        {
            Debug.Log($"Equipping weapon: {item.weaponData.weaponName}");
            weaponManager.EquipWeapon(item.weaponData);
        }
    }

}
