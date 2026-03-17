using Godot;

public class PotionSystem
{
    private int maxPotions = 5;
    private int currentPotions;
    private int healAmount;
    
    private HealthSystem healthSystem;
    private UI uiReference;

    public PotionSystem(int healAmount, HealthSystem healthSystem, UI uiReference)
    {
        this.healAmount = healAmount;
        this.healthSystem = healthSystem;
        this.uiReference = uiReference;
        
        currentPotions = maxPotions;
        uiReference?.UpdatePotionDisplay(currentPotions);
    }

    public void TryUsePotion()
    {
        if (Input.IsActionJustPressed("use_potion") && currentPotions > 0)
        {
            healthSystem.ChangeHealth(healAmount); // Heals the player
            currentPotions--;
            
            uiReference?.UpdatePotionDisplay(currentPotions);
		    GD.Print($"Potion used! Remaining: {currentPotions}");
        }
    }

    // TODO Call this later when you implement checkpoints
    public void RefillPotions() 
    {
        currentPotions = maxPotions;
        uiReference?.UpdatePotionDisplay(currentPotions);
    }
}