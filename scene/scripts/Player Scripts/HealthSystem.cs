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
	private float regenTimer = 0f;
	private float timeBeforeRegenStart = 1.0f; // 1 second delay before regen starts
	private float regenTickRate = 0.1f; // How often to add stamina (smoothness)
	private float currentRegenTick = 0f;
	private int regenAmount = 1; // How much to add per tick

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
		if (amount < 0) 
		{
			regenTimer = 0f;
			currentRegenTick = 0f;
		}
		// GD.Print($"Stamina Changed: {currentStamina}/{maxStamina}"); //comment out to reduce console spam
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
	public void Update(float dt)
	{
		regenTimer += dt;

		// If enough time passed since last stamina usage
		if (regenTimer >= timeBeforeRegenStart)
		{
			currentRegenTick += dt;
			if (currentRegenTick >= regenTickRate)
			{
				ChangeStamina(regenAmount);
				currentRegenTick = 0f;
			}
		}
	}
}
