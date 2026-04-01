using Godot;

public partial class PlayerAnimationDriver : Node
{
	[ExportGroup("Refs")]
	[Export] public AnimationTree AnimationTree;

	// Your tree is: Idle (BlendTree) -> BlendSpace2D -> TimeScale -> Output
	[ExportGroup("AnimationTree Keys")]
	[Export] public string IdleBlendPositionKey = "parameters/Idle/BlendSpace2D/blend_position";
	[Export] public string RunBlendPositionKey  = "parameters/Run/BlendSpace2D/blend_position";
	[Export] public string DashBlendPositionKey = "parameters/Dash/BlendSpace2D/blend_position";

	// Attack state BlendSpace2D blend_position
	[Export] public string AttackBlendPositionKey = "parameters/Attack/BlendSpace2D/blend_position";

	[ExportGroup("StateMachine")]
	[Export] public bool UseStateMachine = true;
	[Export] public string PlaybackKey = "parameters/playback";
	[Export] public string IdleStateName = "Idle";
	[Export] public string RunStateName = "Run";
	[Export] public string DashStateName = "Dash";
	[Export] public string AttackStateName = "Attack";

	[ExportGroup("BlendSpace Convention")]
	[Export] public bool InvertYForBlendSpace = false;

	[ExportGroup("Inputs")]
	[Export] public string LeftAction = "ui_left";
	[Export] public string RightAction = "ui_right";
	[Export] public string UpAction = "ui_up";
	[Export] public string DownAction = "ui_down";

	[ExportGroup("Attack")]
	[Export] public bool AttackOverridesOtherStates = true;

	// If true, we automatically leave Attack after AttackDurationSeconds.
	// If false, you must call EndAttack() (e.g., from an animation signal).
	[Export] public bool AutoEndAttack = true;

	// Tune to your animation length if AutoEndAttack is enabled.
	[Export(PropertyHint.Range, "0.01,5.0,0.01")]
	public float AttackDurationSeconds = 0.35f;

	[ExportGroup("Debug")]
	[Export] public bool PrintDebug = false;

	private Vector2 _facing = Vector2.Down;

	// For “first key pressed wins” diagonals
	private ulong _tLeft, _tRight, _tUp, _tDown;

	// Attack bookkeeping
	private bool _attackRequested = false;
	private bool _isAttacking = false;
	private double _attackEndsAt = 0.0;

	public override void _Ready()
	{
		if (AnimationTree == null)
		{
			GD.PrintErr("[PlayerAnimationDriver] AnimationTree not assigned.");
			return;
		}

		AnimationTree.Active = true;

		// Helpful warning if someone cleared exports in the Inspector
		if (string.IsNullOrWhiteSpace(LeftAction) ||
			string.IsNullOrWhiteSpace(RightAction) ||
			string.IsNullOrWhiteSpace(UpAction) ||
			string.IsNullOrWhiteSpace(DownAction))
		{
			GD.PrintErr("[PlayerAnimationDriver] One or more input action names are empty. " +
						"Set Left/Right/Up/Down Action exports on the PlayerAnimationDriver node.");
		}
	}

	private static bool IsPressedSafe(string action)
	{
		if (string.IsNullOrWhiteSpace(action))
			return false;
		return Input.IsActionPressed(action);
	}

	private static bool IsJustPressedSafe(string action)
	{
		if (string.IsNullOrWhiteSpace(action))
			return false;
		return Input.IsActionJustPressed(action);
	}

	/// <summary>
	/// Call this when the player starts an attack (button pressed).
	/// </summary>
	public void TriggerAttack()
	{
		_attackRequested = true;
	}

	/// <summary>
	/// Call this if you want to end attack manually (e.g. via animation finished signal).
	/// Only needed when AutoEndAttack == false.
	/// </summary>
	public void EndAttack()
	{
		_isAttacking = false;
		_attackRequested = false;
	}

