using Godot;
using System;

public class HealthSystem
{
	private int maxHealth;
	private int maxStamina;
	private int currentHealth;
	private int currentStamina;
	private UI uiReference;

	private int softExhaustThreshold;
	private int hardExhaustThreshold;

	public int CurrentHealth => currentHealth;
	public int CurrentStamina => currentStamina;

	private float regenTimer = 0f;
	private float timeBeforeRegenStart = 1.0f;
	private float regenTickRate = 0.1f;
	private float currentRegenTick = 0f;
	private int regenAmount = 1;

	public HealthSystem(
		int maxHealth,
		int maxStamina,
		UI uiReference,
		int softThreshold,
		int hardThreshold)
	{
		this.maxHealth = maxHealth;
		this.maxStamina = maxStamina;
		this.uiReference = uiReference;

		softExhaustThreshold = softThreshold;
		hardExhaustThreshold = hardThreshold;

		currentHealth = maxHealth;
		currentStamina = maxStamina;

		uiReference?.InitializeHealth(maxHealth, currentHealth);
		uiReference?.InitializeStamina(maxStamina, currentStamina);
	}

	
	public void ChangeHealth(int amount)
	{
		currentHealth = Math.Clamp(currentHealth + amount, 0, maxHealth);
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

		uiReference?.UpdateStaminaDisplay(currentStamina);
	}

	public bool IsBelowSoftThreshold()
	{
		return currentStamina < softExhaustThreshold;
	}

	public bool CanAct()
	{
		return currentStamina >= hardExhaustThreshold;
	}

	public void Update(float dt)
	{
		regenTimer += dt;

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
