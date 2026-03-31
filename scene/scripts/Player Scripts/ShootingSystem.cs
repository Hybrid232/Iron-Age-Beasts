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

	// Delayed bullet spawn: bullet fires on frame 4 of a 7-frame animation.
	// bulletSpawnDelay is set by Player as (4f / 7f) * ShootDurationSeconds.
	private bool _pendingShot = false;
	private float _pendingBulletTimer = 0f;
	private float _pendingBulletDelay = 0f;
	private Vector2 _pendingDirection = Vector2.Zero;
	private Vector2 _pendingPosition = Vector2.Zero;

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

	/// <summary>
	/// Returns true if the shoot animation should begin this frame.
	/// The bullet itself is spawned later via UpdatePendingShot().
	/// </summary>
	public bool TryShoot(Vector2 direction, Vector2 playerPosition, float bulletSpawnDelay)
	{
		if (!Input.IsActionJustPressed("shoot") || availableShots <= 0 || direction == Vector2.Zero)
			return false;

		// Queue the bullet — it spawns after bulletSpawnDelay seconds (frame 4 of the animation)
		_pendingShot = true;
		_pendingBulletTimer = bulletSpawnDelay;
		_pendingBulletDelay = bulletSpawnDelay;
		_pendingDirection = direction.Normalized();
		_pendingPosition = playerPosition;

		// Consume a shot charge immediately so UI reflects it at once
		for (int i = 0; i < maxShots; i++)
		{
			if (shotTimers[i] <= 0f)
			{
				shotTimers[i] = bulletCooldown;
				availableShots--;
				break;
			}
		}

		return true;
	}

	/// <summary>
	/// Tick the pending bullet timer. Call once per physics frame.
	/// Pass the player's current GlobalPosition so the spawn origin stays accurate.
	/// </summary>
	public void UpdatePendingShot(float dt, Vector2 playerPosition)
	{
		if (!_pendingShot)
			return;

		_pendingBulletTimer -= dt;

		if (_pendingBulletTimer <= 0f)
		{
			_pendingShot = false;

			// Use latest player position so the bullet origin is never stale
			SpawnBullet(_pendingDirection, playerPosition);

			if (gunSFX != null)
				gunSFX.Play();
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
