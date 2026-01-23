using Godot;
using System.Collections.Generic;

public partial class UI : CanvasLayer
{
    [Export] public Texture2D RedHeartTexture;
    [Export] public Texture2D BlackHeartTexture;
    [Export] public HBoxContainer HealthContainer;

    [Export] public Texture2D StaminaFullTexture;
    [Export] public Texture2D StaminaEmptyTexture;
    [Export] public HBoxContainer StaminaContainer;

    private List<TextureRect> _hearts = new List<TextureRect>();
    private List<TextureRect> _staminaIcons = new List<TextureRect>();

    public override void _Ready()
    {
        // Find all TextureRects inside the container and add them to our list
        foreach (Node child in HealthContainer.GetChildren())
        {
            if (child is TextureRect rect)
            {
                _hearts.Add(rect);
            }
        }
        // Initialize display to full health
        UpdateHealthDisplay(10);
        
        foreach (Node child in StaminaContainer.GetChildren())
        {
            if (child is TextureRect rect)
            {
                _staminaIcons.Add(rect);
            }
        }
        // Initialize Stamina to 5
        UpdateStaminaDisplay(5);
    }

    public void UpdateHealthDisplay(int currentHealth)
    {
        // Loop through all hearts
        for (int i = 0; i < _hearts.Count; i++)
        {
            // If the index is less than current health, it's Red (Alive)
            // Otherwise, it's Black (Damaged)
            if (i < currentHealth)
            {
                _hearts[i].Texture = RedHeartTexture;
            }
            else
            {
                _hearts[i].Texture = BlackHeartTexture;
            }
        }
    }
    public void UpdateStaminaDisplay(int currentStamina)
    {
        for (int i = 0; i < _staminaIcons.Count; i++)
        {
            if (i < currentStamina)
                _staminaIcons[i].Texture = StaminaFullTexture;
            else
                _staminaIcons[i].Texture = StaminaEmptyTexture;
        }
    }
}