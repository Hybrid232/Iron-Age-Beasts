using Godot;

public partial class Checkpoint : Area2D
{
	[Export] private Control _interactPrompt;
	private Player _activePlayer;

	[ExportGroup("Respawn")]
	[Export] private bool _respawnEnemiesOnRest = true;

	// Keep this FALSE so the tutorial boss doesn't get re-instanced or reset on rest.
	[Export] private bool _respawnBossOnRest = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		if (_interactPrompt != null)
			_interactPrompt.Visible = false;

		SetProcessUnhandledInput(false);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;

			if (_interactPrompt != null)
				_interactPrompt.Visible = true;

			SetProcessUnhandledInput(true);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			_activePlayer = null;

			if (_interactPrompt != null)
				_interactPrompt.Visible = false;

			SetProcessUnhandledInput(false);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("interact")) return;
		if (_activePlayer == null) return;

		_activePlayer.SetRespawnPoint(GlobalPosition);

		// Heal + refill potions (your Player handles this)
		_activePlayer.RespawnAndReset();

		// Dark Souls rest: respawn normal enemies
		if (_respawnEnemiesOnRest)
			RespawnAllEnemySpawners();

		// Prevent "interact" from leaking and triggering boss/UI logic elsewhere
		GetViewport().SetInputAsHandled();

		GD.Print("Checkpoint activated!");
	}

	private void RespawnAllEnemySpawners()
	{
		var spawners = GetTree().GetNodesInGroup(EnemySpawner.GROUP_NAME);
		if (spawners == null || spawners.Count == 0)
		{
			GD.PrintErr($"[Checkpoint] No nodes in group '{EnemySpawner.GROUP_NAME}'. Add EnemySpawner nodes.");
			return;
		}

		foreach (Node node in spawners)
		{
			if (node is not EnemySpawner spawner)
				continue;

			// Don't respawn boss unless explicitly allowed
			if (spawner.IsBossSpawner && !_respawnBossOnRest)
				continue;

			spawner.ForceRespawn();
		}
	}
}
