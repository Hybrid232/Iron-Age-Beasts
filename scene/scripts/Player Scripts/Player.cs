using Godot;
using Vector2 = Godot.Vector2;

public partial class Player : CharacterBody2D, IDamageable, IStunnable
{
	// ===== HEALTH/STAMINA EXPORTS =====
	[ExportGroup("Health System")]
	[Export] private UI uiReference;
	[Export] private int maxHealth = 100;
	[Export] private int maxStamina = 100;
	[Export] private int potionHealAmount = 30;
	[Export] private AudioStreamPlayer HealthStimSFX;
	[Export] private AudioStream HealthStimFile;

	[ExportGroup("Starting Consumables")]
	[Export] private int startingPotions = 1;

	[ExportGroup("Death")]
	[Export] private bool resetSceneOnDeath = true;

	// ===== Death Screen (already placed in scene) =====
	[ExportGroup("UI Scenes")]
	[Export] private DeathScreenUI _deathScreenUI;

	private bool _isDying = false;

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

	// NEW: Add Dodge SFX exports
	[Export] private AudioStreamPlayer dodgeSFX;
	[Export] private AudioStream dodgeSFXFile;

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
	[Export] private AudioStreamPlayer reloadSFX;
	[Export] private AudioStream reloadSoundFile;

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

	// ===== ANIMATION =====
	[ExportGroup("Animation")]
	[Export] private PlayerAnimationDriver animationDriver;

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

	// ===== STUN STATE =====
	private float _stunTimer = 0f;
	public bool IsStunned => _stunTimer > 0f;

	// NEW: last checkpoint the player actually rested at (interacted)
	private Checkpoint _lastRestedCheckpoint;

