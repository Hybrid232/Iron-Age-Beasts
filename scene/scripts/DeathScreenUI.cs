using Godot;
using System.Threading.Tasks;

public partial class DeathScreenUI : Control
{
	[ExportGroup("Nodes")]
	[Export] private Control _root;               // The full-screen panel/control you animate
	[Export] private Label _label;
	[Export] private AnimationPlayer _animPlayer;

	[ExportGroup("Animations")]
	[Export] private string _deathAnimName = "Death_Anim";
	[Export] private string _respawnAnimName = "Respawning_Anim";

	[ExportGroup("Text")]
	[Export] private string _defaultDeathText = "TIME CLAIMS ANOTHER";

	public override void _EnterTree()
	{
		HideInstant();
	}

	public override void _Ready()
	{
		_animPlayer?.Stop();
		HideInstant();
	}

	private void HideInstant()
	{
		Visible = false;

		if (_root != null)
			_root.Visible = false;

		// Reset alpha so we don't appear "half faded" at scene start
		if (_root != null)
		{
			var m = _root.Modulate;
			m.A = 1f;
			_root.Modulate = m;
		}

		if (_label != null)
		{
			var lm = _label.Modulate;
			lm.A = 1f;
			_label.Modulate = lm;
		}
	}

	private void ShowInstant()
	{
		Visible = true;
		if (_root != null) _root.Visible = true;
	}

	public async Task PlayDeathAndWaitAsync(string text = null)
	{
		if (_animPlayer == null)
		{
			GD.PrintErr("[DeathScreenUI] Missing _animPlayer export.");
			return;
		}

		ShowInstant();

		if (_label != null)
			_label.Text = string.IsNullOrEmpty(text) ? _defaultDeathText : text;

		_animPlayer.Stop();
		_animPlayer.Play(_deathAnimName);

		await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);

		// Do NOT hide instantly here—leave it up so respawn can fade it out.
	}

	public async Task PlayRespawnFadeAndHideAsync()
	{
		if (_animPlayer == null)
		{
			GD.PrintErr("[DeathScreenUI] Missing _animPlayer export.");
			HideInstant();
			return;
		}

		// If the overlay is already hidden, nothing to do
		if (!Visible)
			return;

		_animPlayer.Stop();
		_animPlayer.Play(_respawnAnimName);

		await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);

		HideInstant();
	}
}
