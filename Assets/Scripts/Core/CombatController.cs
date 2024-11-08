using System;
using Unity.VisualScripting;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GameManager.Instance.PlayerStats;
        if(playerStats != null)
        {
            playerStats.OnHealthChanged += HandleHealthChanged;
            playerStats.OnLevelUp += HandleLevelChanged;
            playerStats.OnExpChanged += HandleExpChanged;
            playerStats.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDestroy()
    {
        if(playerStats != null)
        {
            playerStats.OnHealthChanged -= HandleHealthChanged;
            playerStats.OnLevelUp -= HandleLevelChanged;
            playerStats.OnExpChanged -= HandleExpChanged;
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void HandleHealthChanged(float health)
    {
        UpdateHealthUI(health);
    }

    private void UpdateHealthUI(float health)
    {
        
    }

    private void HandleLevelChanged(int level) 
    {
        
    }

    private void HandleExpChanged(float exp)
    {

    }

    private void HandlePlayerDeath()
    {

    }
}
