// Dilophosaurus.cs
using Godot;
public partial class Dilophosaurus : BaseEnemy
{
	[Export] public Area2D HitArea;
	[Export] public float AttackAnimationDuration = 0.5f;
	
	private bool _isPerformingAttack = false;
	private float _attackAnimationTimer = 0f;
	private Vector2 _lungeDirection = Vector2.Zero;
	
	public override void _Ready()
	{   
		base._Ready();
		
		Speed = 60f;
		StopDistance = 8f;
		AttackRange = 80f;
		AttackDamage = 15;
		AttackCooldown = 1.5f;
		_attackCooldownTimer = 1.5f; // wait before first attack
		
		if (HitArea != null)
		{
			HitArea.Position = Vector2.Zero; // start overlapping dino
			HitArea.Monitoring = false;
			HitArea.Monitorable = false;
			HitArea.BodyEntered += OnHitAreaBodyEntered;
			GD.Print($"HitArea mask: {HitArea.CollisionMask}");
		}

		GD.Print($"AttackRange after _Ready: {AttackRange}");
		GD.Print($"AttackCooldown after _Ready: {AttackCooldown}");
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		
		if (_isPerformingAttack)
		{
			_attackAnimationTimer -= (float)delta;
			if (_attackAnimationTimer <= 0f)
			{
				_isPerformingAttack = false;
				if (HitArea != null)
				{
					HitArea.Position = Vector2.Zero;
					HitArea.Monitoring = false;
				}
			}
		}
	}

	protected override bool CanAttack()
	{
		return !_isPerformingAttack && _attackCooldownTimer <= 0f;
	}
	
	protected override void OnAttackStart()
	{
		GD.Print("🦖 Dilophosaurus claw swipe!");
		_isPerformingAttack = true;
		_attackAnimationTimer = AttackAnimationDuration;

		if (HitArea != null && _player != null)
		{
			// Start overlapping dino
			HitArea.Position = Vector2.Zero;

			// Capture direction toward player at moment of attack
			_lungeDirection = (_player.GlobalPosition - GlobalPosition).Normalized();

			// After short delay, shoot rectangle outward
			GetTree().CreateTimer(0.15f).Timeout += () =>
			{
				if (HitArea != null && _isPerformingAttack)
				{
					HitArea.Position = _lungeDirection * 20f;
					HitArea.Monitoring = true;
				}
			};

			// After attack window, retract back to center
			GetTree().CreateTimer(0.4f).Timeout += () =>
			{
				if (HitArea != null)
				{
					HitArea.Position = Vector2.Zero;
					HitArea.Monitoring = false;
				}
			};
		}
	}

	protected override void OnAttackEnd() { }

	private void OnHitAreaBodyEntered(Node2D body)
	{
		if (!body.IsInGroup(PlayerGroup))
			return;

		GD.Print($"✅ Claw hit {body.Name}!");

		if (body is IDamageable damageable)
			damageable.TakeDamage(AttackDamage);

		Vector2 knockbackDir = (_player.GlobalPosition - GlobalPosition).Normalized();
		if (body is Player player)
			player.TriggerHitRecoil(knockbackDir);

		// Disable after one hit per swing
		if (HitArea != null)
			HitArea.Monitoring = false;
	}

	protected override void MoveTowardsTarget(Node2D target, double delta)
	{
		Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed;
		MoveAndSlide();
	}

	public void _on_detection_area_body_entered(Node2D body)
	{
		if (!body.IsInGroup(PlayerGroup)) return;
		OnPlayerDetected(body);
	}
	
	public void _on_detection_area_body_exited(Node2D body)
	{
		if (body != _player) return;
		OnPlayerLost(body);
	}
}
