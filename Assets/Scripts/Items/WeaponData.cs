using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;
    public string weapongName;
    public Sprite icon;
    public WeaponRarity rarity;

    [Header("Invetory")]
    public Vector2Int size;
    public bool[,] shape;
    [TextArea]
    public string shapeLayout;

    [Header("Stats")]
    public float damage;
    public float attackSpeed;
    public float range;

    [Header("Drop Settings")]
    [Range(0, 100)]
    public float dropRate;

    private void OnValidate()
    {
        // shapeLayout 문자열을 shape 배열로 변환
        if (!string.IsNullOrEmpty(shapeLayout))
        {
            string[] rows = shapeLayout.Split('/');
            size = new Vector2Int(rows[0].Split(',').Length, rows.Length);
            shape = new bool[size.x, size.y];

            for (int y = 0; y < rows.Length; y++)
            {
                string[] cols = rows[y].Split(',');
                for (int x = 0; x < cols.Length; x++)
                {
                    shape[x, y] = cols[x] == "1";
                }
            }
        }
    }
}
