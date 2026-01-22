using Godot;
using System;

public partial class Player : CharacterBody2D
{
	/*
	*---MOVEMENT SETTINGS---
	*/
	[Export] private int playerSpeed = 50; 		 // Player Speed.
	[Export] private int dodgeSpeed = 200;	 	 // Speed when Dodging.
	[Export] private float dodgeTime = 0.20f;	 // Duration of dodge.
	[Export] private float dodgeCooldown = 0.7f; // Dodge Cooldown.
	
	/*
	*---STAMINA SETTINGS---
	*/
	
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
		
		// Cooldown ticking down.
		if (cooldownTimer > 0)
			cooldownTimer -= dt;
		
		if (isDodging)
		{
			// Move in dodged direction.
			currentVelocity = dodgeDirection * dodgeSpeed;
			
			// Reduce dodge timer.
			dodgeTimer -= dt;
			
			if (dodgeTimer <= 0)
			{
				isDodging = false;
				currentVelocity = Vector2.Zero; // Reset Velocity.
			}
		}
		else
		{
			// Normal Movement
			handelInput();
			
			// Update last non-zero movement direction.
			if (currentVelocity != Vector2.Zero)
				lastMoveDirection = currentVelocity.Normalized();
			
			// Check dodge input if cooldown is finished.
			if (Input.IsActionJustPressed("dodge") && cooldownTimer <= 0 && lastMoveDirection != Vector2.Zero)
			{
				isDodging = true;
				dodgeDirection = lastMoveDirection;
				dodgeTimer = dodgeTime;
				cooldownTimer = dodgeCooldown;
			}
		}
		
		// Apply Movement.
		Velocity = currentVelocity;
		MoveAndSlide();
	}
	
	private void handelInput()
	{
		currentVelocity = Input.GetVector("ui_left","ui_right","ui_up","ui_down");
		currentVelocity *= playerSpeed;
	}
	
	//private void attack()
	//{
		//
	//}
}
