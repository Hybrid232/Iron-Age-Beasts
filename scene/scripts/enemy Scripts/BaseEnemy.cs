// BaseEnemy.cs
using Godot;

public partial class BaseEnemy : CharacterBody2D
{
	[Export] public float Speed = 60f;
	[Export] public float StopDistance = 8f;
	[Export] public float AttackRange = 15f;
	[Export] public int AttackDamage = 10;
	[Export] public float AttackCooldown = 1.5f;
	[Export] public string PlayerGroup = "player";
	
	protected Node2D _player = null;
	protected bool _chasing = false;
	protected float _attackCooldownTimer = 0f;
	protected bool _isAttacking = false;
	
	public override void _Process(double delta)
	{
		// Update attack cooldown
		if (_attackCooldownTimer > 0f)
		{
			_attackCooldownTimer -= (float)delta;
		}
	}
	
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
		
		// Try to attack if in range and cooldown is ready
		if (CanAttack() && distance <= AttackRange)
		{
			Attack(_player);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}
		
		// Stop if close enough but can't attack yet
		if (distance <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}
		
		// Move toward player
		MoveTowardsTarget(_player, delta);
	}
	
	// Check if enemy can attack
	protected virtual bool CanAttack()
	{
		return !_isAttacking && _attackCooldownTimer <= 0f;
	}
	
	// Main attack method
	protected virtual void Attack(Node2D target)
	{
		_isAttacking = true;
		_attackCooldownTimer = AttackCooldown;
		
		OnAttackStart();
		DealDamage(target);
		OnAttackEnd();
		
		_isAttacking = false;
	}
	
	// Called when attack starts - override for animations/sounds
	protected virtual void OnAttackStart()
	{
		GD.Print("Enemy attacks!");
	}
	
	// Deal damage to target
	protected virtual void DealDamage(Node2D target)
	{
		if (target is IDamageable damageable)
		{
			damageable.TakeDamage(AttackDamage);
			GD.Print($"Dealt {AttackDamage} damage to {target.Name}");
		}
	}
	
	// Called when attack ends - override for cleanup
	protected virtual void OnAttackEnd()
	{
		// Override in derived classes for specific behavior
	}
	
	// Movement logic
	protected virtual void MoveTowardsTarget(Node2D target, double delta)
	{
		Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed;
		MoveAndSlide();
	}
	
	// Detection methods - call these from your Area2D signals
	protected virtual void OnPlayerDetected(Node2D player)
	{
		_player = player;
		_chasing = true;
		GD.Print("Player detected 👀");
	}
	
	protected virtual void OnPlayerLost(Node2D player)
	{
		_player = null;
		_chasing = false;
		GD.Print("Player left detection ❌");
	}
}
