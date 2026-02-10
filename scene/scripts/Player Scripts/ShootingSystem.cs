using Godot;

public class ShootingSystem
{
	private int maxShots;
	private float bulletCooldown;
	private PackedScene bulletScene;
	private Node2D bulletContainer;
	private ProgressBar[] bulletBars;

	private int availableShots;
	private float[] shotTimers;

	public ShootingSystem(int maxShots, float cooldown, PackedScene scene, 
						  Node2D container, ProgressBar[] bars)
	{
		this.maxShots = maxShots;
		bulletCooldown = cooldown;
		bulletScene = scene;
		bulletContainer = container;
		bulletBars = bars;

		availableShots = maxShots;
		shotTimers = new float[maxShots];
		for (int i = 0; i < maxShots; i++)
		{
			shotTimers[i] = 0f;
		}
	}

	public void TryShoot(Vector2 direction, Vector2 playerPosition)
	{
		if (!Input.IsActionJustPressed("shoot") || availableShots <= 0 || direction == Vector2.Zero)
			return;

		GD.Print("Shooting bullet! Direction: ", direction);
		SpawnBullet(direction.Normalized(), playerPosition);

		for (int i = 0; i < maxShots; i++)
		{
			if (shotTimers[i] <= 0)
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
			if (shotTimers[i] > 0)
			{
				shotTimers[i] -= dt;

				//  Added null checks here
				if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
				{
					float progress = 100f * (1 - (shotTimers[i] / bulletCooldown));
					bulletBars[i].Value = progress;
				}

				if (shotTimers[i] <= 0)
				{
					availableShots++;
					shotTimers[i] = 0;

					//  Added null checks here
					if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
					{
						bulletBars[i].Value = 100f;
					}
				}
			}
			else
			{
				// Added null checks here
				if (bulletBars != null && i < bulletBars.Length && bulletBars[i] != null)
				{
					bulletBars[i].Value = 100f;
				}
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
		Node root =  bulletContainer.GetTree().Root;
		root.AddChild(bullet);
		bullet.GlobalPosition = spawnPosition;

		GD.Print("Bullet spawned at ", bullet.Position, " with direction ", direction);
	}
}
