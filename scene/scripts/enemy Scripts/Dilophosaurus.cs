// Dilophosaurus.cs
using Godot;
public partial class Dilophosaurus : BaseEnemy
{
	[Export] public Area2D HitArea;
	[Export] public float AttackAnimationDuration = 0.5f;
	
	private bool _isPerformingAttack = false;
	private float _attackAnimationTimer = 0f;
	
	public override void _Ready()
	{   
		base._Ready();
		
		Speed = 60f;
		StopDistance = 8f;
		AttackRange = 30f;  // Increased so attack triggers earlier
		AttackDamage = 15;
		AttackCooldown = 2.0f; // Attack every 2 seconds
		
		if (HitArea != null)
		{
			HitArea.Monitoring = false;
			HitArea.Monitorable = false;
			HitArea.BodyEntered += OnHitAreaBodyEntered;
			GD.Print($"HitArea mask: {HitArea.CollisionMask}");
		}
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		
		//if (_chasing) {
				//GD.Print($"CooldownTimer: {_attackCooldownTimer:F2} | CanAttack: {CanAttack()} | Distance: {GlobalPosition.DistanceTo(_player.GlobalPosition):F1} | AttackRange: {AttackRange}");
//
		//}
		
		if (_isPerformingAttack)
		{
			_attackAnimationTimer -= (float)delta;
			if (_attackAnimationTimer >= 1.5f)
			{
				_isPerformingAttack = false;
				if (HitArea != null)
					HitArea.Monitoring = false;
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
			Vector2 dirToPlayer = (_player.GlobalPosition - GlobalPosition).Normalized();
			float swipeDistance = 16f;
			Vector2 newPos = dirToPlayer * swipeDistance;
			
			HitArea.Position = newPos;
			GD.Print($"🟥 Dir: {dirToPlayer}");
			GD.Print($"🟥 HitArea new position: {newPos}");
			GD.Print($"🟥 HitArea actual position after set: {HitArea.Position}");
			
			HitArea.Monitoring = true;
		}
	}



	private void OnHitAreaBodyEntered(Node2D body)
	{
		GD.Print($"🟥 HitArea body entered: {body.Name}");
		
		if (!body.IsInGroup(PlayerGroup))
		{
			GD.Print($"❌ {body.Name} not in player group!");
			return;
		}

		//GD.Print($"✅ HitArea hit player!");

		if (body is IDamageable damageable)
			damageable.TakeDamage(AttackDamage);

		Vector2 knockbackDir = (body.GlobalPosition - GlobalPosition).Normalized();
		if (body is Player player)
			player.TriggerHitRecoil(knockbackDir);

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
