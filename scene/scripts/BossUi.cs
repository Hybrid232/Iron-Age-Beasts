using Godot;

public partial class BossUI : CanvasLayer, IBossUI
{
	[Export] private ProgressBar bossHealthBar;

	public void InitializeBoss(int maxHealth, int currentHealth)
	{
		if (bossHealthBar == null) return;
		bossHealthBar.MaxValue = maxHealth;
		bossHealthBar.Value = currentHealth;
		Visible = true;
	}

	public void UpdateBossHealth(int currentHealth)
	{
		if (bossHealthBar == null) return;
		bossHealthBar.Value = currentHealth;
	}
}
