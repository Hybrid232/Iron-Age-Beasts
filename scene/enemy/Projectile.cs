using Godot;

public partial class Projectile : Area2D
{
	[Export] public float Speed  = 300f;
	[Export] public int   Damage = 10;

	private Vector2 _direction = Vector2.Right;

	public void Initialize(Vector2 direction)
	{
		_direction = direction.Normalized();
		// Rotate sprite to face direction
		Rotation = _direction.Angle();
	}

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		// Auto-delete when leaves screen
		var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		notifier.ScreenExited += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		Position += _direction * Speed * (float)delta;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			player.TakeDamage(Damage);
			GD.Print($"[Projectile] Hit player for {Damage} damage!");
			QueueFree();
		}
		// Also delete if hits a wall
		else if (body is TileMapLayer || body is StaticBody2D)
		{
			QueueFree();
		}
	}
}
