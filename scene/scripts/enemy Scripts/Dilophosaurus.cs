using Godot;

public partial class Dilophosaurus : BaseEnemy
{
	[Export] public Area2D HitArea;
	[Export] public float AttackAnimationDuration = 0.5f;
	[Export] public float PatrolRadius = 300f;
	[Export] public float SearchTime = 3.0f;

	private enum State { Patrol, Chase, Search, Return }
	private State _currentState = State.Patrol;

	private bool _isPerformingAttack = false;
	private float _attackAnimationTimer = 0f;
	private Vector2 _lungeDirection = Vector2.Zero;

	private Vector2 _lastSeenPosition;
	private float _searchTimer = 0f;
	private float _patrolAngle = 0f;

	public override void _Ready()
	{
		base._Ready();

		Speed = 60f;
		StopDistance = 8f;
		AttackRange = 60f;
		AttackDamage = 5;
		AttackCooldown = 1.5f;
		_attackCooldownTimer = 1.5f;

		_startPosition = GlobalPosition;

		if (HitArea != null)
		{
			HitArea.Position = Vector2.Zero;
			HitArea.Monitoring = false;
			HitArea.Monitorable = false;
			HitArea.BodyEntered += OnHitAreaBodyEntered;
		}
	}

	// ===== IMPORTANT: let boss force this enemy to chase immediately =====
	public override void ForceAggro(Player player)
	{
		base.ForceAggro(player);
		_currentState = State.Chase;
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

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		switch (_currentState)
		{
			case State.Patrol:
				PatrolCircle(dt);
				break;
			case State.Chase:
				base._PhysicsProcess(delta);
				break;
			case State.Search:
				Search(dt);
				break;
			case State.Return:
				ReturnToStart(dt);
				break;
		}
	}

	private void PatrolCircle(float dt)
	{
		_patrolAngle += dt * 0.8f;

		Vector2 target = _startPosition + new Vector2(
			Mathf.Cos(_patrolAngle) * PatrolRadius,
			Mathf.Sin(_patrolAngle) * PatrolRadius
		);

		Vector2 direction = (target - GlobalPosition).Normalized();
		Velocity = direction * (Speed * 0.5f);
		MoveAndSlide();
	}

	private void Search(float dt)
	{
		Velocity = Vector2.Zero;
		MoveAndSlide();

		_searchTimer -= dt;
		if (_searchTimer <= 0f)
			_currentState = State.Return;
	}

	private void ReturnToStart(float dt)
	{
		float distToStart = GlobalPosition.DistanceTo(_startPosition);

		if (distToStart <= 10f)
		{
			_currentState = State.Patrol;
			return;
		}

		Vector2 direction = (_startPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed;
		MoveAndSlide();
	}

	protected override void OnPlayerDetected(Node2D player)
	{
		base.OnPlayerDetected(player);
		_currentState = State.Chase;
	}

	protected override void OnPlayerLost(Node2D player)
	{
		base.OnPlayerLost(player);

		_lastSeenPosition = player.GlobalPosition;
		_searchTimer = SearchTime;
		_currentState = State.Search;
	}

	protected override bool CanAttack()
	{
		return !_isPerformingAttack && _attackCooldownTimer <= 0f;
	}

	protected override void OnAttackStart()
	{
		_isPerformingAttack = true;
		_attackAnimationTimer = AttackAnimationDuration;

		if (HitArea != null && _player != null)
		{
			HitArea.Position = Vector2.Zero;
			_lungeDirection = (_player.GlobalPosition - GlobalPosition).Normalized();

			GetTree().CreateTimer(0.15f).Timeout += () =>
			{
				if (HitArea != null && _isPerformingAttack)
				{
					HitArea.Position = _lungeDirection * 20f;
					HitArea.Monitoring = true;
				}
			};

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

	private void OnHitAreaBodyEntered(Node2D body)
	{
		if (!body.IsInGroup(PlayerGroup))
			return;

		if (body is IDamageable damageable)
			damageable.TakeDamage(AttackDamage);

		Vector2 knockbackDir = (body.GlobalPosition - GlobalPosition).Normalized();
		if (body is Player player)
			player.TriggerHitRecoil(knockbackDir);

		if (HitArea != null)
			HitArea.Monitoring = false;
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

	public override void ResetEnemy()
	{
		base.ResetEnemy();
		_currentState = State.Patrol;
	}
}
