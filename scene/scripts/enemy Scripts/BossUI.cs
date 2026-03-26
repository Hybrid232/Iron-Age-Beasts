using Godot;

public partial class BossUI : Control, IBossUI
{
	public const string GROUP_NAME = "BossUI";

	[Export] private NodePath bossHealthBarPath;
	private Range bossHealthBarRange;

	public override void _Ready()
	{
		// Make UI discoverable without fragile NodePath/script casting
		AddToGroup(GROUP_NAME);

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
