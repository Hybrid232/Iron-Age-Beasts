using Godot;

public partial class BossUI : Control, IBossUI
{
	[Export] private TextureProgressBar bossHealthBar;

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
