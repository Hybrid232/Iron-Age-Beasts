// Dilophosaurus.cs
using Godot;

public partial class Dilophosaurus : BaseEnemy
{
	[Export] public float KnockbackForce = 300f;
	[Export] public Area2D AttackArea; // Drag your attack Area2D here
	[Export] public float AttackAnimationDuration = 0.3f;
	
	private bool _isPerformingAttack = false;
	private float _attackAnimationTimer = 0f;
	
	public override void _Ready()
	{	
		base._Ready();
		
		Speed = 60f;
		StopDistance = 8f;
		AttackRange = 15f;
		AttackDamage = 15;
		AttackCooldown = 1.5f;
		
		// Setup attack area
		if (AttackArea != null)
		{
			AttackArea.Monitoring = false; // Start disabled
			AttackArea.BodyEntered += OnAttackHit;
		}
	}
	
	public override void _Process(double delta)
	{
		base._Process(delta);
		
		// Handle attack animation
		if (_isPerformingAttack)
		{
			_attackAnimationTimer -= (float)delta;
			if (_attackAnimationTimer <= 0f)
			{
				_isPerformingAttack = false;
				if (AttackArea != null)
					AttackArea.Monitoring = false;
			}
		}
	}
	
	protected override void OnAttackStart()
	{
		GD.Print("🦖 Dilophosaurus bites ferociously!");
		
		// Enable attack hitbox
		_isPerformingAttack = true;
		_attackAnimationTimer = AttackAnimationDuration;
		
		if (AttackArea != null)
			AttackArea.Monitoring = true;
		
		// Play attack animation here
		// AnimationPlayer?.Play("bite_attack");
	}
	
	protected override void OnAttackEnd()
	{
		GD.Print("Dilophosaurus attack complete");
	}
	
	// This is called when the attack hitbox hits something
	private void OnAttackHit(Node2D body)
	{
		if (!body.IsInGroup(PlayerGroup)) return;
		
		GD.Print($"Attack hit {body.Name}!");
		
		// Damage is already handled by base DealDamage
		// Just apply knockback here
		Vector2 knockbackDir = (body.GlobalPosition - GlobalPosition).Normalized();
		
		if (body is Player player)
		{
			player.TriggerHitRecoil(knockbackDir);
		}
	}
	
	public void _on_detection_area_body_entered(Node2D body)
	{
		if (!body.IsInGroup(PlayerGroup))
			return;
		
		OnPlayerDetected(body);
	}
	
	public void _on_detection_area_body_exited(Node2D body)
	{
		if (body != _player)
			return;
		
		OnPlayerLost(body);
	}
}
