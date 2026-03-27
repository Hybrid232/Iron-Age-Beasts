using Godot;

public partial class PlayerAnimationDriver : Node
{
	[ExportGroup("Refs")]
	[Export] public AnimationTree AnimationTree;

	// Your tree is: Idle (BlendTree) -> BlendSpace2D -> TimeScale -> Output
	[ExportGroup("AnimationTree Keys")]
	[Export] public string IdleBlendPositionKey = "parameters/Idle/BlendSpace2D/blend_position";
	[Export] public string RunBlendPositionKey  = "parameters/Run/BlendSpace2D/blend_position";

	// NEW: Dash state BlendSpace2D blend_position
	[Export] public string DashBlendPositionKey = "parameters/Dash/BlendSpace2D/blend_position";

	[ExportGroup("StateMachine")]
	[Export] public bool UseStateMachine = true;
	[Export] public string PlaybackKey = "parameters/playback";
	[Export] public string IdleStateName = "Idle";
	[Export] public string RunStateName = "Run";

	// NEW: Dash state name (you said the state is named Dash)
	[Export] public string DashStateName = "Dash";

	[ExportGroup("BlendSpace Convention")]
	[Export] public bool InvertYForBlendSpace = false;

	[ExportGroup("Inputs")]
	[Export] public string LeftAction = "ui_left";
	[Export] public string RightAction = "ui_right";
	[Export] public string UpAction = "ui_up";
	[Export] public string DownAction = "ui_down";

	[ExportGroup("Debug")]
	[Export] public bool PrintDebug = false;

	private Vector2 _facing = Vector2.Down;

	// For “first key pressed wins” diagonals
	private ulong _tLeft, _tRight, _tUp, _tDown;

	public override void _Ready()
	{
		if (AnimationTree == null)
		{
			GD.PrintErr("[PlayerAnimationDriver] AnimationTree not assigned.");
			return;
		}

		AnimationTree.Active = true;
	}

	// CHANGED: added isDashing
	public void UpdateFromInput(bool allowRun = true, bool isDashing = false)
	{
		if (AnimationTree == null)
			return;

		bool anyHeld =
			Input.IsActionPressed(LeftAction) ||
			Input.IsActionPressed(RightAction) ||
			Input.IsActionPressed(UpAction) ||
			Input.IsActionPressed(DownAction);

		Vector2 dir = anyHeld ? GetCardinalDirectionWithDiagonalPriority() : Vector2.Zero;

		if (dir != Vector2.Zero)
			_facing = dir;

		// State selection: Dash overrides Run/Idle
		if (UseStateMachine)
		{
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

		Vector2 blendDir = _facing;
		if (InvertYForBlendSpace)
			blendDir.Y *= -1f;

		// Update blend positions for all 3 states (safe even if not currently active)
		AnimationTree.Set(new StringName(IdleBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(RunBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(DashBlendPositionKey), blendDir);
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

		if (Input.IsActionJustPressed(LeftAction)) _tLeft = now;
		if (Input.IsActionJustPressed(RightAction)) _tRight = now;
		if (Input.IsActionJustPressed(UpAction)) _tUp = now;
		if (Input.IsActionJustPressed(DownAction)) _tDown = now;

		bool left = Input.IsActionPressed(LeftAction);
		bool right = Input.IsActionPressed(RightAction);
		bool up = Input.IsActionPressed(UpAction);
		bool down = Input.IsActionPressed(DownAction);

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
