using Godot;
using System;

public partial class StevePlayerMovement : CharacterBody2D
{
	
	[Export] private int speed = 50;
	[Export] private int dodgeSpeed = 200;
	[Export] private float dodgeTime = 0.20f;
	[Export] private float dodgeCooldown = 0.7f;
	
	private float dodgeTimer = 0f;
	private float cooldownTimer = 0f;
	private bool isDodging = false;
	private Vector2 dodgeDirection;
	
	private Vector2 currentVelocity;
	private Vector2 lastMoveDirection = Vector2.Zero;
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		
		float dt = (float)delta;
		
		if (cooldownTimer > 0)
			cooldownTimer -= dt;
		
		if (isDodging)
		{
			currentVelocity = dodgeDirection * dodgeSpeed;
			
			dodgeTimer -= dt;
			
			if (dodgeTimer <= 0)
			{
				isDodging = false;
				currentVelocity = Vector2.Zero;
			}
		}
		else
		{
			// Normal Movement
			handelInput();
			
			if (currentVelocity != Vector2.Zero)
				lastMoveDirection = currentVelocity.Normalized();
				
			if (Input.IsActionJustPressed("dodge") && cooldownTimer <= 0 && lastMoveDirection != Vector2.Zero)
			{
				isDodging = true;
				dodgeDirection = lastMoveDirection;
				dodgeTimer = dodgeTime;
				cooldownTimer = dodgeCooldown;
			}
		}
		
		Velocity = currentVelocity;
		MoveAndSlide();
	}
	
	private void handelInput()
	{
		currentVelocity = Input.GetVector("ui_left","ui_right","ui_up","ui_down");
		currentVelocity *= speed;
	}
}
