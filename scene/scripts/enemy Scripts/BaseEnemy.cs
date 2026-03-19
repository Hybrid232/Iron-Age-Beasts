// BaseEnemy.cs
using Godot;

public partial class BaseEnemy : CharacterBody2D, IDamageable
{
	[ExportGroup("Movement")]
	[Export] public float Speed = 60f;
	[Export] public float StopDistance = 8f;

	[ExportGroup("Combat")]
	[Export] public int MaxHealth = 50;
	public int AttackDamage = 20;
	[Export] public float AttackRange = 30f;
	[Export] public float AttackCooldown = 1.5f;
	public const string PLAYER_GROUP = "Player";
	[Export] public string PlayerGroup = PLAYER_GROUP;

	// State Variables
	protected int _currentHealth;
	protected float _attackCooldownTimer = 2f;
	protected bool _isAttacking = false;
	protected bool _chasing = false;

	// CHANGE: store the actual Player (prevents chasing a random node in the "Player" group)
	protected Player _player = null;

	// Knockback State
	private float _knockbackTimer = 0f;
	private Vector2 _knockbackVelocity = Vector2.Zero;

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
	}

	public virtual void TakeDamage(int damage)
	{
		_currentHealth -= damage;
		GD.Print($"{Name} took {damage} damage! Health: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0)
		{
			Die();
		}
		else
		{
			OnDamageTaken(damage);
		}
	}

	protected virtual void OnDamageTaken(int damage)
	{
		GD.Print($"{Name} was hit!");
	}
	
	

	protected virtual void Die()
	{
		QueueFree();
	}

	public override void _Process(double delta)
	{
		if (_attackCooldownTimer > 0f)
		{
			_attackCooldownTimer -= (float)delta;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

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

		if (!_chasing || _player == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (!IsInstanceValid(_player))
		{
			_player = null;
			_chasing = false;
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (CanAttack() && distance <= AttackRange)
		{
			Attack(_player);
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (distance <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		MoveTowardsTarget(_player, delta);
	}

	public void ApplyKnockback(Vector2 pushDir, float distance, float time)
	{
		if (pushDir == Vector2.Zero) return;

		float speed = (time > 0f) ? (distance / time) : 0f;
		_knockbackVelocity = pushDir.Normalized() * speed;
		_knockbackTimer = time;

		_isAttacking = false;
	}

	protected virtual bool CanAttack()
	{
		return !_isAttacking && _attackCooldownTimer <= 0f;
	}

	protected virtual void Attack(Node2D target)
	{
		_isAttacking = true;
		_attackCooldownTimer = AttackCooldown;

		OnAttackStart();
		DealDamage(target);
		OnAttackEnd();

		_isAttacking = false;
	}

	protected virtual void OnAttackStart() { }

	protected virtual void DealDamage(Node2D target)
	{
		if (target is IDamageable damageable)
			damageable.TakeDamage(AttackDamage);
	}

	protected virtual void OnAttackEnd() { }

	protected virtual void MoveTowardsTarget(Node2D target, double delta)
	{
		Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed;
		MoveAndSlide();
	}

	protected virtual void OnPlayerDetected(Node2D player)
	{
		// CHANGE: only accept actual Player
		if (player is not Player p) return;

		_player = p;
		_chasing = true;
	}

	protected virtual void OnPlayerLost(Node2D player)
	{
		if (player is not Player) return;

		_player = null;
		_chasing = false;
	}

	public void _on_hurt_box_body_entered(Node2D body)
	{
		if (body is IDamageable damageable && body.IsInGroup(PlayerGroup))
		{
			Vector2 knockbackDirection = (body.GlobalPosition - GlobalPosition).Normalized();
			damageable.TakeDamage(AttackDamage);

			if (body is Player player)
			{
				player.TriggerHitRecoil(knockbackDirection);
				GD.Print($"==========PUSHBACK============");
			}

			GD.Print($"Hit Player for {AttackDamage} damage!");
		}
	}
}
