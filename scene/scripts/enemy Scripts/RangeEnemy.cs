using Godot;
using System.Collections.Generic;

public partial class RangeEnemy : BaseEnemy
{
	// ── Inspector Tuning ─────────────────────────────────────────
	[ExportGroup("Detection")]
	[Export] public float DetectionRange = 200f;

	[ExportGroup("Range Behaviour")]
	[Export] public float PreferredDistance = 120f;
	[Export] public float TooCloseDistance  = 60f;
	[Export] public float AlertDuration     = 0.6f;
	[Export] public float LoseSightDuration = 2f;
	
	[ExportGroup("Combat")]
	[Export] public PackedScene ProjectileScene;
	
	// ── Patrol ───────────────────────────────────────────────────
	[ExportGroup("Patrol")]
	[Export] public float PatrolRadius    = 80f;  
	[Export] public float PatrolSpeed     = 30f;  
	[Export] public float PatrolWaitTime  = 1.5f;  

	private Vector2 _patrolTarget        = Vector2.Zero;
	private float   _patrolWaitTimer     = 0f;
	private bool    _waitingAtPoint      = false;

	// ── State Machine ────────────────────────────────────────────
	private enum State { Idle, Alert, Attack, Reposition, LoseSight, Dead }
	private State _state = State.Idle;

	// ── Timers ───────────────────────────────────────────────────
	private float _alertTimer    = 0f;
	private float _loseSightTimer = 0f;

	// ── Nodes ────────────────────────────────────────────────────
	private Area2D          _detectionArea;
	private Node2D          _rayCastOrigin;
	private List<RayCast2D> _rays = new();
	private Sprite2D _sprite;

	// ── Tracking ─────────────────────────────────────────────────
	private bool _playerInArea = false;  // broad-phase flag

	public override void _Ready()
	{
		base._Ready();
		PickNewPatrolTarget();
		GD.Print("[RangeEnemy] _Ready called!");
		_detectionArea = GetNode<Area2D>("detection_area");
		_rayCastOrigin = GetNode<Node2D>("RayCastOrigin");
		_sprite = GetNode<Sprite2D>("RangeEnemy");

		GD.Print($"[RangeEnemy] Nodes found: detectionArea={_detectionArea != null}, rayCastOrigin={_rayCastOrigin != null}");

		foreach (Node child in _rayCastOrigin.GetChildren())
		{
			if (child is RayCast2D ray)
				_rays.Add(ray);
		}

		GD.Print($"[RangeEnemy] {_rays.Count} rays loaded.");

		_detectionArea.BodyEntered += OnDetectionBodyEntered;
		_detectionArea.BodyExited  += OnDetectionBodyExited;
	}
	
	private void UpdateFacingDirection(Vector2 targetPosition)
	{
		_sprite.FlipH = targetPosition.X > GlobalPosition.X;
	}

	// ── Main Loop ────────────────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Only run raycasts if player is in the broad-phase area
		if (_playerInArea && _player != null)
			CheckLineOfSight();

		switch (_state)
		{
			case State.Idle:       HandleIdle();            break;
			case State.Alert:      HandleAlert(dt);         break;
			case State.Attack:     HandleAttack(dt);        break;
			case State.Reposition: HandleReposition();      break;
			case State.LoseSight:  HandleLoseSight(dt);     break;
			case State.Dead:                                break;
		}

