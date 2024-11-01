using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event System.Action OnLevelUp;
    private int level = 1;
    private float currentExp = 0;
    private float requiredExp = 100;

    public void AddExperience(float exp)
    {
        currentExp += exp;
        if (currentExp >= requiredExp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        currentExp -= requiredExp;
        requiredExp *= 1.2f;
        OnLevelUp?.Invoke();
    }
}
