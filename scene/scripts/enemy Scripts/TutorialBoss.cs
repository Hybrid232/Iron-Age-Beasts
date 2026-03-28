using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialBoss : BaseEnemy
{
	private enum BossState { Idle, Chasing, Telegraph, Active, Recover, Roaring, Dead }
	private enum BossAttack { None, Bite, TailSweep, Charge }

	[ExportGroup("Arena Activation")]
	[Export] public Area2D ArenaTriggerArea;
	[Export] public StaticBody2D EntranceGate;
	[Export] public bool LockEntranceOnStart = true;

	[ExportGroup("Boss UI (Robust Lookup)")]
	[Export] public bool UseBossUIGroupLookup = true;
	[Export] public NodePath BossUIPath;

	private CanvasItem _bossUIItem;
	private IBossUI bossUI;

	// -----------------------------
	// Encounter Reset
	// -----------------------------
	[ExportGroup("Encounter Reset")]
	[Export] public bool ResetEncounterWhenPlayerDies = true;

	public const string BOSS_ENCOUNTER_GROUP = "BossEncounter";
	private bool _playerDeathHooked = false;

	// -----------------------------
	// Animation
	// -----------------------------
	[ExportGroup("Animation")]
	[Export] public AnimatedSprite2D Sprite;
	[Export] public string IdleAnimName = "idle";
	[Export] public string ChaseAnimName = "chase";

	[Export] public string BiteAnimName = "bite";
	[Export] public int BiteDamageFrameIndex = 2;

	[Export] public string TailSweepAnimName = "Spin";
	[Export] public int TailSweepActiveStartFrame = 8;
	[Export] public int TailSweepActiveEndFrame = 11;

	[Export] public string ChargeAnimName = "charge";
	[Export] public int ChargeActiveStartFrame = 7;
	[Export] public int ChargeActiveEndFrame = 15;
	[Export] public float ChargeSpeedMultiplier = 2.2f;

	[Export] public string Phase2RoarAnimName = "Roar";
	[Export] public int Phase2RoarActiveStartFrame = 3; // inclusive
	[Export] public int Phase2RoarActiveEndFrame = 9;   // inclusive

	[Export] public bool DefaultFacesRight = true;
	[Export] public float FaceDeadzonePx = 2f;

	private string _currentAnim = "";
	private bool _facingRight = true;

	// -----------------------------
	// Collision + Hurtbox flipping
	// -----------------------------
	[ExportGroup("Collision / Hurtbox Facing (Flip X)")]
	[Export] public Node2D BodyCollisionRoot;
	[Export] public Node2D HurtboxRoot;
	[Export] public bool CollisionDefaultFacesRight = true;

	[ExportGroup("Phase 2 (<=40% HP)")]
	[Export] public float Phase2SpeedMultiplier = 1.35f;
	[Export] public bool UnlockChargeAt40Percent = true;

	[ExportGroup("Phase 2 Roar (on enter)")]
	[Export] public float Phase2RoarTelegraph = 0.6f; // kept (animation drives timing)
	[Export] public float Phase2RoarRecover = 0.7f;
	[Export] public float RoarKnockbackDistance = 420f;
	[Export] public float RoarKnockbackTime = 0.25f;
	[Export] public float RoarStunSeconds = 2.5f;

	[ExportGroup("Phase 2 Adds (Spawners)")]
	[Export] public NodePath Phase2SpawnerRootPath;
	[Export] public int Phase2SpawnersToActivate = 3;

	private const string ENEMY_GROUP = "Enemy";

	[ExportGroup("Ranges")]
	[Export] public float BiteRange = 40f;
	[Export] public float TailSweepRange = 85f;
	[Export] public float MinChargeRange = 120f;

	[ExportGroup("Damages (vs Player)")]
	[Export] public int BiteDamage = 20;
	[Export] public int TailDamage = 15;
	[Export] public int ChargeDamage = 30;

	[ExportGroup("Damage Tuning (vs Little Dinos)")]
	[Export] public float DamageToEnemiesMultiplier = 0.35f;
	[Export] public int DamageToEnemiesFlatOverride = 0;

	[ExportGroup("Cooldowns")]
	[Export] public float BiteCooldown = 1.2f;
	[Export] public float TailCooldown = 1.8f;
	[Export] public float ChargeCooldown = 3.0f;

	[ExportGroup("Timings: Telegraph / Active / Recover")]
	[Export] public float BiteTelegraph = 0.20f;
	[Export] public float BiteActive = 0.12f;
	[Export] public float BiteRecover = 0.55f;

	[Export] public float TailTelegraph = 0.30f;
	[Export] public float TailActive = 0.18f;
	[Export] public float TailRecover = 0.70f;

	[Export] public float ChargeTelegraph = 0.40f;
	[Export] public float ChargeActive = 0.75f;
	[Export] public float ChargeRecover = 0.85f;

	[ExportGroup("Hitboxes (Area2D)")]
	[Export] public Area2D BiteHitbox;
	[Export] public Area2D TailHitbox;
	[Export] public Area2D ChargeHitbox;

	[ExportGroup("Telegraphs (Visuals)")]
	[Export] public NodePath BiteTelegraphPath;
	[Export] public NodePath TailTelegraphPath;
	[Export] public NodePath ChargeTelegraphPath;
	
	[ExportGroup("Boss Fight Audio")]
	[Export] public AudioStreamPlayer bossMusic;
	[Export] public AudioStream bossMusicFile;

	private CanvasItem biteTelegraph;
	private CanvasItem tailTelegraph;
	private CanvasItem chargeTelegraph;

	[ExportGroup("Knockback Attacks (vs Player)")]
	[Export] public float BiteKnockbackDistance = 100f;
	[Export] public float BiteKnockbackTime = 0.20f;
	[Export] public float TailKnockbackDistance = 180f;
	[Export] public float TailKnockbackTime = 0.18f;
	[Export] public float ChargeKnockbackDistance = 250f;
	[Export] public float ChargeKnockbackTime = 2.0f;

	// -----------------------------
	// Knockback Little Enemies (Boss attacks)
	// -----------------------------
	[ExportGroup("Boss Attacks: Knockback Little Enemies")]
	[Export] public bool BossAttacksKnockbackEnemies = true;

	[Export] public float TailEnemyKnockbackDistance = 220f;
	[Export] public float TailEnemyKnockbackTime = 0.22f;
	[Export] public float TailEnemyStunSeconds = 0.25f;

	[Export] public float ChargeEnemyKnockbackDistance = 260f;
	[Export] public float ChargeEnemyKnockbackTime = 0.25f;
	[Export] public float ChargeEnemyStunSeconds = 0.35f;

	[ExportGroup("Facing / Orientation")]
	[Export] public bool FlipAttackDirection = true;

	[ExportGroup("Contact Damage (Body Touch)")]
	[Export] public Area2D BodyContactDamageArea;
	[Export] public int ContactDamage = 5;
	[Export] public float ContactKnockbackDistance = 140f;
	[Export] public float ContactKnockbackTime = 0.18f;
	[Export] public float ContactDamageCooldown = 0.40f;

	private readonly Dictionary<ulong, float> _contactDamageCdByTarget = new();

	[ExportGroup("Phase 2: Knockback Little Enemies")]
	[Export] public bool Phase2RoarAffectsEnemies = true;
	[Export] public float EnemyRoarKnockbackDistance = 320f;
	[Export] public float EnemyRoarKnockbackTime = 0.25f;
	[Export] public float EnemyRoarStunSeconds = 1.0f;

	[ExportGroup("Phase 2: Prevent Boss Getting Stuck On Adds")]
	[Export] public bool IgnoreEnemyBodyCollisionsInPhase2 = true;

	[Export(PropertyHint.Range, "1,32,1")] public int EnemyPhysicsLayerNumber = 3;
	private uint _cachedPreFightCollisionMask = 0;
	private bool _cachedMask = false;

	private BossState state = BossState.Idle;
	private BossAttack currentAttack = BossAttack.None;

	private float stateTimer;
	private float biteCd, tailCd, chargeCd;

	private bool phase2;
	private bool _phase2RoarDone = false;
	private float baseSpeed;

	private Vector2 chargeDir = Vector2.Zero;
	private readonly HashSet<ulong> hitTargetsThisActive = new();

	private bool fightStarted = false;

	private bool _biteHitboxEnabledThisBite = false;
	private bool _tailHitboxEnabledThisSpin = false;
	private bool _chargeActiveWindowOpen = false;

	private bool _roarPulseFiredThisRoar = false;

	public override void _EnterTree()
	{
		base._EnterTree();
		AddToGroup(BOSS_ENCOUNTER_GROUP);
	}

	public override void _Ready()
	{
		base._Ready();

		PlayerGroup = BaseEnemy.PLAYER_GROUP;
		baseSpeed = Speed;

		AutoFillRangesFromHitboxes();

		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		if (BodyContactDamageArea != null)
		{
			SetHitboxEnabled(BodyContactDamageArea, false);
			BodyContactDamageArea.BodyEntered += OnBodyContactEntered;
			BodyContactDamageArea.BodyExited += OnBodyContactExited;
		}

		biteTelegraph = ResolveTelegraph(BiteTelegraphPath);
		tailTelegraph = ResolveTelegraph(TailTelegraphPath);
		chargeTelegraph = ResolveTelegraph(ChargeTelegraphPath);
		HideAllTelegraphs();

		ResolveBossUI();

		fightStarted = false;
		state = BossState.Idle;
		_player = null;
		_chasing = false;
		Velocity = Vector2.Zero;

		SetEntranceGateLocked(false);

		if (ArenaTriggerArea != null)
		{
			ArenaTriggerArea.Monitorable = true;
			ArenaTriggerArea.Monitoring = false;
			ArenaTriggerArea.BodyEntered += OnArenaTriggerBodyEntered;
			CallDeferred(nameof(ArmArenaTriggerNextFrame));
		}
		else
		{
			GD.PushWarning("[Boss] ArenaTriggerArea is not assigned.");
		}

		if (BodyContactDamageArea != null)
		{
			BodyContactDamageArea.CollisionLayer = 2;
			BodyContactDamageArea.CollisionMask = 1;
		}

		if (Sprite == null)
			Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (Sprite != null)
		{
			Sprite.FrameChanged += OnSpriteFrameChanged;
			Sprite.AnimationFinished += OnSpriteAnimationFinished;
		}

		if (BodyCollisionRoot == null)
			BodyCollisionRoot = GetNodeOrNull<Node2D>("BodyCollision");

		if (HurtboxRoot == null)
			HurtboxRoot = GetNodeOrNull<Node2D>("HurtboxArea");

		UpdateFacingAndAnimation(force: true);
	}

	// Player disappearance reset
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (fightStarted && ResetEncounterWhenPlayerDies)
		{
			if (_player == null || !IsInstanceValid(_player))
			{
				GD.Print("[Boss] Player became invalid during fight; resetting encounter.");
				ForceResetEncounter();
				return;
			}
		}

		if (!fightStarted) return;

		float dt = (float)delta;
		biteCd = Math.Max(0, biteCd - dt);
		tailCd = Math.Max(0, tailCd - dt);
		chargeCd = Math.Max(0, chargeCd - dt);

		if (_contactDamageCdByTarget.Count > 0)
		{
			var keys = new List<ulong>(_contactDamageCdByTarget.Keys);
			foreach (var k in keys)
				_contactDamageCdByTarget[k] = Math.Max(0f, _contactDamageCdByTarget[k] - dt);
		}

		HandlePhase2();
		UpdateFacingAndAnimation(force: false);
	}

	private void HookPlayerDeathEvents(Player p)
	{
		if (p == null || !IsInstanceValid(p)) return;
		if (_playerDeathHooked) return;

		p.TreeExiting += OnPlayerTreeExiting;
		_playerDeathHooked = true;
	}

	private void UnhookPlayerDeathEvents(Player p)
	{
		if (p == null || !IsInstanceValid(p)) { _playerDeathHooked = false; return; }
		if (!_playerDeathHooked) return;

		p.TreeExiting -= OnPlayerTreeExiting;
		_playerDeathHooked = false;
	}

	private void OnPlayerTreeExiting()
	{
		if (!fightStarted) return;
		if (!ResetEncounterWhenPlayerDies) return;

		GD.Print("[Boss] Player TreeExiting during fight; resetting encounter.");
		ForceResetEncounter();
	}

	public void ForceResetEncounter()
	{
		if (_bossUIItem != null) _bossUIItem.Visible = false;
		UnhookPlayerDeathEvents(_player);
		ResetEnemy();
	}

	public void HideBossUI()
	{
		if (_bossUIItem != null) _bossUIItem.Visible = false;
	}

	private void UpdateFacingAndAnimation(bool force)
	{
		if (_player != null && IsInstanceValid(_player))
		{
			float dx = _player.GlobalPosition.X - GlobalPosition.X;
			if (Mathf.Abs(dx) > FaceDeadzonePx)
				_facingRight = dx > 0f;
		}

		if (Sprite != null)
		{
			bool flipSprite = DefaultFacesRight ? !_facingRight : _facingRight;
			Sprite.FlipH = flipSprite;
		}

		ApplyLeftRightScaleFlip(BodyCollisionRoot, _facingRight, CollisionDefaultFacesRight);
		ApplyLeftRightScaleFlip(HurtboxRoot, _facingRight, CollisionDefaultFacesRight);

		string desiredAnim =
			(state == BossState.Roaring) ? Phase2RoarAnimName
			: (state == BossState.Active) ? currentAttack switch
			{
				BossAttack.Bite => BiteAnimName,
				BossAttack.TailSweep => TailSweepAnimName,
				BossAttack.Charge => ChargeAnimName,
				_ => IdleAnimName
			}
			: state switch
			{
				BossState.Chasing => ChaseAnimName,
				BossState.Telegraph => IdleAnimName,
				BossState.Recover => IdleAnimName,
				_ => IdleAnimName
			};

		if (Sprite == null) return;

		if (force || _currentAnim != desiredAnim)
		{
			_currentAnim = desiredAnim;

			if (Sprite.SpriteFrames != null && Sprite.SpriteFrames.HasAnimation(desiredAnim))
				Sprite.Play(desiredAnim);
			else
				GD.PushWarning($"[Boss] AnimatedSprite2D missing animation '{desiredAnim}'.");
		}
	}

	private static void ApplyLeftRightScaleFlip(Node2D root, bool facingRight, bool defaultFacesRight)
	{
		if (root == null) return;

		float absX = Mathf.Abs(root.Scale.X);
		if (absX < 0.0001f) absX = 1f;

		bool facingMatchesDefault = (facingRight == defaultFacesRight);
		float sx = facingMatchesDefault ? absX : -absX;

		root.Scale = new Vector2(sx, root.Scale.Y);
	}

	private void ResolveBossUI()
	{
		_bossUIItem = null;
		bossUI = null;

		if (UseBossUIGroupLookup)
		{
			var uiNode = GetTree().GetFirstNodeInGroup(BossUI.GROUP_NAME);

			_bossUIItem = uiNode as CanvasItem;
			bossUI = uiNode as IBossUI;

			if (_bossUIItem == null)
				GD.PushWarning("[Boss] BossUI not found via group lookup OR is not a CanvasItem (group='BossUI').");
			if (bossUI == null)
				GD.PushWarning("[Boss] BossUI found via group lookup but does not implement IBossUI.");
		}
		else
		{
			_bossUIItem = (BossUIPath != null && !BossUIPath.IsEmpty)
				? GetNodeOrNull(BossUIPath) as CanvasItem
				: null;

			bossUI = (BossUIPath != null && !BossUIPath.IsEmpty)
				? GetNodeOrNull(BossUIPath) as IBossUI
				: null;

			if (_bossUIItem == null)
				GD.PushWarning("[Boss] BossUIPath did not resolve to a CanvasItem.");
			if (bossUI == null)
				GD.PushWarning("[Boss] BossUIPath did not resolve to an IBossUI.");
		}

		bossUI?.InitializeBoss(MaxHealth, _currentHealth);
		if (_bossUIItem != null) _bossUIItem.Visible = false;
	}

	private async void ArmArenaTriggerNextFrame()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		if (fightStarted) return;
		if (ArenaTriggerArea == null) return;

		ArenaTriggerArea.Monitoring = true;
		GD.Print("[Boss] Arena trigger armed.");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!fightStarted || state == BossState.Idle)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float dt = (float)delta;

		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Player;
			_chasing = _player != null;
		}

		if (state == BossState.Dead)
			return;

		UpdateState(dt);

		if (state == BossState.Active)
			ApplyActiveHitboxDamage(currentAttack);

		MoveAndSlide();
	}

	private void OnArenaTriggerBodyEntered(Node body)
	{
		if (fightStarted) return;
		if (body is not Player player) return;
		if (!player.IsInGroup(PlayerGroup)) return;

		GD.Print($"[Boss] Arena triggered by Player '{player.Name}'. Starting fight.");
		StartBossFight(player);
	}

	private void StartBossFight(Player player)
	{
		fightStarted = true;
		if (fightStarted == true && bossMusic != null && !bossMusic.Playing)
		{
			bossMusic.Stream = bossMusicFile;
			GD.Print("Boss Music started!");
			bossMusic.Play();
		}
		// Cache collision mask for restoration on reset
		if (!_cachedMask)
		{
			_cachedPreFightCollisionMask = CollisionMask;
			_cachedMask = true;
		}

		_player = player;
		_chasing = true;

		HookPlayerDeathEvents(_player);

		state = BossState.Chasing;
		currentAttack = BossAttack.None;

		bossUI?.InitializeBoss(MaxHealth, _currentHealth);
		if (_bossUIItem != null) _bossUIItem.Visible = true;

		if (BodyContactDamageArea != null)
			SetHitboxEnabled(BodyContactDamageArea, true);

		if (LockEntranceOnStart)
			SetEntranceGateLocked(true);

		if (ArenaTriggerArea != null)
			ArenaTriggerArea.Monitoring = false;

		DisableAndDespawnPhase2Spawners();

		UpdateFacingAndAnimation(force: true);
	}

	private void SetEntranceGateLocked(bool locked)
	{
		if (EntranceGate == null)
		{
			GD.PushWarning("[Boss] EntranceGate is not assigned.");
			return;
		}

		foreach (var node in EntranceGate.GetChildren())
		{
			if (node is CollisionShape2D cs) cs.Disabled = !locked;
			else if (node is CollisionPolygon2D cp) cp.Disabled = !locked;
		}
	}

	private void HandlePhase2()
	{
		if (phase2) return;

		float hpPct = (float)_currentHealth / MaxHealth;
		if (hpPct <= 0.40f)
		{
			phase2 = true;
			Speed = baseSpeed * Phase2SpeedMultiplier;

			BiteCooldown *= 0.85f;
			TailCooldown *= 0.85f;

			if (IgnoreEnemyBodyCollisionsInPhase2)
				RemoveEnemyFromBossCollisionMask();

			if (!_phase2RoarDone && state != BossState.Dead)
				StartPhase2Roar();
		}
	}

	private void RemoveEnemyFromBossCollisionMask()
	{
		int bitIndex = Mathf.Clamp(EnemyPhysicsLayerNumber, 1, 32) - 1;
		uint enemyBit = 1u << bitIndex;
		CollisionMask &= ~enemyBit;
	}

	private void StartPhase2Roar()
	{
		_phase2RoarDone = true;

		currentAttack = BossAttack.None;

		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		HideAllTelegraphs();

		Velocity = Vector2.Zero;

		state = BossState.Roaring;
		_roarPulseFiredThisRoar = false;

		GD.Print("[Boss] PHASE 2 ROAR starting...");
	}

	private void UpdateState(float dt)
	{
		if (_player == null)
		{
			Velocity = Vector2.Zero;
			return;
		}

		switch (state)
		{
			case BossState.Roaring:
				Velocity = Vector2.Zero;
				break;

			case BossState.Chasing:
				Chase();
				TryPickAttack();
				break;

			case BossState.Telegraph:
			case BossState.Active:
			case BossState.Recover:
				if (!(state == BossState.Active && currentAttack == BossAttack.Charge && _chargeActiveWindowOpen))
					Velocity = Vector2.Zero;

				if (!(state == BossState.Active && currentAttack == BossAttack.Bite) &&
					!(state == BossState.Active && currentAttack == BossAttack.TailSweep) &&
					!(state == BossState.Active && currentAttack == BossAttack.Charge))
				{
					stateTimer -= dt;
					if (stateTimer <= 0f)
						AdvanceAttackPhase();
				}
				break;
		}
	}

	private void DoRoarPulseAndStartPhase2Adds()
	{
		GD.Print("[Boss] ROAR pulse (global knockback + stun + phase2 adds).");

		if (_player != null && IsInstanceValid(_player))
		{
			Vector2 pushDir = (_player.GlobalPosition - GlobalPosition);
			if (pushDir == Vector2.Zero) pushDir = Vector2.Right;
			pushDir = pushDir.Normalized();

			_player.TriggerHitRecoil(pushDir, RoarKnockbackDistance, RoarKnockbackTime);

			if (_player is IStunnable stunnable)
				stunnable.ApplyStun(RoarStunSeconds);
		}

		if (phase2 && Phase2RoarAffectsEnemies)
			KnockbackNearbyEnemiesFromRoar();

		ActivatePhase2SpawnersAndForceHunt(Phase2SpawnersToActivate);

		state = BossState.Recover;
		stateTimer = Phase2RoarRecover;
	}

	private void KnockbackNearbyEnemiesFromRoar()
	{
		float radius = Math.Max(120f, TailSweepRange * 2f);
		float radiusSq = radius * radius;

		var enemies = GetTree().GetNodesInGroup(ENEMY_GROUP);
		foreach (var n in enemies)
		{
			if (n is not Node2D e) continue;
			if (e == this || IsAncestorOf(e)) continue;
			if (!IsInstanceValid(e)) continue;
			if (e is TutorialBoss) continue;

			Vector2 delta = e.GlobalPosition - GlobalPosition;
			if (delta.LengthSquared() > radiusSq) continue;

			Vector2 dir = delta == Vector2.Zero ? Vector2.Right : delta.Normalized();

			if (e is BaseEnemy be)
				be.ApplyKnockback(dir, EnemyRoarKnockbackDistance, EnemyRoarKnockbackTime);
			else if (e.HasMethod("ApplyKnockback"))
				e.Call("ApplyKnockback", dir, EnemyRoarKnockbackDistance, EnemyRoarKnockbackTime);
			else if (e.HasMethod("TriggerHitRecoil"))
				e.Call("TriggerHitRecoil", dir, EnemyRoarKnockbackDistance, EnemyRoarKnockbackTime);
			else if (e is CharacterBody2D cb)
				cb.Velocity = dir * (EnemyRoarKnockbackDistance / Math.Max(0.001f, EnemyRoarKnockbackTime));

			if (e is IStunnable stunnable)
				stunnable.ApplyStun(EnemyRoarStunSeconds);
		}
	}

	private void ActivatePhase2SpawnersAndForceHunt(int count)
	{
		var spawners = GetPhase2Spawners();
		if (spawners.Count == 0) return;

		int activated = 0;
		foreach (var sp in spawners)
		{
			if (activated >= count) break;
			if (sp == null || !IsInstanceValid(sp)) continue;
			if (sp.IsBossSpawner) continue;

			sp.EnableAndSpawn(_player);
			activated++;
		}
	}

	private void DisableAndDespawnPhase2Spawners()
	{
		var spawners = GetPhase2Spawners();
		foreach (var sp in spawners)
			sp.DisableAndDespawn();
	}

	private List<EnemySpawner> GetPhase2Spawners()
	{
		var results = new List<EnemySpawner>();
		if (Phase2SpawnerRootPath == null || Phase2SpawnerRootPath.IsEmpty) return results;

		var root = GetNodeOrNull(Phase2SpawnerRootPath);
		if (root == null) return results;

		foreach (var child in root.GetChildren())
			if (child is EnemySpawner sp) results.Add(sp);

		results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return results;
	}

	private void Chase()
	{
		if (_player == null) return;

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float dist = toPlayer.Length();
		if (dist < 1f) { Velocity = Vector2.Zero; return; }

		Vector2 dir = toPlayer / dist;
		Velocity = dir * Speed;
	}

	private void TryPickAttack()
	{
		if (_player == null) return;

		float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
		bool chargeUnlocked = phase2 && UnlockChargeAt40Percent;

		if (dist <= BiteRange && biteCd <= 0f) { StartAttack(BossAttack.Bite); biteCd = BiteCooldown; return; }
		if (dist <= TailSweepRange && tailCd <= 0f) { StartAttack(BossAttack.TailSweep); tailCd = TailCooldown; return; }
		if (chargeUnlocked && dist >= MinChargeRange && chargeCd <= 0f) { StartAttack(BossAttack.Charge); chargeCd = ChargeCooldown; return; }
	}

	private void StartAttack(BossAttack attack)
	{
		currentAttack = attack;
		state = BossState.Telegraph;

		_biteHitboxEnabledThisBite = false;
		_tailHitboxEnabledThisSpin = false;
		_chargeActiveWindowOpen = false;

		if (_player != null && attack == BossAttack.Charge)
			chargeDir = (_player.GlobalPosition - GlobalPosition).Normalized();

		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		ShowTelegraph(attack, true);
		stateTimer = GetTelegraphTime(attack);
	}

	private void AdvanceAttackPhase()
	{
		if (state == BossState.Telegraph)
		{
			state = BossState.Active;
			stateTimer = GetActiveTime(currentAttack);

			hitTargetsThisActive.Clear();

			PrepareHitboxForAttack(currentAttack);
			ShowTelegraph(currentAttack, false);

			// Bite/TailSweep/Charge are animation-driven for hitbox/movement windows
			EnableAttackHitbox(currentAttack, false);

			return;
		}

		if (state == BossState.Active)
		{
			// All 3 end via AnimationFinished
			return;
		}

		if (state == BossState.Recover)
		{
			currentAttack = BossAttack.None;
			state = BossState.Chasing;
			return;
		}
	}

	private float GetTelegraphTime(BossAttack a) => a switch
	{
		BossAttack.Bite => BiteTelegraph,
		BossAttack.TailSweep => TailTelegraph,
		BossAttack.Charge => ChargeTelegraph,
		_ => 0.1f
	};

	private float GetActiveTime(BossAttack a) => a switch
	{
		BossAttack.Bite => BiteActive,
		BossAttack.TailSweep => TailActive,
		BossAttack.Charge => ChargeActive,
		_ => 0.1f
	};

	private float GetRecoverTime(BossAttack a) => a switch
	{
		BossAttack.Bite => BiteRecover,
		BossAttack.TailSweep => TailRecover,
		BossAttack.Charge => ChargeRecover,
		_ => 0.2f
	};

	private void EnableAttackHitbox(BossAttack a, bool enabled)
	{
		switch (a)
		{
			case BossAttack.Bite: SetHitboxEnabled(BiteHitbox, enabled); break;
			case BossAttack.TailSweep: SetHitboxEnabled(TailHitbox, enabled); break;
			case BossAttack.Charge: SetHitboxEnabled(ChargeHitbox, enabled); break;
		}
	}

	private void PrepareHitboxForAttack(BossAttack a)
	{
		if (_player == null) return;

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() <= 0.001f) return;

		float angle = toPlayer.Angle();
		if (FlipAttackDirection) angle += Mathf.Pi;

		switch (a)
		{
			case BossAttack.TailSweep:
				if (TailHitbox != null) TailHitbox.Rotation = 0f;
				break;
			case BossAttack.Charge:
				if (ChargeHitbox != null) ChargeHitbox.GlobalRotation = angle;
				break;
			case BossAttack.Bite:
				if (BiteHitbox != null) BiteHitbox.GlobalRotation = angle;
				break;
		}

		RotateTelegraphToAngle(a, angle);
	}

	private void ApplyActiveHitboxDamage(BossAttack a)
	{
		Area2D hitbox = a switch
		{
			BossAttack.Bite => BiteHitbox,
			BossAttack.TailSweep => TailHitbox,
			BossAttack.Charge => ChargeHitbox,
			_ => null
		};

		int baseDmg = a switch
		{
			BossAttack.Bite => BiteDamage,
			BossAttack.TailSweep => TailDamage,
			BossAttack.Charge => ChargeDamage,
			_ => 0
		};

		if (hitbox == null || baseDmg <= 0) return;
		if (!hitbox.Monitoring) return;

		var bodies = hitbox.GetOverlappingBodies();
		foreach (var obj in bodies)
		{
			if (obj is not Node2D body) continue;
			if (body == this || IsAncestorOf(body)) continue;

			bool isPlayer = body.IsInGroup(PlayerGroup);
			bool isEnemy = body.IsInGroup(ENEMY_GROUP);

			if (!isPlayer && !isEnemy) continue;
			if (isEnemy && body is TutorialBoss) continue;
			if (body is not IDamageable damageable) continue;

			ulong id = body.GetInstanceId();
			if (hitTargetsThisActive.Contains(id)) continue;

			hitTargetsThisActive.Add(id);

			int dmgToApply = baseDmg;
			if (isEnemy)
			{
				if (DamageToEnemiesFlatOverride > 0)
					dmgToApply = DamageToEnemiesFlatOverride;
				else
					dmgToApply = Mathf.Max(1, Mathf.RoundToInt(baseDmg * DamageToEnemiesMultiplier));
			}

			damageable.TakeDamage(dmgToApply);

			// Player knockback
			if (isPlayer && body is Player p)
			{
				(float kbDist, float kbTime) = a switch
				{
					BossAttack.Bite => (BiteKnockbackDistance, BiteKnockbackTime),
					BossAttack.TailSweep => (TailKnockbackDistance, TailKnockbackTime),
					BossAttack.Charge => (ChargeKnockbackDistance, ChargeKnockbackTime),
					_ => (0f, 0f)
				};

				if (kbDist > 0f && kbTime > 0f)
				{
					Vector2 pushDir = (p.GlobalPosition - GlobalPosition).Normalized();
					p.TriggerHitRecoil(pushDir, kbDist, kbTime);
				}
			}

			// Enemy knockback on TailSweep + Charge
			if (isEnemy && BossAttacksKnockbackEnemies && (a == BossAttack.TailSweep || a == BossAttack.Charge))
			{
				(float kbDist, float kbTime, float stun) = a switch
				{
					BossAttack.TailSweep => (TailEnemyKnockbackDistance, TailEnemyKnockbackTime, TailEnemyStunSeconds),
					BossAttack.Charge => (ChargeEnemyKnockbackDistance, ChargeEnemyKnockbackTime, ChargeEnemyStunSeconds),
					_ => (0f, 0f, 0f)
				};

				if (kbDist > 0f && kbTime > 0f)
				{
					Vector2 pushDir = (body.GlobalPosition - GlobalPosition);
					if (pushDir == Vector2.Zero) pushDir = Vector2.Right;
					pushDir = pushDir.Normalized();

					// Your little enemies inherit BaseEnemy, so this is the correct knockback API.
					if (body is BaseEnemy be)
						be.ApplyKnockback(pushDir, kbDist, kbTime);
					else if (body.HasMethod("ApplyKnockback"))
						body.Call("ApplyKnockback", pushDir, kbDist, kbTime);
					else if (body is CharacterBody2D cb)
						cb.Velocity = pushDir * (kbDist / Math.Max(0.001f, kbTime));
				}

				if (stun > 0f && body is IStunnable stunnable)
					stunnable.ApplyStun(stun);
			}
		}
	}

	private void OnBodyContactEntered(Node2D body) => TryApplyContactDamage(body);
	private void OnBodyContactExited(Node2D body) { }

	private void TryApplyContactDamage(Node2D body)
	{
		if (!fightStarted) return;
		if (!body.IsInGroup(PlayerGroup)) return;
		if (body is not IDamageable dmgable) return;

		ulong id = body.GetInstanceId();
		if (_contactDamageCdByTarget.TryGetValue(id, out float cd) && cd > 0f) return;

		_contactDamageCdByTarget[id] = ContactDamageCooldown;
		dmgable.TakeDamage(ContactDamage);

		if (body is Player p)
		{
			Vector2 pushDir = (p.GlobalPosition - GlobalPosition).Normalized();
			p.TriggerHitRecoil(pushDir, ContactKnockbackDistance, ContactKnockbackTime);
		}
	}

	private void AutoFillRangesFromHitboxes()
	{
		BiteRange = Math.Max(BiteRange, GetHitboxReachPixels(BiteHitbox));
		TailSweepRange = Math.Max(TailSweepRange, GetHitboxReachPixels(TailHitbox));
		MinChargeRange = Math.Max(MinChargeRange, GetHitboxReachPixels(ChargeHitbox));
	}

	private float GetHitboxReachPixels(Area2D hitbox)
	{
		if (hitbox == null) return 0f;
		float best = 0f;

		foreach (var node in hitbox.FindChildren("*", "CollisionShape2D", true, false))
		{
			if (node is not CollisionShape2D cs) continue;
			if (cs.Shape == null) continue;
			best = Math.Max(best, ShapeReach(cs.Shape));
		}

		foreach (var node in hitbox.FindChildren("*", "CollisionPolygon2D", true, false))
		{
			if (node is not CollisionPolygon2D cp) continue;
			float polyReach = 0f;
			foreach (var p in cp.Polygon)
				polyReach = Math.Max(polyReach, p.Length());
			best = Math.Max(best, polyReach);
		}

		return best;
	}

	private float ShapeReach(Shape2D shape) => shape switch
	{
		CircleShape2D c => c.Radius,
		RectangleShape2D r => r.Size.Length() * 0.5f,
		CapsuleShape2D cap => cap.Radius + (cap.Height * 0.5f),
		_ => 0f
	};

	private void SetHitboxEnabled(Area2D hitbox, bool enabled)
	{
		if (hitbox == null) return;

		hitbox.Monitoring = enabled;
		hitbox.Monitorable = true;

		foreach (var node in hitbox.FindChildren("*", "CollisionShape2D", true, false))
			((CollisionShape2D)node).Disabled = !enabled;

		foreach (var node in hitbox.FindChildren("*", "CollisionPolygon2D", true, false))
			((CollisionPolygon2D)node).Disabled = !enabled;
	}

	private CanvasItem ResolveTelegraph(NodePath path)
	{
		if (path == null || path.IsEmpty) return null;
		return GetNodeOrNull(path) as CanvasItem;
	}

	private void HideAllTelegraphs()
	{
		if (biteTelegraph != null) biteTelegraph.Visible = false;
		if (tailTelegraph != null) tailTelegraph.Visible = false;
		if (chargeTelegraph != null) chargeTelegraph.Visible = false;
	}

	private void ShowTelegraph(BossAttack a, bool show)
	{
		HideAllTelegraphs();
		if (!show) return;

		switch (a)
		{
			case BossAttack.Bite: if (biteTelegraph != null) biteTelegraph.Visible = true; break;
			case BossAttack.TailSweep: if (tailTelegraph != null) tailTelegraph.Visible = true; break;
			case BossAttack.Charge: if (chargeTelegraph != null) chargeTelegraph.Visible = true; break;
		}
	}

	private void RotateTelegraphToAngle(BossAttack a, float angle)
	{
		CanvasItem t = a switch
		{
			BossAttack.Bite => biteTelegraph,
			BossAttack.TailSweep => tailTelegraph,
			BossAttack.Charge => chargeTelegraph,
			_ => null
		};

		if (t is Node2D n2d)
			n2d.GlobalRotation = angle;
	}

	private void OnSpriteFrameChanged()
	{
		// Roar window (Roaring state)
		if (state == BossState.Roaring && Sprite != null && Sprite.IsPlaying() && Sprite.Animation == Phase2RoarAnimName)
		{
			bool inActive = Sprite.Frame >= Phase2RoarActiveStartFrame && Sprite.Frame <= Phase2RoarActiveEndFrame;

			if (inActive && !_roarPulseFiredThisRoar)
			{
				_roarPulseFiredThisRoar = true;
				DoRoarPulseAndStartPhase2Adds();
			}
			return;
		}

		// Attacks (Active state)
		if (state != BossState.Active) return;
		if (Sprite == null) return;
		if (!Sprite.IsPlaying()) return;

		// Bite
		if (currentAttack == BossAttack.Bite && Sprite.Animation == BiteAnimName)
		{
			if (!_biteHitboxEnabledThisBite && Sprite.Frame == BiteDamageFrameIndex)
			{
				_biteHitboxEnabledThisBite = true;
				EnableAttackHitbox(BossAttack.Bite, true);
			}
			return;
		}

		// Tail sweep
		if (currentAttack == BossAttack.TailSweep && Sprite.Animation == TailSweepAnimName)
		{
			bool inActive = Sprite.Frame >= TailSweepActiveStartFrame && Sprite.Frame <= TailSweepActiveEndFrame;

			if (inActive && !_tailHitboxEnabledThisSpin)
			{
				_tailHitboxEnabledThisSpin = true;
				EnableAttackHitbox(BossAttack.TailSweep, true);
			}
			else if (!inActive && _tailHitboxEnabledThisSpin)
			{
				_tailHitboxEnabledThisSpin = false;
				EnableAttackHitbox(BossAttack.TailSweep, false);
			}
			return;
		}

		// Charge
		if (currentAttack == BossAttack.Charge && Sprite.Animation == ChargeAnimName)
		{
			bool inActive = Sprite.Frame >= ChargeActiveStartFrame && Sprite.Frame <= ChargeActiveEndFrame;

			if (inActive && !_chargeActiveWindowOpen)
			{
				_chargeActiveWindowOpen = true;
				EnableAttackHitbox(BossAttack.Charge, true);
				Velocity = chargeDir * (Speed * ChargeSpeedMultiplier);
			}
			else if (!inActive && _chargeActiveWindowOpen)
			{
				_chargeActiveWindowOpen = false;
				EnableAttackHitbox(BossAttack.Charge, false);
				Velocity = Vector2.Zero;
			}
		}
	}

	private void OnSpriteAnimationFinished()
	{
		// If Roar finishes and we haven't transitioned yet, continue fight.
		if (state == BossState.Roaring && Sprite != null && Sprite.Animation == Phase2RoarAnimName)
		{
			if (!_roarPulseFiredThisRoar)
				DoRoarPulseAndStartPhase2Adds();
			return;
		}

		// Bite ends
		if (currentAttack == BossAttack.Bite && state == BossState.Active && Sprite != null && Sprite.Animation == BiteAnimName)
		{
			EnableAttackHitbox(BossAttack.Bite, false);
			ShowTelegraph(BossAttack.Bite, false);

			Velocity = Vector2.Zero;
			state = BossState.Recover;
			stateTimer = BiteRecover;
			return;
		}

		// Tail ends
		if (currentAttack == BossAttack.TailSweep && state == BossState.Active && Sprite != null && Sprite.Animation == TailSweepAnimName)
		{
			EnableAttackHitbox(BossAttack.TailSweep, false);
			ShowTelegraph(BossAttack.TailSweep, false);

			_tailHitboxEnabledThisSpin = false;

			Velocity = Vector2.Zero;
			state = BossState.Recover;
			stateTimer = TailRecover;
			return;
		}

		// Charge ends
		if (currentAttack == BossAttack.Charge && state == BossState.Active && Sprite != null && Sprite.Animation == ChargeAnimName)
		{
			EnableAttackHitbox(BossAttack.Charge, false);
			ShowTelegraph(BossAttack.Charge, false);

			_chargeActiveWindowOpen = false;

			Velocity = Vector2.Zero;
			state = BossState.Recover;
			stateTimer = ChargeRecover;
			return;
		}
	}

	protected override void OnDamageTaken(int damage)
	{
		base.OnDamageTaken(damage);
		bossUI?.UpdateBossHealth(_currentHealth);
	}

	protected override void Die()
	{
		state = BossState.Dead;
		Velocity = Vector2.Zero;

		if (_bossUIItem != null)
			_bossUIItem.Visible = false;

		HideAllTelegraphs();

		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		if (BodyContactDamageArea != null)
			SetHitboxEnabled(BodyContactDamageArea, false);

		UnhookPlayerDeathEvents(_player);

		base.Die();
	}

	public override void ResetEnemy()
	{
		base.ResetEnemy();

		if (_cachedMask)
			CollisionMask = _cachedPreFightCollisionMask;

		state = BossState.Idle;
		currentAttack = BossAttack.None;
		fightStarted = false;
		bossMusic.Stop();
		GD.Print("Boss Music Stopped");
		phase2 = false;
		_phase2RoarDone = false;
		Speed = baseSpeed;

		biteCd = 0f;
		tailCd = 0f;
		chargeCd = 0f;
		hitTargetsThisActive.Clear();
		_contactDamageCdByTarget.Clear();

		_biteHitboxEnabledThisBite = false;
		_tailHitboxEnabledThisSpin = false;
		_chargeActiveWindowOpen = false;
		_roarPulseFiredThisRoar = false;

		if (_bossUIItem != null) _bossUIItem.Visible = false;
		bossUI?.InitializeBoss(MaxHealth, _currentHealth);

		HideAllTelegraphs();
		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);
		if (BodyContactDamageArea != null) SetHitboxEnabled(BodyContactDamageArea, false);

		SetEntranceGateLocked(false);

		if (ArenaTriggerArea != null)
		{
			ArenaTriggerArea.Monitorable = true;
			ArenaTriggerArea.Monitoring = false;
			CallDeferred(nameof(ArmArenaTriggerNextFrame));
		}

		DisableAndDespawnPhase2Spawners();

		UpdateFacingAndAnimation(force: true);
	}
}
