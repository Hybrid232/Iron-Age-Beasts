using Godot;

public partial class XPManager : Node
{
	public const string GROUP_NAME = "XPManager";

	[Signal] public delegate void XpChangedEventHandler(int currentXp);

	[Export] public int CurrentXp { get; private set; } = 0;

	public override void _Ready()
	{
		AddToGroup(GROUP_NAME);
		GD.Print($"[XPManager] Ready. Path={GetPath()} AddedToGroup={GROUP_NAME}");
	}

	public bool CanAfford(int cost) => cost > 0 && CurrentXp >= cost;

	public bool TrySpendXp(int cost)
	{
		if (cost <= 0) return true;
		if (CurrentXp < cost) return false;

		CurrentXp -= cost;
		GD.Print($"[XPManager] -{cost} XP (Total: {CurrentXp})");
		EmitSignal(SignalName.XpChanged, CurrentXp);
		return true;
	}

	public void AddXp(int amount)
	{
		if (amount <= 0) return;

		CurrentXp += amount;
		GD.Print($"[XPManager] +{amount} XP (Total: {CurrentXp})");
		EmitSignal(SignalName.XpChanged, CurrentXp);
	}
}
