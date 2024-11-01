using NUnit.Framework.Interfaces;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System;

public class InventoryItem : MonoBehaviour
{
    public WeaponData weaponData;

    public int HEIGHT
    {
        get
        {
            if(rotated == false)
            {
                return weaponData.height;
            }
            return weaponData.width;
        }
    }

    public int WIDTH
    {
        get
        {
            if(rotated == false)
            {
                return weaponData.width;
            }
            return weaponData.height;
        }
    }

    public int onGridPositionX;
    public int onGridPositionY;

    public bool rotated = false;

  

    internal void Set(WeaponData weaponData)
    {
        this.weaponData = weaponData;

        GetComponent<Image>().sprite = weaponData.weaponIcon;

        Vector2 size = new Vector2();
        size.x = weaponData.width * ItemGrid.tileSizeWidth;
        size.y = weaponData.height * ItemGrid.tileSizeHeight;
        GetComponent<RectTransform>().sizeDelta = size;
    }

    internal void Rotate()
    {
        rotated = !rotated;
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.rotation = Quaternion.Euler(0, 0, rotated == true ? 90f : 0f);
    }
}
