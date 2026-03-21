using Godot;

public partial class Inventory : Node
{
	public const int MaxPotions = 5;

	[Signal] public delegate void PotionsChangedEventHandler(int currentPotions);

	[Export] public int CurrentPotions { get; private set; } = 0;

	public bool CanAddPotion() => CurrentPotions < MaxPotions;

	public bool TryAddPotion(int amount = 1)
	{
		if (amount <= 0) return true;
		if (CurrentPotions >= MaxPotions) return false;

		CurrentPotions = Mathf.Min(CurrentPotions + amount, MaxPotions);
		EmitSignal(SignalName.PotionsChanged, CurrentPotions);
		return true;
	}
}
