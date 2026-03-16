using Godot;

public partial class BossUI : Control, IBossUI
{
	[Export] private NodePath bossHealthBarPath;
	private TextureProgressBar bossHealthBar;

	public override void _Ready()
	{
		if (bossHealthBarPath == null || bossHealthBarPath.IsEmpty)
		{
			GD.PushError("[BossUI] bossHealthBarPath is not assigned.");
			return;
		}

		bossHealthBar = GetNodeOrNull<TextureProgressBar>(bossHealthBarPath);
		if (bossHealthBar == null)
			GD.PushError($"[BossUI] Could not find TextureProgressBar at path: {bossHealthBarPath}");
	}

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
