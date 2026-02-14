using Godot;

public partial class Player : CharacterBody2D
{
	// ===== HEALTH/STAMINA EXPORTS =====
	[ExportGroup("Health System")]
	[Export] private UI uiReference;
	[Export] private int maxHealth = 100;
	[Export] private int maxStamina = 100;
	
	

	// ===== MOVEMENT EXPORTS =====
	[ExportGroup("Movement System")]
	[Export] private int playerSpeed = 130;
	[Export] private float exhaustedSpeedMultiplier = 0.4f;
	[Export] private int softExhaustThreshold = 20;
	[Export] private int hardExhaustThreshold = 20;
	[Export] private float lowStaminaSpeedMultiplier = 0.6f;

	// ===== DODGE EXPORTS =====
	[ExportGroup("Dodge System")]
	[Export] private int dodgeSpeed = 200;
	[Export] private float dodgeTime = 0.20f;
	[Export] private float dodgeCooldown = 0.7f;
	[Export] private int dodgeStaminaCost = 20;

	// ===== RECOIL EXPORTS =====
	[ExportGroup("Recoil System")]
	[Export] private float hitRecoilDistance = 12f;
	[Export] private float hitRecoilTime = 0.06f;
	[Export] private float playerRecoilDistance = 12f;
	[Export] private float recoilTime = 0.06f;

	// ===== MELEE EXPORTS =====
	[ExportGroup("Melee System")]
	[Export] private Node2D attackPivot;
	[Export] private Area2D attackHitbox;
	[Export] private float attackDuration = 0.25f;
	[Export] private float attackRange = 16f;
	[Export] private float enemyKnockbackDistance = 80f;
	[Export] private float enemyKnockbackTime = 0.10f;
	[Export] private int meleeDamage = 15; 
	[Export] private int staminaCost = 20;
	[Export] private int staminaBuffer = 5;

	// ===== SHOOTING EXPORTS =====
	[ExportGroup("Shooting System")]
	[Export] private int maxShots = 3;
	[Export] private float bulletCooldown = 30f;
	[Export] private PackedScene bulletScene;
	[Export] private Node2D bulletContainer;
	[Export] private ProgressBar[] bulletBars;

	// System Components (not exported)
	private HealthSystem healthSystem;
	private MovementSystem movementSystem;
	private DodgeSystem dodgeSystem;
	private MeleeSystem meleeSystem;
	private ShootingSystem shootingSystem;
	private RecoilSystem recoilSystem;

	public override void _Ready()
	{
		// Initialize all systems with exported values
		healthSystem = new HealthSystem(
		maxHealth,
		maxStamina,
		uiReference,
		softExhaustThreshold,
		hardExhaustThreshold
		);		
		movementSystem = new MovementSystem(playerSpeed);
		dodgeSystem = new DodgeSystem(dodgeSpeed, dodgeTime, dodgeCooldown, dodgeStaminaCost);
		recoilSystem = new RecoilSystem(hitRecoilDistance, hitRecoilTime, playerRecoilDistance, recoilTime);
		
		
		meleeSystem = new MeleeSystem(
			attackPivot, 
			attackHitbox, 
			attackDuration, 
			attackRange, 
			enemyKnockbackDistance, 
			enemyKnockbackTime, 
			meleeDamage,
			staminaCost,
			staminaBuffer,
			this,
			healthSystem
		);
		
		shootingSystem = new ShootingSystem(maxShots, bulletCooldown, bulletScene, bulletContainer, bulletBars);

		meleeSystem.Initialize();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		healthSystem.HandleDebugInput(@event);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Update all systems
		recoilSystem.Update(dt);
		healthSystem.Update(dt);
		dodgeSystem.UpdateCooldown(dt);
		shootingSystem.UpdateCooldowns(dt);

		// Priority: Recoil > Attacking > Dodging > Normal Movement
		if (recoilSystem.IsInRecoil())
		{
			Velocity = recoilSystem.GetRecoilVelocity();
			MoveAndSlide();
			return;
		}

		if (meleeSystem.IsAttacking)
		{
			meleeSystem.UpdateAttack(dt);
			Velocity = Vector2.Zero;
		}
		else if (dodgeSystem.IsDodging)
		{
			Velocity = dodgeSystem.UpdateDodge(dt);
		}
		else
		{
			// Normal gameplay
			Vector2 moveDirection = movementSystem.HandleInput();
			
			float speedMultiplier = 1f;
			
			if (healthSystem.IsBelowSoftThreshold())
			{
				speedMultiplier = lowStaminaSpeedMultiplier;
			}
			
			Velocity = movementSystem.GetVelocity(moveDirection, speedMultiplier);

			// Try actions
			if (dodgeSystem.TryDodge(moveDirection, healthSystem))
			{
				Velocity = dodgeSystem.GetDodgeVelocity();
			}

			meleeSystem.TryAttack(moveDirection, recoilSystem);
			shootingSystem.TryShoot(moveDirection, GlobalPosition);
		}

		MoveAndSlide();
	}

<<<<<<< HEAD
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
=======
	// Public API for systems to interact with player
	public Vector2 GetGlobalPosition() => GlobalPosition;
	public void TriggerHitRecoil(Vector2 pushDirection) => recoilSystem.StartHitRecoil(pushDirection);
>>>>>>> 170743f696e2d400bf6b36690254207e8d364899
}
