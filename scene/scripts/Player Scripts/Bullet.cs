using Godot;
using System;

public partial class Bullet : Area2D
{
	[Export] private float speed = 300f;
	[Export] private float lifetime = 3f;
	[Export] private int damage = 2;
	
	private Vector2 direction = Vector2.Right;
	
	public override void _Ready()
	{
		// Set bullet direction from metadata
		if(HasMeta("direction"))
		{
			direction = (Vector2)GetMeta("direction");
		}
		GD.Print("Bullet direction: ", direction);
		
		// Connect collision signal
		BodyEntered += OnBodyEntered;
		AreaEntered += OnAreaEntered;
		
		// Make bullet independent of parent transforms
		TopLevel = true;
	}

	public override void _PhysicsProcess(double delta) 
	{
		float dt = (float)delta;
		
		// Move bullet in world space
		GlobalPosition += direction * speed * dt;
		
		// Destroy bullet after lifetime expires
		lifetime -= dt;
		if(lifetime <= 0)
		{
			QueueFree();
		}
	}
	
	// Handle collision with CharacterBody2D (like enemies)
	private void OnBodyEntered(Node2D body)
	{
		GD.Print($"Bullet hit body: {body.Name}");
		
		// Check if the body can take damage
		if (body is IDamageable damageable)
		{
			damageable.TakeDamage(damage);
			GD.Print($"Bullet dealt {damage} damage to {body.Name}");
			QueueFree(); // Destroy bullet after hit
		}
	}
	
	// Handle collision with Area2D (if needed)
	private void OnAreaEntered(Area2D area)
	{
		GD.Print($"Bullet hit area: {area.Name}");
		
		// Check if the area's parent can take damage
		if (area.GetParent() is IDamageable damageable)
		{
			damageable.TakeDamage(damage);
			GD.Print($"Bullet dealt {damage} damage to {area.GetParent().Name}");
			QueueFree(); // Destroy bullet after hit
		}
	}
}
