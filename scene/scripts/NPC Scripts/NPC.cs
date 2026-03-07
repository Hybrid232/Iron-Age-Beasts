using Godot;

public partial class NPC : Node2D
{
	[Export] private Control _menu;
	[Export] private Label _dialogueLabel;
	[Export] private Button _speakButton; 
	
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
			ToggleInteraction();
			
			// 3. Mark the input as handled so other systems don't use it
			GetViewport().SetInputAsHandled();
		}
	}

	private void ToggleInteraction()
	{
		GD.Print("Interacting with Boogins.");
		
		// Toggle menu visibility
		_menu.Visible = !_menu.Visible;
		
		// Disable player movement when menu is open
		if (_activePlayer != null)
		{
			_activePlayer.CanMove = !_menu.Visible;
		}

		// Clear dialogue and focus button if opening
		if (_menu.Visible)
		{
			_dialogueLabel.Text = "Waiting for Boogins to speak...";
			_speakButton.GrabFocus();
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
