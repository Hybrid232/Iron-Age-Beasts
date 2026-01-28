using Godot;
using System;

public partial class Bullet : Area2D
{
	[Export] private float speed = 300f;
	[Export] private float lifetime = 3f;
	private Vector2 direction = Vector2.Right;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(HasMeta("direction"))
		{
			direction = (Vector2)GetMeta("direction");
		}
		GD.Print("Bullet direction: ", direction);
	}

	public override void _PhysicsProcess(double delta) 
	{
		float dt = (float)delta;
		Position += direction * speed * dt;
		
		lifetime -=dt;
		if(lifetime <= 0)
		{
			QueueFree();
		}
	}
}
