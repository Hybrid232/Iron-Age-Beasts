using Godot;

public partial class XPDisplay : Label
{
	[Export] private float countUpDuration = 0.45f;

	private XPManager _xpManager;

	private int _displayedXp;
	private int _targetXp;

	private bool _isCounting = false;
	private double _timer = 0.0;
	private int _startXp = 0;

	public override void _Ready()
	{
		GD.Print($"[XPDisplay] Ready. Path={GetPath()}");

		// BEST FOR YOUR SCENE: Label is a child of XPManager node in XPManager scene
		_xpManager = GetParent() as XPManager;

		// Fallback: group lookup (in case you rearrange nodes later)
		if (_xpManager == null)
			_xpManager = GetTree().GetFirstNodeInGroup(XPManager.GROUP_NAME) as XPManager;

		if (_xpManager == null)
		{
			GD.PrintErr("[XPDisplay] Could not find XPManager (neither parent nor group).");
			return;
		}

		GD.Print($"[XPDisplay] Found XPManager at Path={_xpManager.GetPath()} - connecting signal");
		_xpManager.XpChanged += OnXpChanged;

		_displayedXp = _xpManager.CurrentXp;
		_targetXp = _xpManager.CurrentXp;
		UpdateText(_displayedXp);
	}

	public override void _ExitTree()
	{
		if (_xpManager != null)
			_xpManager.XpChanged -= OnXpChanged;
	}

	public override void _Process(double delta)
	{
		if (!_isCounting) return;

		_timer += delta;

		float t = countUpDuration <= 0.0001f
			? 1f
			: Mathf.Clamp((float)(_timer / countUpDuration), 0f, 1f);

		float eased = 1f - Mathf.Pow(1f - t, 3f);

		int newValue = Mathf.RoundToInt(Mathf.Lerp(_startXp, _targetXp, eased));

		if (newValue != _displayedXp)
		{
			_displayedXp = newValue;
			UpdateText(_displayedXp);
		}

		if (t >= 1f)
		{
			_displayedXp = _targetXp;
			UpdateText(_displayedXp);
			_isCounting = false;
		}
	}

	private void OnXpChanged(int newTotalXp)
	{
		GD.Print($"[XPDisplay] XpChanged received: {newTotalXp}");

		_targetXp = newTotalXp;
		_startXp = _displayedXp;
		_timer = 0.0;
		_isCounting = true;
	}

	private void UpdateText(int xp)
	{
		Text = $"XP: {xp}";
	}
}
