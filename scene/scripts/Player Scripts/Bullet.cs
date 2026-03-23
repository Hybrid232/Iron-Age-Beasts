using Godot;
using System;

public partial class Bullet : Area2D
{
	[Export] private float speed = 300f;
	[Export] private float lifetime = 3f;
	[Export] private int damage = 2;

	[Export] private PackedScene hitParticlesScene;

	private Vector2 direction = Vector2.Right;
	private bool _didHit = false;

	public override void _Ready()
	{
		if (HasMeta("direction"))
			direction = (Vector2)GetMeta("direction");

		if (direction.LengthSquared() > 0.0001f)
			direction = direction.Normalized();
		else
			direction = Vector2.Right;

		Rotation = direction.Angle();

		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;

		TopLevel = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_didHit) return;

		float dt = (float)delta;
		GlobalPosition += direction * speed * dt;

		lifetime -= dt;
		if (lifetime <= 0)
			QueueFree();
	}

	private static GpuParticles2D FindGpuParticles2D(Node root)
	{
		if (root is GpuParticles2D pRoot)
			return pRoot;

		foreach (Node child in root.GetChildren())
		{
			var found = FindGpuParticles2D(child);
			if (found != null) return found;
		}
		return null;
	}

	private void SpawnHitParticles(Vector2 worldPos)
	{
		if (hitParticlesScene == null)
		{
			GD.PrintErr("Bullet: hitParticlesScene is NULL. Drag your VFX .tscn into the Bullet inspector.");
			return;
		}

		Node vfx = hitParticlesScene.Instantiate();

		// Add to current scene (so it survives when bullet frees)
		var root = GetTree().CurrentScene ?? GetTree().Root;
		root.AddChild(vfx);

		// Force position / draw order
		if (vfx is Node2D vfx2D)
		{
			vfx2D.TopLevel = true;
			vfx2D.GlobalPosition = worldPos;
			vfx2D.GlobalRotation = GlobalRotation;

			// Make it render above almost everything (debug)
			vfx2D.ZAsRelative = false;
			vfx2D.ZIndex = 9999;
		}

		var particles = FindGpuParticles2D(vfx);
		if (particles == null)
		{
			GD.PrintErr("Bullet: No GpuParticles2D found inside hitParticlesScene.");
			return;
		}

		// Force visibility (debug)
		particles.Visible = true;
		particles.OneShot = true;

		// Make it BIG (debug). Lower/remove once you see it.
		particles.Scale = new Vector2(2.5f, 2.5f);

		// Restart burst
		particles.Emitting = false;
		particles.Restart();
		particles.Emitting = true;

		GD.Print($"Bullet: particles found '{particles.Name}' lifetime={particles.Lifetime} emitting={particles.Emitting}");

		float seconds = Mathf.Max(0.05f, (float)particles.Lifetime);
		GetTree().CreateTimer(seconds).Timeout += () =>
		{
			if (IsInstanceValid(vfx))
				vfx.QueueFree();
		};
	}

	private void HitAndDie(Vector2 hitPos)
	{
		if (_didHit) return;
		_didHit = true;

		SpawnHitParticles(hitPos);
		QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_didHit) return;

		if (body is IDamageable damageable)
			damageable.TakeDamage(damage);

		HitAndDie(GlobalPosition);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (_didHit) return;

		if (area.GetParent() is IDamageable damageable)
			damageable.TakeDamage(damage);

		HitAndDie(GlobalPosition);
	}
}
