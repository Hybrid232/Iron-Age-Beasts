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
	[Export] private int potionHealAmount = 30;

	[ExportGroup("Death")]
	[Export] private bool resetSceneOnDeath = true;

	[ExportGroup("Combat Feel")]
	[Export] private bool enableAttackerRecoilOnHit = true;

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

	// i-frames tuning (normalized 0..1 within the dodge)
	[Export(PropertyHint.Range, "0,1,0.01")]
	private float dodgeIFrameStartNormalized = 0.10f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	private float dodgeIFrameDurationNormalized = 0.50f;

	// ===== RECOIL EXPORTS =====
	[ExportGroup("Recoil System")]
	[Export] private float hitRecoilDistance = 59f;
	[Export] private float hitRecoilTime = 0.3f;
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
	[Export] private AudioStreamPlayer halberdSFX;
	[Export] private AudioStream halberdSoundFile;

	// ===== SHOOTING EXPORTS =====
	[ExportGroup("Shooting System")]
	[Export] private int maxShots = 3;
	[Export] private float bulletCooldown = 30f;
	[Export] private PackedScene bulletScene;
	[Export] private Node2D bulletContainer;
	[Export] private ProgressBar[] bulletBars;
	[Export] private AudioStreamPlayer gunSFX;
	[Export] private AudioStream gunSoundFile;

	// ======= AUDIO EXPORTS =======
	[ExportGroup("Audio System")]
	[Export] private AudioStreamPlayer walkSFX;
	[Export] private AudioStream walkSoundFile;
	[Export] private AudioStreamPlayer swingSFX;
	[Export] private AudioStream swingSoundFile;

	// System Components (not exported)
	private HealthSystem healthSystem;
	private MovementSystem movementSystem;
	private DodgeSystem dodgeSystem;
	private MeleeSystem meleeSystem;
	private ShootingSystem shootingSystem;
	private RecoilSystem recoilSystem;
	private PotionSystem potionSystem;

	public bool CanMove { get; set; } = true;

	// Single source of truth: can the player be “hit” right now?
	// (Use this to block damage AND knockback/stagger.)
	public bool IsInvulnerable => dodgeSystem != null && dodgeSystem.IsInIFrames;

	public override void _Ready()
{
	AddToGroup(BaseEnemy.PLAYER_GROUP);
	CollisionLayer = 1;
	CollisionMask = 2;

	healthSystem = new HealthSystem(
		maxHealth,
		maxStamina,
		uiReference,
		softExhaustThreshold,
		hardExhaustThreshold
	);

		movementSystem = new MovementSystem(playerSpeed);

		dodgeSystem = new DodgeSystem(
			dodgeSpeed,
			dodgeTime,
			dodgeCooldown,
			dodgeStaminaCost,
			dodgeIFrameStartNormalized,
			dodgeIFrameDurationNormalized
		);

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
			healthSystem,
			halberdSFX,
			halberdSoundFile
		);

		shootingSystem = new ShootingSystem(
			maxShots,
			bulletCooldown,
			bulletScene,
			bulletContainer,
			bulletBars,
			gunSFX,
			gunSoundFile
		);

		meleeSystem.Initialize();

		potionSystem = new PotionSystem(potionHealAmount, healthSystem, uiReference);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove)
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

			if (swingSFX != null && !swingSFX.Playing) swingSFX.Play();
		}
		else if (dodgeSystem.IsDodging)
		{
			Velocity = dodgeSystem.UpdateDodge(dt);
		}
		else
		{
			Vector2 moveDirection = movementSystem.HandleInput();

			float speedMultiplier = 1f;
			if (healthSystem.IsBelowSoftThreshold())
				speedMultiplier = lowStaminaSpeedMultiplier;

			Velocity = movementSystem.GetVelocity(moveDirection, speedMultiplier);

			// Try actions
			if (dodgeSystem.TryDodge(moveDirection, healthSystem))
				Velocity = dodgeSystem.GetDodgeVelocity();

			meleeSystem.TryAttack(moveDirection, recoilSystem);
			shootingSystem.TryShoot(moveDirection, GlobalPosition);

			// Walk SFX
			if (moveDirection != Vector2.Zero)
			{
				if (walkSFX != null && !walkSFX.Playing) walkSFX.Play();
			}
			else
			{
				walkSFX?.Stop();
			}
		}

		MoveAndSlide();

		potionSystem.TryUsePotion();
	}

	// Defender recoil (when you take damage) - default strength
	public void TriggerHitRecoil(Vector2 pushDirection)
	{
		// NEW: no knockback/stagger during successful dodge i-frames
		if (IsInvulnerable)
			return;

		GD.Print("========== TriggerHitRecoil called ==========");
		GD.Print($"Push direction: {pushDirection}");
		recoilSystem.StartHitRecoil(pushDirection);
		GD.Print($"Is in recoil now? {recoilSystem.IsInRecoil()}");
	}

	// Defender recoil with custom strength (boss can override distance/time)
	public void TriggerHitRecoil(Vector2 pushDirection, float distance, float time)
	{
		GD.Print($"[Player] Knockback attempt | Invulnerable={IsInvulnerable}");

		if (IsInvulnerable)
		{
			GD.Print("❌ Knockback BLOCKED by i-frames");
			return;
		}

		GD.Print("✅ Knockback APPLIED");

		recoilSystem.StartHitRecoil(pushDirection, distance, time);
	}

	// Attacker recoil (when you successfully hit something)
	public void TriggerAttackerRecoil(Vector2 attackDirectionTowardEnemy)
	{
		if (!enableAttackerRecoilOnHit) return;
		recoilSystem.StartPlayerRecoil(attackDirectionTowardEnemy);
	}

	public void TakeDamage(int damage)
	{
		// Still good to keep: ignore damage during i-frames
		if (IsInvulnerable)
			return;

		healthSystem.ChangeHealth(-damage);
		GD.Print($"Ouch! Player health: {healthSystem.CurrentHealth}");

		if (healthSystem.CurrentHealth <= 0)
			HandleDeath();
	}

	private void HandleDeath()
	{
		GD.Print("Player died!");

		CanMove = false;
		Velocity = Vector2.Zero;

		if (resetSceneOnDeath)
			GetTree().CallDeferred("reload_current_scene");
	}

	// If anything uses this to knock you back, block it during i-frames too.
	public void ApplyKnockback(Vector2 force)
	{
		// NEW: no knockback during successful dodge i-frames
		if (IsInvulnerable)
			return;

		if (force.Length() > 0)
		{
			Vector2 direction = force.Normalized();
			recoilSystem.StartHitRecoil(direction);
			GD.Print($"Knockback applied via recoil system! Force: {force}");
		}
	}
}
