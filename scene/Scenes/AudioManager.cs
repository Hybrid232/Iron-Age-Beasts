using Godot;

/// <summary>
/// AudioManager — Autoload singleton.
///
/// This manager owns only the background music (BGM) player.
/// The NPC and TutorialBoss each own their own AudioStreamPlayer nodes
/// and handle their own fade logic — they just tell this manager to duck
/// or restore the BGM when they start/stop their music.
///
/// ─── Setup ───────────────────────────────────────────────────────────────
/// 1. Add as an Autoload:
///      Project → Project Settings → Autoload → +
///      Path : res://AudioManager.cs   Name : AudioManager
///
/// 2. Assign a BGM AudioStreamPlayer node to _bgmPlayer in the Inspector
///    (or create a child AudioStreamPlayer on the AudioManager node and
///    assign it there). Set its Bus to "Music" (or whatever bus you use).
///
/// 3. Optionally assign a _defaultBGM stream — it will start playing on load.
///
/// ─── Usage ───────────────────────────────────────────────────────────────
///   // Play / swap BGM track
///   AudioManager.Instance.PlayBGM(myStream);
///
///   // Called by NPC when dialogue opens / closes
///   AudioManager.Instance.DuckBGM("npc");
///   AudioManager.Instance.RestoreBGM("npc");
///
///   // Called by TutorialBoss when fight starts / ends
///   AudioManager.Instance.DuckBGM("boss");
///   AudioManager.Instance.RestoreBGM("boss");
/// </summary>
public partial class AudioManager : Node
{
	// -------------------------------------------------------------------------
	// Singleton
	// -------------------------------------------------------------------------
	public static AudioManager Instance { get; private set; }

	// -------------------------------------------------------------------------
	// Exports
	// -------------------------------------------------------------------------
	[ExportGroup("BGM Player")]
	/// <summary>
	/// Assign an AudioStreamPlayer node here (child of this node, or placed
	/// anywhere in the scene). It must already exist — we don't create it.
	/// </summary>
	[Export] private AudioStreamPlayer _bgmPlayer;

	[Export] private AudioStream _defaultBGM;

	[ExportGroup("BGM Volume")]
	[Export] private float _bgmFullDb   =   0f;   // volume when nothing is ducking it
	[Export] private float _bgmDuckedDb = -80f;   // volume while boss/NPC music is active

	[ExportGroup("BGM Fade Timings (seconds)")]
	[Export] private float _bgmFadeOutTime = 1.0f; // how long to fade down when ducking
	[Export] private float _bgmFadeInTime  = 1.5f; // how long to fade back up on restore
	
	

	// -------------------------------------------------------------------------
	// Internal state
	// -------------------------------------------------------------------------
	private Tween _bgmTween;

	// Tracks which systems are currently asking the BGM to stay ducked.
	// BGM only restores when this set is empty.
	private readonly System.Collections.Generic.HashSet<string> _duckers
		= new System.Collections.Generic.HashSet<string>();

	// =========================================================================
	// Lifecycle
	// =========================================================================
	public override void _EnterTree()
	{
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}

	public override void _Ready()
	{
		if (_bgmPlayer == null)
		{
			GD.PushError("[AudioManager] _bgmPlayer is not assigned. " +
				"Add an AudioStreamPlayer child and assign it in the Inspector.");
			return;
		}

		if (_defaultBGM != null)
			PlayBGM(_defaultBGM);
	}

	// =========================================================================
	// Public API
	// =========================================================================

	/// <summary>
	/// Start (or cross-fade to) a new BGM track.
	/// Safe to call at any time — if BGM is currently ducked it starts silently
	/// and fades in when the ducker releases.
	/// </summary>
	public void PlayBGM(AudioStream stream)
	{
		if (_bgmPlayer == null || stream == null) return;

		// Don't restart the same track that's already playing.
		if (_bgmPlayer.Stream == stream && _bgmPlayer.Playing) return;

		KillBgmTween();
		_bgmPlayer.Stream = stream;
		_bgmPlayer.VolumeDb = _bgmDuckedDb;
		_bgmPlayer.Play();

		// Only fade it up if nothing is ducking it right now.
		if (_duckers.Count == 0)
			FadeBgmTo(_bgmFullDb, _bgmFadeInTime);
	}

	/// <summary>
	/// Tell the manager that <paramref name="source"/> is now playing its own
	/// music and wants the BGM silenced. Multiple callers are tracked — BGM
	/// won't restore until all of them call RestoreBGM.
	/// </summary>
	public void DuckBGM(string source)
	{
		_duckers.Add(source);
		FadeBgmTo(_bgmDuckedDb, _bgmFadeOutTime);
	}

	/// <summary>
	/// Tell the manager that <paramref name="source"/> is done with its music.
	/// If nothing else is ducking the BGM it will fade back up.
	/// </summary>
	public void RestoreBGM(string source)
	{
		_duckers.Remove(source);

		if (_duckers.Count == 0 && _bgmPlayer != null && _bgmPlayer.Playing)
			FadeBgmTo(_bgmFullDb, _bgmFadeInTime);
	}

	/// <summary>
	/// Immediately silence the BGM without going through a ducker.
	/// Useful for cutscenes, death screens, etc.
	/// </summary>
	public void SilenceBGMImmediate()
	{
		KillBgmTween();
		if (_bgmPlayer != null) _bgmPlayer.VolumeDb = _bgmDuckedDb;
	}

	/// <summary>
	/// Stop BGM entirely (fade out then stop playback).
	/// </summary>
	public void StopBGM()
	{
		KillBgmTween();
		if (_bgmPlayer == null || !_bgmPlayer.Playing) return;

		_bgmTween = CreateTween();
		_bgmTween.TweenProperty(_bgmPlayer, "volume_db", _bgmDuckedDb, _bgmFadeOutTime);
		_bgmTween.TweenCallback(Callable.From(() => _bgmPlayer?.Stop()));
	}

	// =========================================================================
	// Internal helpers
	// =========================================================================
	private void FadeBgmTo(float targetDb, float time)
	{
		if (_bgmPlayer == null) return;
		KillBgmTween();

		if (time <= 0f) { _bgmPlayer.VolumeDb = targetDb; return; }

		_bgmTween = CreateTween();
		_bgmTween.TweenProperty(_bgmPlayer, "volume_db", targetDb, time)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void KillBgmTween()
	{
		if (_bgmTween == null) return;
		if (IsInstanceValid(_bgmTween)) _bgmTween.Kill();
		_bgmTween = null;
	}
}
