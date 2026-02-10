using Godot;
using System;

public class HealthSystem
{
	private int maxHealth;
	private int maxStamina;
	private int currentHealth;
	private int currentStamina;
	private UI uiReference;

	public int CurrentHealth => currentHealth;
	public int CurrentStamina => currentStamina;

	public HealthSystem(int maxHealth, int maxStamina, UI uiReference)
	{
		this.maxHealth = maxHealth;
		this.maxStamina = maxStamina;
		this.uiReference = uiReference;

		currentHealth = maxHealth;
		currentStamina = maxStamina;

		if (uiReference != null)
		{
			uiReference.InitializeHealth(maxHealth, currentHealth);
			uiReference.InitializeStamina(maxStamina, currentStamina);
		}
	}

	public void ChangeHealth(int amount)
	{
		currentHealth = Math.Clamp(currentHealth + amount, 0, maxHealth);
		GD.Print($"Health Changed: {currentHealth}/{maxHealth}");
		uiReference?.UpdateHealthDisplay(currentHealth);
	}

	public void ChangeStamina(int amount)
	{
		currentStamina = Math.Clamp(currentStamina + amount, 0, maxStamina);
		GD.Print($"Stamina Changed: {currentStamina}/{maxStamina}");
		uiReference?.UpdateStaminaDisplay(currentStamina);
	}

	public void HandleDebugInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
		{
			if (eventKey.Keycode == Key.J) ChangeHealth(-5);
			if (eventKey.Keycode == Key.K) ChangeHealth(5);
			if (eventKey.Keycode == Key.U) ChangeStamina(-5);
			if (eventKey.Keycode == Key.I) ChangeStamina(5);
		}
	}
}
