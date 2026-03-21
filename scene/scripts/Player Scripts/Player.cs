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

	[ExportGroup("Starting Consumables")]
	[Export] private int startingPotions = 1;

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

	// ======= UPGRADES =======
	[ExportGroup("Upgrades")]
	[Export] private int healthUpgradeAmount = 15;
	[Export] private int staminaUpgradeAmount = 10;
	[Export] private int damageUpgradeAmount = 2;

	public int HealthUpgradeLevel { get; private set; } = 1;
	public int StaminaUpgradeLevel { get; private set; } = 1;
	public int DamageUpgradeLevel { get; private set; } = 1;

	// System Components (not exported)
	private HealthSystem healthSystem;
	private MovementSystem movementSystem;
	private DodgeSystem dodgeSystem;
	private MeleeSystem meleeSystem;
	private ShootingSystem shootingSystem;
	private RecoilSystem recoilSystem;
	private PotionSystem potionSystem;

	public bool CanMove { get; set; } = true;
	private Vector2 respawnPosition;

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

		respawnPosition = GlobalPosition;

		// Potion system now tracks unlocked potions; starts with 1
		potionSystem = new PotionSystem(potionHealAmount, healthSystem, uiReference, startingPotions);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove)
		{
			Velocity = Vector2.Zero;
			return;
		}

		float dt = (float)delta;

		recoilSystem.Update(dt);
		healthSystem.Update(dt);
		dodgeSystem.UpdateCooldown(dt);
		shootingSystem.UpdateCooldowns(dt);

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

			if (dodgeSystem.TryDodge(moveDirection, healthSystem))
				Velocity = dodgeSystem.GetDodgeVelocity();

			meleeSystem.TryAttack(moveDirection, recoilSystem);
			shootingSystem.TryShoot(moveDirection, GlobalPosition);

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

	// ===== SHOP HELPERS (CALLED BY NPC) =====
	public bool CanBuyPotion() => potionSystem != null && potionSystem.CanBuyPotion();

	// Buying a potion permanently increases refill amount too
	public bool TryAddPotionFromShop(int amount = 1)
	{
		return potionSystem != null && potionSystem.TryBuyPotions(amount);
	}

	public void UpgradeHealthFromShop()
	{
		HealthUpgradeLevel++;
		healthSystem.SetMaxHealth(healthSystem.MaxHealth + healthUpgradeAmount, healToFull: true);
	}

	public void UpgradeStaminaFromShop()
	{
		StaminaUpgradeLevel++;
		healthSystem.SetMaxStamina(healthSystem.MaxStamina + staminaUpgradeAmount, refillToFull: true);
	}

	public void UpgradeDamageFromShop()
	{
		DamageUpgradeLevel++;
		meleeSystem.SetMeleeDamage(meleeSystem.MeleeDamage + damageUpgradeAmount);
	}

	// Defender recoil
	public void TriggerHitRecoil(Vector2 pushDirection)
	{
		if (IsInvulnerable)
			return;

		GD.Print("========== TriggerHitRecoil called ==========");
		GD.Print($"Push direction: {pushDirection}");
		recoilSystem.StartHitRecoil(pushDirection);
		GD.Print($"Is in recoil now? {recoilSystem.IsInRecoil()}");
	}

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

	// Attacker recoil
	public void TriggerAttackerRecoil(Vector2 attackDirectionTowardEnemy)
	{
		if (!enableAttackerRecoilOnHit) return;
		recoilSystem.StartPlayerRecoil(attackDirectionTowardEnemy);
	}

	public void TakeDamage(int damage)
	{
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
		RespawnAndReset();
	}

	public void SetRespawnPoint(Vector2 pos)
	{
		respawnPosition = pos;
	}

	public void RespawnAndReset()
	{
		GlobalPosition = respawnPosition;
		healthSystem.HealToFull();

		// Refill only what player has unlocked/bought
		potionSystem.RefillPotions();

		CanMove = true;

		var enemies = GetTree().GetNodesInGroup("Enemy");
		foreach (Node node in enemies)
		{
			if (node is BaseEnemy enemy)
			{
				enemy.ResetEnemy();
			}
		}
	}

	public void ApplyKnockback(Vector2 force)
	{
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