	public bool IsDead => _isDying;

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
			dodgeIFrameDurationNormalized,
			dodgeSFX    // Pass dodge SFX to DodgeSystem
		);

		recoilSystem = new RecoilSystem(hitRecoilDistance, hitRecoilTime, playerRecoilDistance, recoilTime);

		// IMPORTANT: MeleeSystem constructor is the original one (no animationDriver parameter)
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
			gunSoundFile,
			reloadSFX,
			reloadSoundFile
		);

		meleeSystem.Initialize();

		respawnPosition = GlobalPosition;

		potionSystem = new PotionSystem(potionHealAmount, healthSystem, uiReference, startingPotions);

		// NEW: wire up potion SFX
		if (HealthStimSFX != null && HealthStimFile != null)
			HealthStimSFX.Stream = HealthStimFile;

		// Wire up dodge SFX
		if (dodgeSFX != null && dodgeSFXFile != null)
			dodgeSFX.Stream = dodgeSFXFile;

		if (uiReference != null)
			uiReference.UpdatePotionDisplay(potionSystem.CurrentPotions);

		if (_deathScreenUI != null)
			_deathScreenUI.RespawnRequested += OnDeathScreenRespawnRequested;
		else
			GD.PrintErr("[Player] _deathScreenUI is not assigned. Death animation will not play.");

		if (animationDriver == null)
			GD.PrintErr("[Player] animationDriver is not assigned. Movement/attack animations will not play.");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Always tick these
		recoilSystem.Update(dt);
		healthSystem.Update(dt);
		dodgeSystem.UpdateCooldown(dt);
		shootingSystem.UpdateCooldowns(dt);

		if (_stunTimer > 0f)
			_stunTimer = Mathf.Max(0f, _stunTimer - dt);

		if (_isDying)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// ===== STUNNED: block input/attacks/dodge/shooting, but ALLOW recoil motion =====
		if (IsStunned)
		{
			if (recoilSystem.IsInRecoil())
			{
				Velocity = recoilSystem.GetRecoilVelocity();
				MoveAndSlide();
				return;
			}

			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// External freeze
		if (!CanMove)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// Recoil overrides everything
		if (recoilSystem.IsInRecoil())
		{
			Velocity = recoilSystem.GetRecoilVelocity();
			MoveAndSlide();
			return;
		}

		// If currently attacking, just tick attack + stop movement
		if (meleeSystem.IsAttacking)
		{
			meleeSystem.UpdateAttack(dt);
			Velocity = Vector2.Zero;

			if (swingSFX != null && !swingSFX.Playing)
				swingSFX.Play();

			// Keep animation updated so Attack can auto-end, and keep facing/blend updated
			animationDriver?.UpdateFromInput(
				allowRun: false,
				isDashing: dodgeSystem.IsDodging
			);

			MoveAndSlide();
			TryUsePotionWithSfx();
			return;
		}

		// If the shoot animation is playing, tick the pending bullet and block movement
		if (animationDriver != null && animationDriver.IsShootAnimating)
		{
			float bulletSpawnDelay = animationDriver.ShootDurationSeconds * (4f / 7f);
			shootingSystem.UpdatePendingShot(dt, GlobalPosition);
			Velocity = Vector2.Zero;

			animationDriver.UpdateFromInput(
				allowRun: false,
				isDashing: false
			);

			MoveAndSlide();
			TryUsePotionWithSfx();
			return;
		}

		// Dodge motion
		if (dodgeSystem.IsDodging)
		{
			Velocity = dodgeSystem.UpdateDodge(dt);

			animationDriver?.UpdateFromInput(
				allowRun: false,
				isDashing: true
			);

			MoveAndSlide();
			TryUsePotionWithSfx();
			return;
		}

		// ===== Normal movement state =====
		Vector2 moveDirection = movementSystem.HandleInput();

		float speedMultiplier = 1f;
		if (healthSystem.IsBelowSoftThreshold())
			speedMultiplier = lowStaminaSpeedMultiplier;

		Velocity = movementSystem.GetVelocity(moveDirection, speedMultiplier);

		// Start dodge (if requested)
		if (dodgeSystem.TryDodge(moveDirection, healthSystem))
			Velocity = dodgeSystem.GetDodgeVelocity();

		// Try melee attack, and if it starts, trigger the attack animation state
		bool attackStarted = meleeSystem.TryAttack(moveDirection, recoilSystem);
		if (attackStarted)
			animationDriver?.TriggerAttack();

		// Shooting — TryShoot returns true the frame input is accepted and queues the bullet
		float spawnDelay = animationDriver != null
			? animationDriver.ShootDurationSeconds * (4f / 7f)
			: 0f;
		bool shotFired = shootingSystem.TryShoot(moveDirection, GlobalPosition, spawnDelay);
		if (shotFired)
			animationDriver?.TriggerShoot();

		// Tick any pending bullet even from the normal movement path
		// (covers the edge case where animationDriver is null)
		if (animationDriver == null)
			shootingSystem.UpdatePendingShot(dt, GlobalPosition);

		// Walk SFX
		if (moveDirection != Vector2.Zero)
		{
			if (walkSFX != null && !walkSFX.Playing)
				walkSFX.Play();
		}
		else
		{
			walkSFX?.Stop();
		}

		// ===== ANIMATION (after input/attack decisions) =====
		bool allowRunAnim =
			CanMove &&
			!IsStunned &&
			!recoilSystem.IsInRecoil() &&
			!meleeSystem.IsAttacking &&
			!dodgeSystem.IsDodging;

		animationDriver?.UpdateFromInput(
			allowRun: allowRunAnim,
			isDashing: dodgeSystem.IsDodging
		);

		MoveAndSlide();
		TryUsePotionWithSfx();
	}

	// NEW: play SFX when a potion is actually consumed
	private void TryUsePotionWithSfx()
	{
		int before = potionSystem.CurrentPotions;
		potionSystem.TryUsePotion();
		int after = potionSystem.CurrentPotions;

		// Potion was used if count went down by 1
		if (after < before)
		{
			if (HealthStimSFX != null)
			{
				if (!HealthStimSFX.Playing)
					HealthStimSFX.Play();
				else
					HealthStimSFX.Stop(); // optional: restart sound if spammed
			}
		}
	}

	// ===== STUN API =====
	public void ApplyStun(float seconds)
	{
		if (seconds <= 0f) return;
		_stunTimer = Mathf.Max(_stunTimer, seconds);
	}

	// ===== SHOP HELPERS =====
	public bool CanBuyPotion() => potionSystem != null && potionSystem.CanBuyPotion();

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

		recoilSystem.StartHitRecoil(pushDirection);
	}

	public void TriggerHitRecoil(Vector2 pushDirection, float distance, float time)
	{
		if (IsInvulnerable)
			return;

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

		if (healthSystem.CurrentHealth <= 0)
			HandleDeath();
	}

	private async void HandleDeath()
	{
		if (_isDying) return;
		_isDying = true;

		// 🔥 FORCE CLOSE PAUSE MENU
		var pauseMenu = GetTree().GetFirstNodeInGroup("pause_menu") as PauseMenu;
		pauseMenu?.ForceClose();

		_stunTimer = 0f;

		CanMove = false;
		Velocity = Vector2.Zero;

		// Play death animation immediately — driver locks in Death state until ResetDeath()
		animationDriver?.TriggerDeath();

		StopAllPlayerAudio();
		AudioManager.Instance?.SilenceBGMImmediate();

		if (_deathScreenUI != null)
		{
			// DeathScreenUI handles boss reset + emits RespawnRequested internally
			// (via Anim_RequestRespawn keyframe → ResetBossEncounters → EmitSignal)
			await _deathScreenUI.PlayAndWaitAsync("TIME CLAIMS ANOTHER");
		}
		else
		{
			// No death screen: manually reset all boss encounters before respawning
			ResetAllBossEncounters();
			await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
			RespawnAndReset();
		}

		CanMove = true;
		_isDying = false;
	}

	/// <summary>
	/// Resets all nodes in the BossEncounter group. Mirrors what DeathScreenUI does,
	/// used as a fallback when no death screen UI is assigned.
	/// </summary>
	private void ResetAllBossEncounters()
	{
		var nodes = GetTree().GetNodesInGroup(TutorialBoss.BOSS_ENCOUNTER_GROUP);
		if (nodes == null || nodes.Count == 0) return;

		foreach (var n in nodes)
		{
			if (n is TutorialBoss boss)
			{
				boss.ForceResetEncounter();
				continue;
			}

			// Duck-typed fallback for other boss scripts
			if (n is Node node && node.HasMethod("ForceResetEncounter"))
				node.Call("ForceResetEncounter");
		}
	}
	
	private void StopAllPlayerAudio()
	{
		walkSFX?.Stop();
		swingSFX?.Stop();
		gunSFX?.Stop();
		reloadSFX?.Stop();
		halberdSFX?.Stop();
	}

	private void OnDeathScreenRespawnRequested()
	{
		if (!_isDying) return;

		if (_lastRestedCheckpoint != null && IsInstanceValid(_lastRestedCheckpoint))
		{
			// Respawn at last rested checkpoint: heals, refills potions, respawns normal enemies.
			// Boss was already reset by DeathScreenUI (via Anim_RequestRespawn) before this fires.
			_lastRestedCheckpoint.RestHere(this);
			return;
		}

		// No checkpoint rested at: fall back to origin respawn.
		// Boss was already reset by DeathScreenUI before this signal fired.
		RespawnAndReset();
	}

	public void SetRespawnPoint(Vector2 pos)
	{
		respawnPosition = pos;
	}

	// NEW: called by Checkpoint when player rests
	public void SetLastRestedCheckpoint(Checkpoint cp)
	{
		if (cp == null || !IsInstanceValid(cp)) return;
		_lastRestedCheckpoint = cp;
	}

	public void RespawnAndReset()
	{
		GlobalPosition = respawnPosition;
		healthSystem.HealToFull();
		potionSystem.RefillPotions();

		_stunTimer = 0f;

		CanMove = true;

		// Allow the animation driver to resume normal state selection
		animationDriver?.ResetDeath();

		var enemies = GetTree().GetNodesInGroup("Enemy");
		foreach (Node node in enemies)
		{
			if (node is BaseEnemy enemy)
				enemy.ResetEnemy();
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
		}
	}
}
