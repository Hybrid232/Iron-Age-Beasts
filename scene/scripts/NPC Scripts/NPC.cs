using Godot;

public partial class NPC : Node2D
{
	[Export] private Control _menu;
	[Export] private Label _dialogueLabel;
	[Export] private Button _speakButton;

	// Assign this to your HUD's InteractPrompt (CanvasLayer/Control) in the Inspector
	[Export] private Control _interactPrompt;

	[Export] private AudioStreamPlayer booginsThemeSFX;
	[Export] private AudioStream booginsThemeFile;

	private Player _activePlayer;
	
	private readonly RandomNumberGenerator _rng = new();

	[Export(PropertyHint.MultilineText)]
	private string[] _randomDialogues =
	{
		"Hello! I am CHUNKY boogins!",
		"I am so FAT, but I love you buddy!",
		"You got XP? I got deals.",
		"Pick ONE upgrade. Choose wisely, lil bro.",
		"Potions capped at 5. Don’t waste my time."
	};

	public override void _Ready()
	{
		if (_menu == null || _dialogueLabel == null || _speakButton == null || _interactPrompt == null)
			GD.PrintErr("NPC Error: Please assign all Exported nodes in the Inspector!");

		_menu.Visible = false;
		_interactPrompt.Visible = false;
		_rng.Randomize();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") && _activePlayer != null)
		{
			_menu.Visible = !_menu.Visible;
			_activePlayer.CanMove = !_menu.Visible;
			_dialogueLabel.Text = "";

			// Optional: hide the prompt while menu is open
			_interactPrompt.Visible = !_menu.Visible;

			if (_menu.Visible)
			{
				_speakButton.GrabFocus();
				booginsThemeSFX.Stream = booginsThemeFile;
				booginsThemeSFX.Play();
			}
		}
	}

	private void OnAreaBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
			if (!_menu.Visible)
				_interactPrompt.Visible = true;
		}
	}

	private void OnAreaBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			if (_activePlayer != null)
				_activePlayer.CanMove = true;

			_activePlayer = null;
			_menu.Visible = false;
			_interactPrompt.Visible = false;
		}
	}

	private void OnSpeakPressed()
	{
		if (_randomDialogues == null || _randomDialogues.Length == 0)
		{
			_dialogueLabel.Text = "...";
			return;
		}

		int i = _rng.RandiRange(0, _randomDialogues.Length - 1);
		_dialogueLabel.Text = _randomDialogues[i];
	}

	private void OnPurchasePressed()
	{
		_dialogueLabel.Text = "I don't have anything to sell yet. Sorry lil bro.";
	}
}
