using Godot;

public partial class NPC : Node2D
{
	[Export] private Control _menu;
	[Export] private Label _dialogueLabel;
	[Export] private Button _speakButton; // I added this so that the game focuses on it when the menu is pulled up
	[Export] private AudioStreamPlayer booginsThemeSFX;
	[Export] private AudioStream booginsThemeFile;
	private Player _activePlayer;

	public override void _Ready()
	{
		// 1. Safety check to ensure exports aren't null
		if (_menu == null || _dialogueLabel == null || _speakButton == null)
		{
			GD.PrintErr("NPC Error: Please assign all Exported nodes in the Inspector!");
		}

		_menu.Visible = false; // Hide menu on start
	}

	public override void _Input(InputEvent @event)
	{
		// 2. Check if the "interact" action was just pressed
		if (@event.IsActionPressed("interact") && _activePlayer != null)
		{
			_menu.Visible = !_menu.Visible; // Toggle menu
			_activePlayer.CanMove = !_menu.Visible; // Disable player movement when menu is open
			_dialogueLabel.Text = ""; // Clear dialogue when opening menu
			

			if (_menu.Visible)
			{
				//focus on the speak button so that the player can immediately interact with it using keyboard/controller
				_speakButton.GrabFocus();
				
				// Work in Progress - Boogin's theme
				booginsThemeSFX.Stream = booginsThemeFile;
				booginsThemeSFX.Play();
			}
		}
	}

	// --- Signal Connections (Ensure these are connected in the Editor!) ---

	private void OnAreaBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
			GD.Print("Player entered interaction range.");
		}
	}

	private void OnAreaBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			GD.Print("Player left interaction range.");
			
			// Force reset state if player walks away
			if (_activePlayer != null)
			{
				_activePlayer.CanMove = true; 
			}
			
			_activePlayer = null;
			_menu.Visible = false;
		}
	}

	private void OnSpeakPressed()
	{
		_dialogueLabel.Text = "Hello! I am CHUNKY boogins! I am so FAT, but I love you buddy!";
	}

	private void OnPurchasePressed()
	{
		_dialogueLabel.Text = "I don't have anything to sell yet. Sorry lil bro.";
	}
}
