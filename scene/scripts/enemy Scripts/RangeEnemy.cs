using Godot;

public partial class RangeEnemy : BaseEnemy
{
	// ─── Cone Detection ───────────────────────────────────────────
	[ExportGroup("Cone Detection")]
	[Export] public float ConeRange = 200f;
	[Export] public float ConeHalfAngleDeg = 45f;  // 90° total cone width

	// ─── Range Behaviour ──────────────────────────────────────────
	[ExportGroup("Range Behaviour")]
	[Export] public float PreferredDistance = 120f;   // ideal shooting distance
	[Export] public float TooCloseDistance = 60f;     // backs away if player closer than this
	[Export] public float AlertDuration = 0.6f;       // pause before shooting
	[Export] public float LoseSightDuration = 2f;     // how long before returning to idle

	// ─── State Machine ────────────────────────────────────────────
	private enum State { Idle, Alert, Attack, Reposition, LoseSight, Dead }
	private State _state = State.Idle;

	// ─── Timers ───────────────────────────────────────────────────
	private float _alertTimer = 0f;
	private float _loseSightTimer = 0f;

	// ─── Nodes ────────────────────────────────────────────────────
	private Area2D _detectionArea;

	public override void _Ready()
	{
		base._Ready();
		_detectionArea = GetNode<Area2D>("detection_area");

		// Hook up detection signals
		_detectionArea.BodyEntered += OnDetectionBodyEntered;
		_detectionArea.BodyExited  += OnDetectionBodyExited;
	}

	// ─── State Machine Core ───────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		switch (_state)
		{
			case State.Idle:
				HandleIdle();
				break;

			case State.Alert:
				HandleAlert(dt);
				break;

			case State.Attack:
				HandleAttack(dt);
				break;

			case State.Reposition:
				HandleReposition();
				break;

			case State.LoseSight:
				HandleLoseSight(dt);
				break;

			case State.Dead:
				break;
		}

		MoveAndSlide();
	}

	// ─── State Handlers ───────────────────────────────────────────

	private void HandleIdle()
	{
		Velocity = Vector2.Zero;
		// Just wait — detection signals will trigger the transition
	}

	private void HandleAlert(float dt)
	{
		Velocity = Vector2.Zero;
		FacePlayer();

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

		// Player got too close — back away
		if (distance < TooCloseDistance)
		{
			ChangeState(State.Reposition);
			return;
		}

		// Player left the cone — start lose sight timer
		if (!IsPlayerInCone())
		{
			ChangeState(State.LoseSight);
			return;
		}

		FacePlayer();

		// Shoot if cooldown is ready
		if (CanAttack())
			Attack(_player);

		Velocity = Vector2.Zero;
	}

	private void HandleReposition()
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			ChangeState(State.Idle);
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// Backed away enough — return to attacking
		if (distance >= TooCloseDistance + 20f)
		{
			ChangeState(State.Attack);
			return;
		}

		// Move directly away from the player
		Vector2 fleeDirection = (GlobalPosition - _player.GlobalPosition).Normalized();
		Velocity = fleeDirection * Speed;
	}

	private void HandleLoseSight(float dt)
	{
		Velocity = Vector2.Zero;
		_loseSightTimer -= dt;

		// Player came back into cone while we were looking
		if (_player != null && IsPlayerInCone())
		{
			ChangeState(State.Alert);
			return;
		}

		if (_loseSightTimer <= 0f)
		{
			_player = null;
			_chasing = false;
			ChangeState(State.Idle);
		}
	}

	// ─── State Transition ─────────────────────────────────────────
	private void ChangeState(State newState)
	{
		// Entry actions
		switch (newState)
		{
			case State.Alert:
				_alertTimer = AlertDuration;
				GD.Print("[RangeEnemy] → Alert");
				break;

			case State.Attack:
				GD.Print("[RangeEnemy] → Attack");
				break;

			case State.Reposition:
				GD.Print("[RangeEnemy] → Reposition");
				break;

			case State.LoseSight:
				_loseSightTimer = LoseSightDuration;
				GD.Print("[RangeEnemy] → LoseSight");
				break;

			case State.Idle:
				GD.Print("[RangeEnemy] → Idle");
				break;

			case State.Dead:
				GD.Print("[RangeEnemy] → Dead");
				break;
		}

		_state = newState;
	}

	// ─── Cone Detection ───────────────────────────────────────────
	private bool IsPlayerInCone()
	{
		if (_player == null || !IsInstanceValid(_player)) return false;

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();

		// Out of range entirely
		if (distance > ConeRange) return false;

		// Check angle between facing direction and direction to player
		float angleToPlayer = Mathf.RadToDeg(
			Mathf.Abs(GlobalTransform.X.AngleTo(toPlayer))
		);

		return angleToPlayer <= ConeHalfAngleDeg;
	}

	private void FacePlayer()
	{
		if (_player == null) return;
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Rotation = direction.Angle();
	}

	// ─── Detection Signals ────────────────────────────────────────
	private void OnDetectionBodyEntered(Node2D body)
	{
		if (body is not Player p) return;
		_player = p;
		_chasing = true;

		if (_state == State.Idle && IsPlayerInCone())
			ChangeState(State.Alert);
	}

	private void OnDetectionBodyExited(Node2D body)
	{
		if (body is not Player) return;

		if (_state == State.Attack || _state == State.Alert)
			ChangeState(State.LoseSight);
	}

	// ─── Override Attack (will add projectile next step) ──────────
	protected override void Attack(Node2D target)
	{
		_attackCooldownTimer = AttackCooldown;
		GD.Print("[RangeEnemy] Fired at player! (projectile coming next)");
		// We'll spawn a projectile here in the next step
	}

	// ─── Override Die ─────────────────────────────────────────────
	protected override void Die()
	{
		ChangeState(State.Dead);
		Velocity = Vector2.Zero;
		// Animation will go here later
		base.Die(); // grants XP and QueueFree
	}
}