		MoveAndSlide();
	}

	// ── Line of Sight (Raycast cone) ─────────────────────────────
	private void CheckLineOfSight()
	{
		bool seen = false;

		// Rotate the RayCastOrigin to face the player
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		_rayCastOrigin.Rotation = toPlayer.Angle();

		// Check if any ray hits the player
		foreach (RayCast2D ray in _rays)
		{
			if (!ray.IsColliding()) continue;
			if (ray.GetCollider() is Player)
			{
				seen = true;
				break;
			}
		}

		if (seen)
		{
			if (_state == State.Idle || _state == State.LoseSight)
				ChangeState(State.Alert);
		}
		else
		{
			if (_state == State.Attack || _state == State.Alert)
				ChangeState(State.LoseSight);
		}
	}

	// ── State Handlers ───────────────────────────────────────────
	private void HandleIdle()
	{
		// Waiting at a patrol point
		if (_waitingAtPoint)
		{
			Velocity = Vector2.Zero;
			_patrolWaitTimer -= (float)GetPhysicsProcessDeltaTime();

			if (_patrolWaitTimer <= 0f)
			{
				_waitingAtPoint = false;
				PickNewPatrolTarget();
			}
			return;
		}

		// Move toward patrol target
		float distToTarget = GlobalPosition.DistanceTo(_patrolTarget);

		if (distToTarget < 4f)
		{
			// Reached the target — wait before picking next
			Velocity = Vector2.Zero;
			_waitingAtPoint  = true;
			_patrolWaitTimer = PatrolWaitTime;
			return;
		}

		// Face and move toward patrol target
		Vector2 dir = (_patrolTarget - GlobalPosition).Normalized();
		Velocity = dir * PatrolSpeed;

		// Face walking direction
		_rayCastOrigin.Rotation = dir.Angle();
		UpdateFacingDirection(_patrolTarget); 
	}

	private void PickNewPatrolTarget()
	{
		// Pick a random point within PatrolRadius of the start position
		float angle  = (float)GD.RandRange(0, Mathf.Tau);
		float radius = (float)GD.RandRange(0, PatrolRadius);

		_patrolTarget = _startPosition + new Vector2(
			Mathf.Cos(angle) * radius,
			Mathf.Sin(angle) * radius
		);
	}
	
	private void HandleAlert(float dt)
	{
		Velocity = Vector2.Zero;
		_alertTimer -= dt;
		if (_alertTimer <= 0f)
			ChangeState(State.Attack);
	}

	private void HandleAttack(float dt)
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			ChangeState(State.Idle);
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance < TooCloseDistance)
		{
			ChangeState(State.Reposition);
			return;
		}

		Velocity = Vector2.Zero;
		UpdateFacingDirection(_player.GlobalPosition);
		
		if (CanAttack())
			Attack(_player);
	}

	private void HandleReposition()
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			ChangeState(State.Idle);
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance >= TooCloseDistance + 20f)
		{
			ChangeState(State.Attack);
			return;
		}

		Vector2 fleeDir = (GlobalPosition - _player.GlobalPosition).Normalized();
		Velocity = fleeDir * Speed;
	}

	private void HandleLoseSight(float dt)
	{
		Velocity = Vector2.Zero;
		_loseSightTimer -= dt;

		if (_loseSightTimer <= 0f)
		{
			_player    = null;
			_chasing   = false;
			_playerInArea = false;
			ChangeState(State.Idle);
		}
	}

	// ── State Transitions ────────────────────────────────────────
	private void ChangeState(State newState)
	{
		switch (newState)
		{
			case State.Alert:
				_alertTimer = AlertDuration;
				break;
			case State.LoseSight:
				_loseSightTimer = LoseSightDuration;
				break;
		}

		GD.Print($"[RangeEnemy] {_state} → {newState}");
		_state = newState;
	}

	// ── Area Signals (broad phase) ────────────────────────────────
	private void OnDetectionBodyEntered(Node2D body)
	{
		if (body is not Player p) return;
		_player = p;
		_chasing = true;
		_playerInArea = true;
		GD.Print("[RangeEnemy] Player entered detection area.");
	}

	private void OnDetectionBodyExited(Node2D body)
	{
		if (body is not Player) return;
		_playerInArea = false;

		if (_state != State.Idle && _state != State.Dead)
			ChangeState(State.LoseSight);
	}

	// ── Attack stub (projectile next) ────────────────────────────
	protected override void Attack(Node2D target)
	{
		_attackCooldownTimer = AttackCooldown;

		if (ProjectileScene == null)
		{
			GD.PrintErr("[RangeEnemy] ProjectileScene not assigned!");
			return;
		}

		// Spawn projectile at enemy position
		var projectile = ProjectileScene.Instantiate<Projectile>();
		
		// Add to the scene root (not as child of enemy, so it moves independently)
		GetTree().CurrentScene.AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition;

		// Fire towards player
		Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
		projectile.Initialize(direction);

		GD.Print("[RangeEnemy] SHOOT!");
	}


	// ── Death ────────────────────────────────────────────────────
	protected override void Die()
	{
		ChangeState(State.Dead);
		Velocity = Vector2.Zero;
		base.Die();
	}
}
