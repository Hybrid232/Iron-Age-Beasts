using Godot;

public partial class Dilophosaurus : CharacterBody2D
{
	[Export] public float Speed = 60f;          // How fast the enemy moves
	[Export] public float StopDistance = 8f;    // Distance before stopping near player
	[Export] public string PlayerGroup = "player"; // Player must be in this group

	private Node2D _player = null;  // Stores player reference
	private bool _chasing = false;  // Whether enemy should move

	public override void _PhysicsProcess(double delta)
	{
		// If not chasing or player reference is gone, stop moving
		if (!_chasing || _player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Distance to player
		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// Stop if close enough
		if (distance <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Move toward player
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed;

		MoveAndSlide();
	}

	// SIGNAL from detection_area when something enters
	private void _on_detection_area_body_entered(Node2D body)
	{
		// Only react if it's the player
		if (!body.IsInGroup(PlayerGroup))
			return;

		_player = body;
		_chasing = true;

		GD.Print("Player detected 👀");
	}

	// SIGNAL from detection_area when something leaves
	private void _on_detection_area_body_exited(Node2D body)
	{
		if (body != _player)
			return;

		_player = null;
		_chasing = false;

		GD.Print("Player left detection ❌");
	}
}
