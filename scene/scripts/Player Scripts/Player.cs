using Godot;
using System;

public partial class Player : CharacterBody2D
{
	/*
	*---HEALTH SETTINGS---
	*/
	[Export] public int MaxHealth = 10;
	private int _currentHealth;
	/*
	*---STAMINA SETTINGS---
	*/
	[Export] public int MaxStamina = 5;
	private int _currentStamina;
	[Export] public UI UiReference;

	/*
	*---MOVEMENT SETTINGS---
	*/
	
	// Walking Speed of Player.
	[Export] private int playerSpeed = 130;
	
	// Speed applied to dodge.
	[Export] private int dodgeSpeed = 200;
	
	// How long the dodge lasts in seconds.
	[Export] private float dodgeTime = 0.20f;
	
	// How long before the player can dodge again.
	[Export] private float dodgeCooldown = 0.7f;

	/*
	*---ATTACK SETTINGS---
	*/
	
	// Total time an attack is active.
	[Export] private float attackDuration = 0.25f;
	
	// Distance the attack hitbox is pushed away from the player.
	[Export] private float attackRange = 16f;
	
	/*
	*---BULLET SETTINGS---
	*/
	// Max Bullets.
	[Export] private int maxShots = 3;
	
	// Time for each shot to regenerate.
	[Export] private float bulletCooldown = 30f;
	
	// Reference to Bullet scene.
	[Export] private PackedScene bulletScene;
	
	// UI bar to show cooldown.
	[Export] private ProgressBar[] bulletBars;
	
	// Tracks how many shots can currently fire.
	private int availableShots = 3;
	
	// Individual cooldown timers.
	private float[] shotTimers;

	/*
	*---NODE REFERENCES---
	*/
	
	// Pivot node that moves and rotates the attack hitbox.
	[Export] private Node2D attackPivot;
	
	// Area2D that detects enemies during an attack.
	[Export] private Area2D attackHitbox;

	// Parent Node for bullets.	
	[Export] private Node2D bulletContainer;

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

		// Initialize Health
		_currentHealth = MaxHealth;
		// Initialize Stamina
		_currentStamina = MaxStamina;
		
		// Initialize Shooting Timers.
		availableShots = maxShots;
		shotTimers = new float[maxShots];
		for (int i = 0; i < maxShots; i++) 
		{
			// All shots start ready to fire.
			shotTimers[i] = 0f;
		}
	}

	// This handles single key presses (J for Damage, K for Heal)
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
		{
			if (eventKey.Keycode == Key.J) ChangeHealth(-1);
			if (eventKey.Keycode == Key.K) ChangeHealth(1);

			if (eventKey.Keycode == Key.U) ChangeStamina(-1);
			if (eventKey.Keycode == Key.I) ChangeStamina(1);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Reduce dodge cooldown overtime.
		if (cooldownTimer > 0)
			cooldownTimer -= dt;
	
		// Update bullet cooldowns and UI.
		UpdateBulletCooldowns(dt);
		
		// Ensures only one action at a time.
		if (isAttacking)
		{
			UpdateAttack(dt);
		}
		else if (isDodging)
		{
			// Move player in dodge direciton.
			currentVelocity = dodgeDirection * dodgeSpeed;
			dodgeTimer -= dt;

			// End dodge when timer expires.
			if (dodgeTimer <= 0)
			{
				isDodging = false;
				currentVelocity = Vector2.Zero;
			}
		}
		else
		{
			// Handles movement, attacks, dodges, and shooting.
			HandleMovement();
			TryAttack();
			TryDodge();
			TryShoot();
		}
	
		// Apply Movement
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
	
	/*
	*---SHOOTING---
	*/
	private void TryShoot()
	{
		if (!Input.IsActionJustPressed("shoot") || availableShots <= 0 || lastMoveDirection == Vector2.Zero)
		{
			// Debug messages to know why shooting is blocked.
			if (!Input.IsActionJustPressed("shoot"))
				return;
				
			if (availableShots <= 0)
			{
				GD.Print("Cannot shoot: no available shots");
				return;
			}

			if (lastMoveDirection == Vector2.Zero)
			{
				GD.Print("Cannot shoot: no movement direction");
				return;
			}
			
			return;
		}
			
		GD.Print("Shooting bullet! Direction: ", lastMoveDirection);
		SpawnBullet(lastMoveDirection.Normalized());
		
		// Start Cooldown for first available bullet.
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
	
	// Updates cooldown timers for bullets and updates UI bars.
	private void UpdateBulletCooldowns(float dt) 
{
	for (int i = 0; i < maxShots; i++)
	{
		if (shotTimers[i] > 0)
		{
			shotTimers[i] -= dt;

			// Update UI: show % of cooldown completed
			if (bulletBars != null && bulletBars.Length > i && bulletBars[i] != null)
			{
				float progress = 100f * (1 - (shotTimers[i] / bulletCooldown));
				bulletBars[i].Value = progress;
			}
			
			// When cooldown finishes.
			if (shotTimers[i] <= 0)
			{
				availableShots++;
				shotTimers[i] = 0;

				// Fully filled bar.
				if (bulletBars != null && bulletBars.Length > i && bulletBars[i] != null)
				{
					bulletBars[i].Value = 100f;
				}
			}
		}
		else
		{
			// If no cooldown, ensure bar is full.
			if (bulletBars != null && bulletBars.Length > i && bulletBars[i] != null)
			{
				bulletBars[i].Value = 100f;
			}
		}
	}
}
	
	private void SpawnBullet(Vector2 direction)
	{
		// Safety Checks.
		if (bulletScene == null || bulletContainer == null)
		{
			GD.PrintErr("BulletScene or BulletContainer is null!");
			return;
		}
		
		// Instantiate bullet and set its direciton.
		Area2D bullet = (Area2D)bulletScene.Instantiate();
		bullet.SetMeta("direction", direction);
		
		// Add bullet to scene.
		bulletContainer.AddChild(bullet);
		
		// Position bullet at player.
		bullet.GlobalPosition = GlobalPosition;
		
		
		GD.Print("Bullet spawned at ", bullet.Position, " with direction ", direction);
	}

	/*
	*---PLAYER ANIMATIONS---
	*/
	private void PlayAttackAnimation()
	{
		// animationPlayer.Play($"attack_{facing.ToString().ToLower()}");
	}

	/*
	*---HEALTH METHODS---
	*/
	public void ChangeHealth(int amount)
	{
		_currentHealth += amount;
		
		// Keep health between 0 and 10
		_currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
		
		// Update the UI if we attached it
		if (UiReference != null)
		{
			UiReference.UpdateHealthDisplay(_currentHealth);
		}
		else
		{
			GD.PrintErr("UI Reference is missing! Drag the UI node into the Player script in the Inspector.");
		}
	}
	public void ChangeStamina(int amount)
	{
		_currentStamina += amount;

		// Clamp between 0 and MaxStamina (5)
		_currentStamina = Mathf.Clamp(_currentStamina, 0, MaxStamina);

		if (UiReference != null)
		{
			UiReference.UpdateStaminaDisplay(_currentStamina);
		}
	}
}
