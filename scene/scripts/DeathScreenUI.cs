using Godot;
using System.Threading.Tasks;

public partial class DeathScreenUI : Control
{
	[Signal] public delegate void RespawnRequestedEventHandler();

	[ExportGroup("Nodes")]
	[Export] private Label _label;
	[Export] private AnimationPlayer _animPlayer;

	[ExportGroup("Animation")]
	[Export] private string _animName = "Death_Anim";

	[ExportGroup("Text")]
	[Export] private string _defaultDeathText = "TIME CLAIMS ANOTHER";

	private bool _respawnSignalSentThisPlay = false;

	public override void _EnterTree()
	{
		Visible = false;
	}

	public override void _Ready()
	{
		_animPlayer?.Stop();
		Visible = false;
	}

	// Put a "Call Method Track" keyframe on this method at the black-screen moment.
	public void Anim_RequestRespawn()
	{
		if (_respawnSignalSentThisPlay) return;
		_respawnSignalSentThisPlay = true;

		EmitSignal(SignalName.RespawnRequested);
	}

	public async Task PlayAndWaitAsync(string text = null)
	{
		if (_animPlayer == null)
		{
			GD.PrintErr("[DeathScreenUI] Missing _animPlayer export.");
			return;
		}

		_respawnSignalSentThisPlay = false;

		Visible = true;

		if (_label != null)
			_label.Text = string.IsNullOrEmpty(text) ? _defaultDeathText : text;

		_animPlayer.Stop();
		_animPlayer.Play(_animName);

		await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);

		Visible = false;
	}
}