	/// <summary>
	/// Call once per frame from Player.
	/// </summary>
	public void UpdateFromInput(bool allowRun = true, bool isDashing = false)
	{
		if (AnimationTree == null)
			return;

		bool anyHeld =
			IsPressedSafe(LeftAction) ||
			IsPressedSafe(RightAction) ||
			IsPressedSafe(UpAction) ||
			IsPressedSafe(DownAction);

		Vector2 dir = anyHeld ? GetCardinalDirectionWithDiagonalPriority() : Vector2.Zero;

		if (dir != Vector2.Zero)
			_facing = dir;

		Vector2 blendDir = _facing;
		if (InvertYForBlendSpace)
			blendDir.Y *= -1f;

		// Update blend positions for all states (safe even if not currently active)
		AnimationTree.Set(new StringName(IdleBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(RunBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(DashBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(AttackBlendPositionKey), blendDir);

		if (!UseStateMachine)
			return;

		// Handle attack request -> enter Attack
		if (_attackRequested && !_isAttacking)
		{
			_isAttacking = true;
			_attackRequested = false;

			TrySetState(AttackStateName);

			if (AutoEndAttack)
				_attackEndsAt = Time.GetTicksMsec() / 1000.0 + AttackDurationSeconds;
		}

		// If we are attacking, optionally keep us in Attack and/or auto-exit
		if (_isAttacking)
		{
			if (AttackOverridesOtherStates)
				TrySetState(AttackStateName);

			if (AutoEndAttack)
			{
				double now = Time.GetTicksMsec() / 1000.0;
				if (now >= _attackEndsAt)
					_isAttacking = false;
			}

			// While attacking (and overriding), don't switch to Dash/Run/Idle.
			if (AttackOverridesOtherStates)
				return;
		}

		// State selection: Dash overrides Run/Idle (when not overridden by attack)
		if (isDashing)
		{
			TrySetState(DashStateName);
		}
		else
		{
			bool isRunning = allowRun && anyHeld;
			TrySetState(isRunning ? RunStateName : IdleStateName);
		}
	}

	private void TrySetState(string stateName)
	{
		var playbackVar = AnimationTree.Get(new StringName(PlaybackKey));
		var playback = playbackVar.As<AnimationNodeStateMachinePlayback>();
		if (playback == null)
			return;

		if (playback.GetCurrentNode() != stateName)
			playback.Travel(stateName);
	}

	private Vector2 GetCardinalDirectionWithDiagonalPriority()
	{
		ulong now = Time.GetTicksMsec();

		if (IsJustPressedSafe(LeftAction)) _tLeft = now;
		if (IsJustPressedSafe(RightAction)) _tRight = now;
		if (IsJustPressedSafe(UpAction)) _tUp = now;
		if (IsJustPressedSafe(DownAction)) _tDown = now;

		bool left  = IsPressedSafe(LeftAction);
		bool right = IsPressedSafe(RightAction);
		bool up    = IsPressedSafe(UpAction);
		bool down  = IsPressedSafe(DownAction);

		if (!left && !right && !up && !down)
			return Vector2.Zero;

		// Single axis
		if ((left ^ right) && !(up || down))
			return left ? Vector2.Left : Vector2.Right;

		if ((up ^ down) && !(left || right))
			return up ? Vector2.Up : Vector2.Down;

		// Diagonal/conflict: FIRST pressed wins => OLDEST timestamp
		ulong bestTime = ulong.MaxValue;
		Vector2 chosen = Vector2.Zero;

		if (up && _tUp < bestTime) { bestTime = _tUp; chosen = Vector2.Up; }
		if (down && _tDown < bestTime) { bestTime = _tDown; chosen = Vector2.Down; }
		if (left && _tLeft < bestTime) { bestTime = _tLeft; chosen = Vector2.Left; }
		if (right && _tRight < bestTime) { bestTime = _tRight; chosen = Vector2.Right; }

		return chosen == Vector2.Zero ? Vector2.Down : chosen;
	}
}
