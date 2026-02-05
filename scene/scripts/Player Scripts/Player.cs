using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] private UI uiReference;
	[Export] private int maxHealth = 100;
	[Export] private int maxStamina = 100;
	private int currentHealth;
	private int currentStamina; 
	
	private void InitializePlayerHealth()
	{
		currentHealth = maxHealth;
		currentStamina = maxStamina;
		if (uiReference != null)
		{
			uiReference.InitializeHealth(maxHealth, currentHealth);
			uiReference.InitializeStamina(maxStamina, currentStamina);
		}
	}
	private void ChangeHealth(int amount)
	{
		currentHealth += amount;   
		// Ensure health stays between 0 and Max
		currentHealth = Math.Clamp(currentHealth, 0, maxHealth);
		GD.Print($"Health Changed: {currentHealth}/{maxHealth}");
		if (uiReference != null) uiReference.UpdateHealthDisplay(currentHealth);
	}
	private void ChangeStamina(int amount)
	{
		currentStamina += amount;
		// Ensure stamina stays between 0 and Max
		currentStamina = Math.Clamp(currentStamina, 0, maxStamina);
		GD.Print($"Stamina Changed: {currentStamina}/{maxStamina}");
		if (uiReference != null) uiReference.UpdateStaminaDisplay(currentStamina);
	}
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
	
	[Export] private float enemyKnockbackTime = 0.10f; // add this

		/*
	*---HIT RECOIL (PLAYER) SETTINGS---
	*/
	[Export] private float hitRecoilDistance = 12f;  // push player a little on HIT
	[Export] private float hitRecoilTime = 0.06f;    // duration of the push

	private float hitRecoilTimer = 0f;
	private Vector2 hitRecoilVelocity = Vector2.Zero;

	// Only recoil once per attack swing
	private bool usedHitRecoilThisAttack = false;

	/*
	*---ATTACK SETTINGS---
	*/
	
		/*
	*---ATTACK KNOCKBACK SETTINGS---
	*/
	[Export] private float enemyKnockbackDistance = 80f;   // bigger push
	[Export] private float playerRecoilDistance = 12f;     // small push
	[Export] private float recoilTime = 0.06f;             // how long recoil lasts (seconds)
	[Export] private int hitsToKillEnemy = 3;

	
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

	// Parent Node for bullets.	p
	[Export] private Node2D bulletContainer;

	/*
	*---STATE VARIABLES---
	*/
		// Recoil
	private float recoilTimer = 0f;
	private Vector2 recoilVelocity = Vector2.Zero;

	// Track hits per enemy and prevent multi-hits in same swing
	private readonly System.Collections.Generic.Dictionary<ulong, int> enemyHitCounts
		= new System.Collections.Generic.Dictionary<ulong, int>();

	private readonly System.Collections.Generic.HashSet<ulong> enemiesHitThisAttack
		= new System.Collections.Generic.HashSet<ulong>();

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
		InitializePlayerHealth();

		// Ensures that the attack hitbox is disabled when the game starts.
		EnableAttackHitbox(false);
		
		// Initialize Shooting Timers.
		availableShots = maxShots;
		shotTimers = new float[maxShots];
		for (int i = 0; i < maxShots; i++) 
		{
			// All shots start ready to fire.
			shotTimers[i] = 0f;
		}
				// Listen for things hit by the melee Area2D.
		attackHitbox.AreaEntered += OnAttackAreaEntered;
		attackHitbox.BodyEntered += OnAttackBodyEntered;
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
		{
			if (eventKey.Keycode == Key.J) ChangeHealth(-5);
			if (eventKey.Keycode == Key.K) ChangeHealth(5);

			if (eventKey.Keycode == Key.U) ChangeStamina(-5);
			if (eventKey.Keycode == Key.I) ChangeStamina(5);
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
				// Apply player recoil ONLY when a hit happens.
		if (hitRecoilTimer > 0f)
		{
			hitRecoilTimer -= dt;
			currentVelocity = hitRecoilVelocity;

			if (hitRecoilTimer <= 0f)
				hitRecoilVelocity = Vector2.Zero;
			return;
		}

				// Apply short recoil velocity (player gets pushed slightly back).
		if (recoilTimer > 0f)
		{
			recoilTimer -= dt;
			currentVelocity = recoilVelocity;

			if (recoilTimer <= 0f)
				recoilVelocity = Vector2.Zero;
			return;
		}


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
				// Start a new swing hit-list so enemies only count once per attack.
		enemiesHitThisAttack.Clear();
		usedHitRecoilThisAttack = false;


		// Small recoil on the player opposite the attack direction.
		StartPlayerRecoil(lastMoveDirection);

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

		private void StartHitRecoil(Vector2 pushDirFromPlayerToEnemy)
	{
		// pushDirFromPlayerToEnemy points from player -> enemy
		if (pushDirFromPlayerToEnemy == Vector2.Zero)
			return;

		Vector2 awayFromEnemy = -pushDirFromPlayerToEnemy.Normalized();

		float speed = (hitRecoilTime > 0f) ? (hitRecoilDistance / hitRecoilTime) : 0f;

		hitRecoilVelocity = awayFromEnemy * speed;
		hitRecoilTimer = hitRecoilTime;
	}


	private void EnableAttackHitbox(bool enabled)
	{
		// Monitoring = detects collisions.
		// Monitorable = can be detected by others.
		attackHitbox.Monitoring = enabled;
		attackHitbox.Monitorable = enabled;
	}
	
		private void StartPlayerRecoil(Vector2 attackDirection)
	{
		if (attackDirection == Vector2.Zero)
			return;

		Vector2 dir = attackDirection.Normalized();

		// Convert "distance over time" into a velocity.
		float speed = (recoilTime > 0f) ? (playerRecoilDistance / recoilTime) : 0f;

		recoilVelocity = -dir * speed;
		recoilTimer = recoilTime;
	}
	
private void HandleMeleeHit(Node hitNode)
{
	// Only count hits while the attack is active
	if (!isAttacking)
		return;

	if (hitNode == null || hitNode == this)
		return;
	// Resolve to the enemy ROOT (CharacterBody2D / BaseEnemy)
	Node2D enemyRoot = GetEnemyRootFromHit(hitNode);
	if (enemyRoot == null)
		return;

	ulong id = enemyRoot.GetInstanceId();

	// Prevent multiple hits on same enemy during one swing
	if (enemiesHitThisAttack.Contains(id))
		return;

	enemiesHitThisAttack.Add(id);

	// Direction from player -> enemy
	Vector2 pushDir = (enemyRoot.GlobalPosition - GlobalPosition).Normalized();

	// Player recoil once per swing when we successfully hit something
	if (!usedHitRecoilThisAttack)
	{
		StartHitRecoil(pushDir);
		usedHitRecoilThisAttack = true;
	}

	// Push enemy
	ApplyEnemyKnockback(enemyRoot, pushDir);

	// Count hits
	if (!enemyHitCounts.ContainsKey(id))
		enemyHitCounts[id] = 0;

	enemyHitCounts[id]++;

	GD.Print($"Hit enemy {enemyRoot.Name} ({id}) = {enemyHitCounts[id]}/{hitsToKillEnemy}");

	// Kill after N hits
	if (enemyHitCounts[id] >= hitsToKillEnemy)
	{
		enemyHitCounts.Remove(id);
		enemiesHitThisAttack.Remove(id);

		GD.Print($"KILLING enemy {enemyRoot.Name} ({id})");

		if (enemyRoot.IsInsideTree())
			enemyRoot.QueueFree();
	}
}


	private void OnAttackAreaEntered(Area2D area)
	{
		HandleMeleeHit(area);
	}

	private void OnAttackBodyEntered(Node2D body)
	{
		HandleMeleeHit(body);
	}

	

	
private void ApplyEnemyKnockback(Node enemyNode, Vector2 pushDir)
{
	if (pushDir == Vector2.Zero)
		return;

	// Prefer BaseEnemy knockback (best)
	if (enemyNode is BaseEnemy baseEnemy)
	{
		baseEnemy.ApplyKnockback(pushDir, enemyKnockbackDistance, enemyKnockbackTime);
		return;
	}

	// Fallbacks (if you hit something else)
	if (enemyNode is CharacterBody2D enemyBody)
	{
		enemyBody.Velocity += pushDir * (enemyKnockbackDistance * 10f);
		return;
	}

	if (enemyNode is Node2D enemy2D)
	{
		enemy2D.GlobalPosition += pushDir * enemyKnockbackDistance;
	}
}

	
private void ApplyEnemyKnockbackPosition(Node enemyNode, Vector2 pushDir)
{
	if (pushDir == Vector2.Zero) return;

	if (enemyNode is Node2D enemy2D)
		enemy2D.GlobalPosition += pushDir * enemyKnockbackDistance;
}

//HELPER METHOT TO PUSH ENEMY BACK

private Node2D GetEnemyRootFromHit(Node hit)
{
	if (hit == null) return null;

	// If the thing we hit IS the enemy root already.
	if (hit is CharacterBody2D cb) return cb;
	if (hit is Node2D n2d && n2d.IsInGroup("enemy")) return n2d;

	// Most common: we hit an Area2D hurtbox that is a CHILD of the enemy root.
	Node current = hit;
	while (current != null)
	{
		if (current is CharacterBody2D body)
			return body;

		if (current is Node2D node2D && node2D.IsInGroup("enemy"))
			return node2D;

		current = current.GetParent();
	}

	return null;
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
}
