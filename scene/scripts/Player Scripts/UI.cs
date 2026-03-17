using Godot;
using System;

public partial class UI : CanvasLayer
{
	[Export] private TextureProgressBar healthBar;
	[Export] private TextureProgressBar staminaBar;
	[Export] private TextureRect[] _potionIcons;

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

	public void UpdatePotionDisplay(int currentPotions)
	{
		if (_potionIcons == null) return;

		for (int i = 0; i < _potionIcons.Length; i++)
		{
			if (_potionIcons[i] != null)
			{
				// Shows the icon if its index is less than the remaining potions
				_potionIcons[i].Visible = i < currentPotions;
			}
		}
	} 
}
