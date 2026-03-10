using Godot;
using System;

public partial class UI : CanvasLayer
{
	[Export] private TextureProgressBar healthBar;
	[Export] private TextureProgressBar staminaBar;
	[Export] private TextureRect _potionIcon;

	public void InitializeHealth(int maxHealth, int currentHealth)
	{
		if (healthBar == null) return;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = currentHealth;
	}
	public void InitializeStamina(int maxStamina, int currentStamina)
	{
		if (staminaBar == null) return;
		staminaBar.MaxValue = maxStamina;
		staminaBar.Value = currentStamina;
	}

	public void UpdateHealthDisplay(int currentHealth)
	{
		if (healthBar == null) return;
		healthBar.Value = currentHealth;
	}

	public void UpdateStaminaDisplay(int currentStamina)
	{
		if (staminaBar == null) return;
		staminaBar.Value = currentStamina;
	}

	public void UpdatePotionDisplay(bool hasPotion)
	{
		if (_potionIcon != null)
		{
			_potionIcon.Visible = hasPotion;
		}
		else
		{
			GD.PrintErr("Potion icon is not assigned in the UI script!");
		}
	}
}
