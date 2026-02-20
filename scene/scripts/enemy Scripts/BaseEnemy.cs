// BaseEnemy.cs
using Godot;

public partial class BaseEnemy : CharacterBody2D, IDamageable
{
	[ExportGroup("Movement")]
	[Export] public float Speed = 60f;
	[Export] public float StopDistance = 8f;

	[ExportGroup("Combat")]
	[Export] public int MaxHealth = 50;
	[Export] public int AttackDamage = 20;
	[Export] public float AttackRange = 15f;
	[Export] public float AttackCooldown = 1.5f;
	[Export] public string PlayerGroup = "player";

	// State Variables
	protected int _currentHealth;
	protected float _attackCooldownTimer = 0f;
	protected bool _isAttacking = false;
	protected bool _chasing = false;
	protected Node2D _player = null;

	// Knockback State
	private float _knockbackTimer = 0f;
	private Vector2 _knockbackVelocity = Vector2.Zero;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
	}
	
	// Implement IDamageable interface
	public void TakeDamage(int damage)
	{
		_currentHealth -= damage;
		GD.Print($"{Name} took {damage} damage! Health: {_currentHealth}/{MaxHealth}");
		
		// Check if enemy died
		if (_currentHealth <= 0)
		{
			Die();
		}
		else
		{
			OnDamageTaken(damage);
		}
	}
	
	// Called when damage is taken but enemy survives
	protected virtual void OnDamageTaken(int damage)
	{
		// Override in derived classes for hit effects, sounds, etc.
		GD.Print($"{Name} was hit!");
	}
	
	// Called when enemy dies
	protected virtual void Die()
	{
		GD.Print($"{Name} has died!");
		// Play death animation, spawn drops, etc.
		QueueFree(); // Remove enemy from scene
	}

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
		float dt = (float)delta;

		// --- KNOCKBACK OVERRIDES AI ---
		if (_knockbackTimer > 0f)
		{
			_knockbackTimer -= dt;
			Velocity = _knockbackVelocity;
			MoveAndSlide();

			if (_knockbackTimer <= 0f)
			{
				_knockbackVelocity = Vector2.Zero;
				Velocity = Vector2.Zero;
			}
			return;
		}

		// If not chasing or player reference is gone, stop moving
		if (!_chasing || _player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Check if player still exists
		if (!IsInstanceValid(_player))
		{
			_player = null;
			_chasing = false;
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// Try to attack if in range
		if (CanAttack() && distance <= AttackRange)
		{
			Attack(_player);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Stop if close but can't attack
		if (distance <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Otherwise chase
		MoveTowardsTarget(_player, delta);
	}

	public void ApplyKnockback(Vector2 pushDir, float distance, float time)
	{
		if (pushDir == Vector2.Zero) return;

		float speed = (time > 0f) ? (distance / time) : 0f;
		_knockbackVelocity = pushDir.Normalized() * speed;
		_knockbackTimer = time;

		// Optional: cancel attack/chase "locking" if you want knockback to interrupt
		_isAttacking = false;
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
		GD.Print($"Checking target: {target.Name}"); // Is the enemy even touching the player?

		if (target is IDamageable damageable)
		{
			damageable.TakeDamage(AttackDamage);
			GD.Print($"SUCCESS: Dealt {AttackDamage} damage to {target.Name}");
		}
		else
		{
			GD.Print($"FAILURE: {target.Name} does not implement IDamageable!");
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
	
	// Link this to your Area2D (Hitbox) 'body_entered' signal in the editor

	// Inside BaseEnemy.cs

	public void _on_hurt_box_body_entered(Node2D body)
	{
		//GD.Print($"HurtBox touched something: {body.Name}"); // DEBUG 1
		// Ensure the body hit is the player and can be damaged
		if (body is IDamageable damageable && body.IsInGroup(PlayerGroup))
		{
			GD.Print($"{body.Name} is Damageable!"); // DEBUG 2
			// 1. Calculate direction for knockback (from Enemy to Player)
			Vector2 knockbackDirection = (body.GlobalPosition - GlobalPosition).Normalized();
			
			// 2. Deal damage using the managed variable
			damageable.TakeDamage(AttackDamage);
			
			// 3. Apply knockback to the player if they support it
			if (body is Player player)
			{
				player.TriggerHitRecoil(knockbackDirection);
			}
			
			GD.Print($"Hit Player for {AttackDamage} damage!");
		}
	}
}
