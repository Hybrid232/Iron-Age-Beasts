using Godot;

public partial class NPC : Node2D
{
	[Export] private Control _menu;
	[Export] private Label _dialogueLabel;
	[Export] private Button _speakButton; // I added this so that the game focuses on it when the menu is pulled up
	private Player _activePlayer;

	public override void _Ready()
	{
		_menu.Visible = false; // Hide menu on start
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_activePlayer != null && Input.IsActionJustPressed("interact"))
		{
			_menu.Visible = !_menu.Visible; // Toggle menu
			_activePlayer.CanMove = !_menu.Visible; // Disable player movement when menu is open
			_dialogueLabel.Text = ""; // Clear dialogue when opening menu

			if (_menu.Visible)
			{
				//focus on the speak button so that the player can immediately interact with it using keyboard/controller
				_speakButton.GrabFocus();
			}
		}
	}

	private void OnAreaBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
		}
	}

	private void OnAreaBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			// Ensure player can move if they somehow leave while menu is open
			_activePlayer.CanMove = true; 
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
		_dialogueLabel.Text = "I don't have anything to sell yet. Sorry lil bro";
	}
}
