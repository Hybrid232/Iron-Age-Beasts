using Godot;

public partial class BossUI : Control, IBossUI
{
	[Export] private NodePath bossHealthBarPath;
	private Range bossHealthBarRange;

	public override void _Ready()
	{
		// Start hidden by default; the boss script will show/hide it.
		Visible = false;

		GD.Print($"[BossUI] _Ready() path={GetPath()} visible={Visible}");
		TryResolveBar();
	}

	private void TryResolveBar()
	{
		if (bossHealthBarRange != null) return;

		if (bossHealthBarPath == null || bossHealthBarPath.IsEmpty)
		{
			GD.PushError($"[BossUI] bossHealthBarPath is not assigned. (BossUI node path={GetPath()})");
			return;
		}

		// Range is the common base type for ProgressBar/TextureProgressBar
		bossHealthBarRange = GetNodeOrNull<Range>(bossHealthBarPath);

		if (bossHealthBarRange == null)
		{
			GD.PushError($"[BossUI] Could not find a Range (ProgressBar/TextureProgressBar) at bossHealthBarPath='{bossHealthBarPath}' (BossUI node path={GetPath()})");
		}
		else
		{
			GD.Print($"[BossUI] Resolved boss health bar node='{bossHealthBarRange.Name}' type={bossHealthBarRange.GetType().Name} path='{bossHealthBarRange.GetPath()}'");
		}
	}

	public void InitializeBoss(int maxHealth, int currentHealth)
	{
		GD.Print($"[BossUI] InitializeBoss(max={maxHealth}, current={currentHealth})");

		TryResolveBar();
		if (bossHealthBarRange == null) return;

		bossHealthBarRange.MaxValue = maxHealth;
		bossHealthBarRange.Value = currentHealth;

		// IMPORTANT: do NOT force Visible=true here.
		// TutorialBoss decides when to show/hide the boss UI.
		GD.Print($"[BossUI] After init: bar.Value={bossHealthBarRange.Value}/{bossHealthBarRange.MaxValue}, BossUI.Visible={Visible}");
	}

	public void UpdateBossHealth(int currentHealth)
	{
		TryResolveBar();
		if (bossHealthBarRange == null)
		{
			GD.Print($"[BossUI] UpdateBossHealth({currentHealth}) skipped (bossHealthBarRange is null)");
			return;
		}

		bossHealthBarRange.Value = currentHealth;
		GD.Print($"[BossUI] UpdateBossHealth -> bar.Value={bossHealthBarRange.Value}/{bossHealthBarRange.MaxValue}");
	}
}
