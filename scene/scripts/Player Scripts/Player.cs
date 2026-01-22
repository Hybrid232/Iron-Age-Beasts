using Godot;
using System;

public partial class Player : CharacterBody2D
{
	/*
	*---MOVEMENT SETTINGS---
	*/
	
	// Walking Speed of Player.
	[Export] private int playerSpeed = 130;
	
	// Speed applied to dodge.
	[Export] private int dodgeSpeed = 200;
	
	// How long the dodge lasts in seconds.
	[Export] private float dodgeTime = 0.20f;
	
	// How long before teh player can dodge again.
	[Export] private float dodgeCooldown = 0.7f;

	/*
	*---ATTACK SETTINGS---
	*/
	
	// Total time an attack is active.
	[Export] private float attackDuration = 0.25f;
	
	// Distance the attack hitbox is pushed away from the player.
	[Export] private float attackRange = 16f;

	/*
	*---NODE REFERENCES---
	*/
	
	// Pivot node that moves and rotates the attack hitbox.
	[Export] private Node2D attackPivot;
	
	//Area2D that detects enemies during an attack.
	[Export] private Area2D attackHitbox;

	/*
	*---STATE VARIABLES---
	*/
	
	// Timer for actions.
	private float dodgeTimer = 0f;
	private float cooldownTimer = 0f;
	private float attackTimer = 0f;

	// State flags. 
	private bool isDodging = false;
	private bool isAttacking = false;

	
	private Vector2 dodgeDirection;
	private Vector2 currentVelocity;
	private Vector2 lastMoveDirection = Vector2.Down;


	

	public override void _Ready()
	{
		// Ensures that the attack hitbox is disabled when the game starts.
		EnableAttackHitbox(false);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Reduce dodge cooldown overtime.
		if (cooldownTimer > 0)
			cooldownTimer -= dt;

		// Ensures only one action at a time.
		if (isAttacking)
		{
			UpdateAttack(dt);
		}
		else if (isDodging)
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
			HandleMovement();
			TryAttack();
			TryDodge();
		}

		Velocity = currentVelocity;
		MoveAndSlide();
	}

	/* 
	 *---MOVEMENT---
	 */
	private void HandleMovement()
	{
		// Read input as direction vector
		currentVelocity = Input.GetVector(
			"ui_left",
			"ui_right",
			"ui_up",
			"ui_down"
		) * playerSpeed;

		// Save last direction so attacks/dodges know the way to go.
		if (currentVelocity != Vector2.Zero)
		{
			lastMoveDirection = currentVelocity.Normalized();
		}
	}

	

	/* =====================
	 * DODGE
	 * ===================== */
	private void TryDodge()
	{
		if (Input.IsActionJustPressed("dodge") &&
			cooldownTimer <= 0 &&
			lastMoveDirection != Vector2.Zero)
		{
			isDodging = true;
			dodgeDirection = lastMoveDirection;
			dodgeTimer = dodgeTime;
			cooldownTimer = dodgeCooldown;
		}
	}

	/* =====================
	 * ATTACK
	 * ===================== */
	private void TryAttack()
	{
		// Only trigger on button press.
		if (!Input.IsActionJustPressed("attack"))
			return;
			
		// Do not attack without a direciton.
		if (lastMoveDirection == Vector2.Zero)
			return;

		isAttacking = true;
		attackTimer = attackDuration;
		
		// Lock movement during attack.
		currentVelocity = Vector2.Zero;

		
		
		PlayAttackAnimation();
		StartAttack();
	}
	
	private void StartAttack()
	{
		// Normalize direction so diagonal attacks are not stronger.
		Vector2 attackDir = lastMoveDirection.Normalized();
		
		// Move the pivot outward from the player.
		attackPivot.Position = attackDir * attackRange;
		
		// Rotate pivot so hitbox faces the attack direction.
		attackPivot.Rotation = attackDir.Angle();
		
		// Enable collison detection for enemies
		EnableAttackHitbox(true);
	}

	private void UpdateAttack(float dt)
	{
		// Reduce remaining attack time.
		attackTimer -= dt;

		// End attack when timer expires.
		if (attackTimer <= 0)
		{
			isAttacking = false;
			EnableAttackHitbox(false);
		}
	}

	

	private void EnableAttackHitbox(bool enabled)
	{
		// Monitoring = detects collisions.
		// Monitorable = can be detected by others.
		attackHitbox.Monitoring = enabled;
		attackHitbox.Monitorable = enabled;
	}

	private void PlayAttackAnimation()
	{
		// animationPlayer.Play($"attack_{facing.ToString().ToLower()}");
	}
}
