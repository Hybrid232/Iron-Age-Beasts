using System.Numerics;
using Godot;
using Vector2 = Godot.Vector2;

public partial class Player : CharacterBody2D, IDamageable
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
	public bool CanMove { get; set; } = true;

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

	

	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove) //checks bool to see if the player is in a state where
		//they should not be able to move (e.g. during dialogue, cutscene, etc.)
		{
			Velocity = Vector2.Zero;
			return;
		}
		
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

	// Public API for systems to interact with player
	//public Vector2 GetGlobalPosition() => GlobalPosition;
	
	// Inside Player.cs

	public void TriggerHitRecoil(Vector2 pushDirection) 
	{
		// This uses your existing recoilSystem from the Player class
		recoilSystem.StartHitRecoil(pushDirection);
	}
	
	public void TakeDamage(int damage)
	{
		// This is where the enemy "touches" your health
		healthSystem.ChangeHealth(-damage);
		
		// You can also trigger your existing recoil here!
		TriggerHitRecoil(Vector2.Zero);
		
		GD.Print($"Ouch! Player health: {healthSystem.CurrentHealth}");
	}
	

	
}
