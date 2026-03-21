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

	public int MaxHealth => maxHealth;
	public int MaxStamina => maxStamina;

	private float regenTimer = 0f;
	private float timeBeforeRegenStart = 1.0f;
	private float regenTickRate = 0.1f;
	private float currentRegenTick = 0f;
	private int regenAmount = 1;

	// ===== Smooth health bar animation (visual only) =====
	private float _displayHealth; // what the bar shows

	// Tune these for feel (hp per second)
	private float _healAnimSpeedPerSecond = 150f;
	private float _damageAnimSpeedPerSecond = 100f;

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

		_displayHealth = currentHealth;

		uiReference?.InitializeHealth(maxHealth, currentHealth);
		uiReference?.InitializeStamina(maxStamina, currentStamina);
	}

	public void SetMaxHealth(int newMax, bool healToFull = false)
	{
		newMax = Math.Max(1, newMax);
		maxHealth = newMax;

		currentHealth = healToFull ? maxHealth : Math.Clamp(currentHealth, 0, maxHealth);

		_displayHealth = Math.Clamp(_displayHealth, 0, maxHealth);

		uiReference?.InitializeHealth(maxHealth, (int)MathF.Round(_displayHealth));
	}

	public void SetMaxStamina(int newMax, bool refillToFull = false)
	{
		newMax = Math.Max(1, newMax);
		maxStamina = newMax;

		currentStamina = refillToFull ? maxStamina : Math.Clamp(currentStamina, 0, maxStamina);

		uiReference?.InitializeStamina(maxStamina, currentStamina);
	}

	public void ChangeHealth(int amount)
	{
		// Gameplay changes immediately
		currentHealth = Math.Clamp(currentHealth + amount, 0, maxHealth);

		// Visual change is handled smoothly in Update()
		// (So both damage and healing animate rather than snap.)
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

	public void HealToFull()
	{
		currentHealth = maxHealth;

		// For respawn/bonfire, snap instantly (feels better than waiting).
		_displayHealth = currentHealth;
		uiReference?.UpdateHealthDisplay(currentHealth);
	}

	public void Update(float dt)
	{
		// ===== Animate health display toward real health (both up & down) =====
		if (_displayHealth < currentHealth)
		{
			_displayHealth = MathF.Min(currentHealth, _displayHealth + _healAnimSpeedPerSecond * dt);
			uiReference?.UpdateHealthDisplay((int)MathF.Round(_displayHealth));
		}
		else if (_displayHealth > currentHealth)
		{
			_displayHealth = MathF.Max(currentHealth, _displayHealth - _damageAnimSpeedPerSecond * dt);
			uiReference?.UpdateHealthDisplay((int)MathF.Round(_displayHealth));
		}

		// ===== existing stamina regen =====
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
