using Godot;
using System;

public partial class UI : CanvasLayer
{
    // Assign this in the Inspector by dragging the TextureProgressBar node here
    [Export] private TextureProgressBar healthBar; // <--- CHANGED THIS FROM ProgressBar

    // Called once when the game starts to set the bar size
    public void InitializeHealth(int maxHealth, int currentHealth)
    {
        if (healthBar == null) return;
        healthBar.MaxValue = maxHealth;
        healthBar.Value = currentHealth;
    }

    // Called whenever player takes damage/heals
    public void UpdateHealthDisplay(int currentHealth)
    {
        if (healthBar == null) return;
        healthBar.Value = currentHealth;
    }

    // Placeholder for your stamina method referenced in Player.cs
    public void UpdateStaminaDisplay(int currentStamina)
    {
        // Add a stamina bar later using the same logic as health
    }
}