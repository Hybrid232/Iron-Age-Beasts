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

	[ExportGroup("Respawn Reset Hooks")]
	[Export] private bool _resetBossEncountersOnRespawn = true;

	// Put TutorialBoss (and any other bosses) into this group in the editor.
	[Export] private string _bossEncounterGroupName = "BossEncounter";

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

		// Reset bosses BEFORE the respawn signal goes out, so the arena/UI are clean on respawn.
		if (_resetBossEncountersOnRespawn)
			ResetBossEncounters();

		EmitSignal(SignalName.RespawnRequested);
	}

	private void ResetBossEncounters()
	{
		if (string.IsNullOrEmpty(_bossEncounterGroupName)) return;

		var nodes = GetTree().GetNodesInGroup(_bossEncounterGroupName);
		if (nodes == null || nodes.Count == 0) return;

		foreach (var n in nodes)
		{
			// Hard-typed path (best)
			if (n is TutorialBoss boss)
			{
				boss.ForceResetEncounter();
				continue;
			}

			// Duck-typed fallback (lets you reuse this for other boss scripts)
			if (n is Node node && node.HasMethod("ForceResetEncounter"))
				node.Call("ForceResetEncounter");
		}
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
