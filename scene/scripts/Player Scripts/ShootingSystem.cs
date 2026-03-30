using Godot;

public class ShootingSystem
{
	private int maxShots;
	private float bulletCooldown;
	private PackedScene bulletScene;
	private Node2D bulletContainer;
	private ProgressBar[] bulletBars;

	private AudioStreamPlayer gunSFX;
	private AudioStream gunSoundFile;

	// NEW: reload SFX (plays when a shot finishes recharging)
	private AudioStreamPlayer reloadSFX;
	private AudioStream reloadSoundFile;

	private int availableShots;
	private float[] shotTimers;

	public ShootingSystem(
		int maxShots,
		float cooldown,
		PackedScene scene,
		Node2D container,
		ProgressBar[] bars,
		AudioStreamPlayer shootSFX,
		AudioStream shootFile,
		AudioStreamPlayer reloadSFX,
		AudioStream reloadFile)
	{
		this.maxShots = maxShots;
		bulletCooldown = cooldown;
		bulletScene = scene;
		bulletContainer = container;
		bulletBars = bars;

		// Shoot audio
		this.gunSFX = shootSFX;
		this.gunSoundFile = shootFile;
		if (this.gunSFX != null && this.gunSoundFile != null)
			this.gunSFX.Stream = this.gunSoundFile;

		// Reload audio
		this.reloadSFX = reloadSFX;
		this.reloadSoundFile = reloadFile;
		if (this.reloadSFX != null && this.reloadSoundFile != null)
			this.reloadSFX.Stream = this.reloadSoundFile;

		availableShots = maxShots;

		shotTimers = new float[maxShots];
		for (int i = 0; i < maxShots; i++)
			shotTimers[i] = 0f;
	}

	public void TryShoot(Vector2 direction, Vector2 playerPosition)
	{
		if (!Input.IsActionJustPressed("shoot") || availableShots <= 0 || direction == Vector2.Zero)
			return;

		GD.Print("Shooting bullet! Direction: ", direction);
		SpawnBullet(direction.Normalized(), playerPosition);

		// FIX: braces, and null check
		if (gunSFX != null)
		{
			gunSFX.Play();
			GD.Print("Gun Sound Played");
		}

		for (int i = 0; i < maxShots; i++)
		{
			if (shotTimers[i] <= 0f)
			{
				shotTimers[i] = bulletCooldown;
				availableShots--;
				break;
			}
		}
	}

	public void UpdateCooldowns(float dt)
	{
		for (int i = 0; i < maxShots; i++)
		{
			if (shotTimers[i] > 0f)
			{
				shotTimers[i] -= dt;

				// Update UI
				if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
				{
					float progress = 100f * (1f - (shotTimers[i] / bulletCooldown));
					bulletBars[i].Value = progress;
				}

				// Recharge completed THIS FRAME
				if (shotTimers[i] <= 0f)
				{
					availableShots++;
					shotTimers[i] = 0f;

					// UI to full
					if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
						bulletBars[i].Value = 100f;

					// NEW: play reload SFX once per recharge completion
					if (reloadSFX != null)
						reloadSFX.Play();
				}
			}
			else
			{
				// Timer already ready; keep UI full
				if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
					bulletBars[i].Value = 100f;
			}
		}
	}

	private void SpawnBullet(Vector2 direction, Vector2 spawnPosition)
	{
		if (bulletScene == null || bulletContainer == null)
		{
			GD.PrintErr("BulletScene or BulletContainer is null!");
			return;
		}

		Area2D bullet = (Area2D)bulletScene.Instantiate();
		bullet.SetMeta("direction", direction);

		Node root = bulletContainer.GetTree().Root;
		root.AddChild(bullet);

		bullet.GlobalPosition = spawnPosition;

		GD.Print("Bullet spawned at ", bullet.Position, " with direction ", direction);
	}
}
