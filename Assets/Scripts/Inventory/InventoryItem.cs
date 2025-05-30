using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리에서 사용되는 아이템을 관리하는 컴포넌트
/// 아이템의 크기, 회전, 그리드 위치 등을 처리
/// </summary>
public class InventoryItem : MonoBehaviour
{
    #region Fields
    [SerializeField] private Image itemImage;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private WeaponData itemData;

    private Vector2Int gridPosition = new Vector2Int(-1, -1);
    private bool isRotated;
    private readonly Vector2Int INVALID_POSITION = new Vector2Int(-1, -1);
    #endregion

    #region Properties
    #region Properties
    /// <summary>
    /// 아이템의 무기 데이터에 대한 공개 접근자
    /// </summary>
    public WeaponData WeaponData
    {
        get { return itemData; }
        private set { itemData = value; }
    }

  
    public int Width => isRotated ? itemData.height : itemData.width;

   
    public int Height => isRotated ? itemData.width : itemData.height;

    public Vector2Int GridPosition => gridPosition;

  
    public bool IsRotated => isRotated;

    public bool OnGrid => gridPosition.x >= 0 && gridPosition.y >= 0;


   
    public int onGridPositionX => gridPosition.x;
    public int onGridPositionY => gridPosition.y;
    #endregion


    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
        gridPosition = INVALID_POSITION;
    }

    private void OnValidate()
    {
        InitializeComponents();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 아이템 초기화
    /// </summary>
    /// <param name="data">무기 데이터</param>
    /// 
    public WeaponData GetWeaponData()
    {
        return itemData;
    }
    public void Initialize(WeaponData data)
    {
        if (data == null)
        {
            Debug.LogError("Attempted to initialize InventoryItem with null WeaponData!");
            return;
        }

        try
        {
            WeaponData = data;
            isRotated = false;
            gridPosition = INVALID_POSITION;
            UpdateVisuals();
            UpdateSize();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Initialize: {e.Message}");
        }
    }
    /// <summary>
    /// 아이템 회전
    /// </summary>
    public void Rotate()
    {
        isRotated = !isRotated;

        // 이미지만 회전하고 크기는 유지
        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, isRotated ? 90f : 0f);
        }
    }


    /// <summary>
    /// 그리드 위치 설정
    /// </summary>
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }
    /// <summary>
    /// 아이템 크기 반환
    /// </summary>
    public Vector2 GetWorldSize()
    {
        return new Vector2(
            Width * ItemGrid.TILE_SIZE,
            Height * ItemGrid.TILE_SIZE
        );
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 아이템의 시각적 요소 업데이트
    /// </summary>
    private void InitializeComponents()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
    }
    private void UpdateVisuals()
    {
        if (itemImage != null && WeaponData != null)
        {
            itemImage.sprite = WeaponData.GetColoredInventoryWeaponIcon();
            itemImage.color = WeaponData.GetTierColor();
        }
    }

    /// <summary>
    /// RectTransform 크기 업데이트
    /// </summary>
    private void UpdateSize()
    {
        if (rectTransform != null)
        {
            // 초기 크기 설정
            rectTransform.sizeDelta = new Vector2(
                itemData.width * ItemGrid.TILE_SIZE,   // Width 대신 직접 itemData.width 사용
                itemData.height * ItemGrid.TILE_SIZE   // Height 대신 직접 itemData.height 사용
            );
        }
    }

    /// <summary>
    /// 회전 적용
    /// </summary>


    #endregion
    #endregion
}