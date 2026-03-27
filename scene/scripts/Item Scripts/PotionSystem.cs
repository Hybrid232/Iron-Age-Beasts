using Godot;

public class PotionSystem
{
	private int maxPotions = 5;
	private int currentPotions;
	private int healAmount;
	
	private AudioStreamPlayer potionSFX;
	private AudioStream potionSFXFile;

	// How many potions the player has permanently unlocked (refill amount)
	private int unlockedPotions;

	private HealthSystem healthSystem;
	private UI uiReference;

	public int CurrentPotions => currentPotions;
	public int MaxPotions => maxPotions;

	// Unlock Potions
	public int UnlockedPotions => unlockedPotions;

	// StartingPotions defaults to 1, and also sets unlocked amount
	public PotionSystem(int healAmount, HealthSystem healthSystem, UI uiReference, int startingPotions = 1, 
						AudioStream potionSFXFile = null, 
						AudioStreamPlayer potionSFX = null)
	{
		this.healAmount = healAmount;
		this.healthSystem = healthSystem;
		this.uiReference = uiReference;
		this.potionSFXFile = potionSFXFile;
		this.potionSFX = potionSFX;

		unlockedPotions = Mathf.Clamp(startingPotions, 0, maxPotions);
		currentPotions = unlockedPotions;

		uiReference?.UpdatePotionDisplay(currentPotions);
		
		if (potionSFX != null && potionSFXFile != null)
		{
			potionSFX.Stream = potionSFXFile;
		}
	}

	public bool CanBuyPotion() => unlockedPotions < maxPotions;

	// Buying increases unlockedPotions AND currentPotions (capped)
	public bool TryBuyPotions(int amount)
	{
		if (amount <= 0) return true;
		if (unlockedPotions >= maxPotions) return false;

		unlockedPotions = Mathf.Min(unlockedPotions + amount, maxPotions);
		currentPotions = Mathf.Min(currentPotions + amount, unlockedPotions);

		uiReference?.UpdatePotionDisplay(currentPotions);
		GD.Print($"Potion bought! Unlocked: {unlockedPotions}/{maxPotions} | Current: {currentPotions}/{unlockedPotions}");
		return true;
	}

	public void TryUsePotion()
	{
		if (Input.IsActionJustPressed("use_potion") && currentPotions > 0)
		{
			healthSystem.ChangeHealth(healAmount);
			currentPotions--;

			uiReference?.UpdatePotionDisplay(currentPotions);
			GD.Print($"Potion used! Remaining: {currentPotions}");
			potionSFX?.Play();
		}
	}

	// Refill to unlocked potions (not max)
	public void RefillPotions()
	{
		currentPotions = unlockedPotions;
		uiReference?.UpdatePotionDisplay(currentPotions);
	}

	// Optional helper if you ever want to hard-set unlocked amount (save/load)
	public void SetUnlockedPotions(int amount)
	{
		unlockedPotions = Mathf.Clamp(amount, 0, maxPotions);
		currentPotions = Mathf.Clamp(currentPotions, 0, unlockedPotions);
		uiReference?.UpdatePotionDisplay(currentPotions);
	}
}
