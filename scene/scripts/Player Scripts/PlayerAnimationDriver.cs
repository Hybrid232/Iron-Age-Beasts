using Godot;

public partial class PlayerAnimationDriver : Node
{
	[ExportGroup("Refs")]
	[Export] public AnimationTree AnimationTree;

	// Your tree is: Idle (BlendTree) -> BlendSpace2D -> TimeScale -> Output
	// So we must set BlendSpace2D's blend_position inside each state.
	[ExportGroup("AnimationTree Keys (match your node names)")]
	[Export] public string IdleBlendPositionKey = "parameters/Idle/BlendSpace2D/blend_position";
	[Export] public string RunBlendPositionKey  = "parameters/Run/BlendSpace2D/blend_position";

	[ExportGroup("StateMachine")]
	[Export] public bool UseStateMachine = true;
	[Export] public string PlaybackKey = "parameters/playback";
	[Export] public string IdleStateName = "Idle";
	[Export] public string RunStateName = "Run";

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

		if (PrintDebug)
		{
			var idle = AnimationTree.Get(new StringName(IdleBlendPositionKey));
			var run = AnimationTree.Get(new StringName(RunBlendPositionKey));
			GD.Print($"[AnimDriver] Ready idle='{idle}' run='{run}'");
		}
	}

	public void UpdateFromInput(bool allowRun = true)
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

		bool isRunning = allowRun && anyHeld;

		if (UseStateMachine)
			TrySetState(isRunning ? RunStateName : IdleStateName);

		Vector2 blendDir = _facing;
		if (InvertYForBlendSpace)
			blendDir.Y *= -1f;

		AnimationTree.Set(new StringName(IdleBlendPositionKey), blendDir);
		AnimationTree.Set(new StringName(RunBlendPositionKey), blendDir);

		if (PrintDebug)
		{
			var idleNow = AnimationTree.Get(new StringName(IdleBlendPositionKey));
			var runNow = AnimationTree.Get(new StringName(RunBlendPositionKey));
			GD.Print($"[AnimDriver] held={anyHeld} running={isRunning} facing={_facing} blend={blendDir} idleNow='{idleNow}' runNow='{runNow}'");
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
