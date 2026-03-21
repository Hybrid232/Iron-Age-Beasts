using Godot;

public partial class Checkpoint : Area2D
{
	[Export] private Control _interactPrompt;
	private Player _activePlayer;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		_interactPrompt.Visible = false;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
			_interactPrompt.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			_activePlayer = null;
			_interactPrompt.Visible = false;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") && _activePlayer != null)
		{
			_activePlayer.SetRespawnPoint(GlobalPosition);
			_activePlayer.RespawnAndReset(); // Saves, heals, refills, and resets enemies
			GD.Print("Checkpoint activated!");
		}
	}
}
