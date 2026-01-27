using Godot;

public partial class Dilophosaurus : CharacterBody2D
{
	[Export] public float Speed = 60f;
	[Export] public float StopDistance = 8f;
	[Export] public string PlayerGroup = "player";
	
	private Node2D _player = null;
	private bool _chasing = false;

	public override void _PhysicsProcess(double delta)
	{
		// If not chasing or player reference is gone, stop moving
		if (!_chasing || _player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Check if player still exists (in case it was freed)
		if (!IsInstanceValid(_player))
		{
			_player = null;
			_chasing = false;
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

	// Changed to PUBLIC so Godot can find it
	public void _on_detection_area_body_entered(Node2D body)
	{
		// Only react if it's the player
		if (!body.IsInGroup(PlayerGroup))
			return;

		_player = body;
		_chasing = true;
		GD.Print("Player detected 👀");
	}

	// Changed to PUBLIC so Godot can find it
	public void _on_detection_area_body_exited(Node2D body)
	{
		// Make sure it's actually the player leaving
		if (body != _player)
			return;

		_player = null;
		_chasing = false;
		GD.Print("Player left detection ❌");
	}
}
