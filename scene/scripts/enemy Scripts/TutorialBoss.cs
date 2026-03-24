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

	[ExportGroup("Boss UI")]
	[Export] public NodePath BossUIPath;

	private CanvasItem _bossUIItem;
	private IBossUI bossUI;

	[ExportGroup("Phase 2 (<=40% HP)")]
	[Export] public float Phase2SpeedMultiplier = 1.35f;
	[Export] public bool UnlockChargeAt40Percent = true;

	[ExportGroup("Phase 2 Roar (on enter)")]
	[Export] public float Phase2RoarTelegraph = 0.6f;
	[Export] public float Phase2RoarRecover = 0.7f;
	[Export] public float RoarKnockbackDistance = 420f;
	[Export] public float RoarKnockbackTime = 0.25f;
	[Export] public float RoarStunSeconds = 2.5f;

	[ExportGroup("Phase 2 Adds")]
	[Export] public int AddsToSpawnOnRoar = 3;

	// NEW: if group lookup fails, search under this node for EnemySpawner children
	[Export] public NodePath AddsSpawnerRootPath;

	public const string ADD_SPAWNER_GROUP = "ArenaAddSpawner";
	private const string ENEMY_GROUP = "Enemy";

	[ExportGroup("Ranges")]
	[Export] public float BiteRange = 40f;
	[Export] public float TailSweepRange = 85f;
	[Export] public float MinChargeRange = 120f;

	[ExportGroup("Damages")]
	[Export] public int BiteDamage = 20;
	[Export] public int TailDamage = 15;
	[Export] public int ChargeDamage = 30;

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

	private CanvasItem biteTelegraph;
	private CanvasItem tailTelegraph;
	private CanvasItem chargeTelegraph;

	[ExportGroup("Knockback Attacks")]
	[Export] public float BiteKnockbackDistance = 100f;
	[Export] public float BiteKnockbackTime = 0.20f;
	[Export] public float TailKnockbackDistance = 180f;
	[Export] public float TailKnockbackTime = 0.18f;
	[Export] public float ChargeKnockbackDistance = 250f;
	[Export] public float ChargeKnockbackTime = 2.0f;

	[ExportGroup("Debug")]
	[Export] public bool DebugPrintHitboxOverlaps = false;

	[ExportGroup("Facing / Orientation")]
	[Export] public bool FlipAttackDirection = true;

	[ExportGroup("Contact Damage (Body Touch)")]
	[Export] public Area2D BodyContactDamageArea;
	[Export] public int ContactDamage = 5;
	[Export] public float ContactKnockbackDistance = 140f;
	[Export] public float ContactKnockbackTime = 0.18f;
	[Export] public float ContactDamageCooldown = 0.40f;

	private readonly Dictionary<ulong, float> _contactDamageCdByTarget = new();

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

		biteTelegraph = ResolveTelegraph(BiteTelegraphPath, "BiteTelegraphPath");
		tailTelegraph = ResolveTelegraph(TailTelegraphPath, "TailTelegraphPath");
		chargeTelegraph = ResolveTelegraph(ChargeTelegraphPath, "ChargeTelegraphPath");
		HideAllTelegraphs();

		GD.Print($"[Boss] BossUIPath='{BossUIPath}' (isEmpty={BossUIPath == null || BossUIPath.IsEmpty})");

		_bossUIItem = (BossUIPath != null && !BossUIPath.IsEmpty)
			? GetNodeOrNull(BossUIPath) as CanvasItem
			: null;

		if (_bossUIItem == null)
			GD.PushWarning("[Boss] Could not find Boss UI node via BossUIPath (CanvasItem was null).");

		bossUI = (BossUIPath != null && !BossUIPath.IsEmpty)
			? GetNodeOrNull(BossUIPath) as IBossUI
			: null;

		if (bossUI == null)
			GD.PushWarning("[Boss] Boss UI node does not implement IBossUI OR was not found.");
		else
			bossUI.InitializeBoss(MaxHealth, _currentHealth);

		if (_bossUIItem != null) _bossUIItem.Visible = false;

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
	}

	private async void ArmArenaTriggerNextFrame()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		if (fightStarted) return;
		if (ArenaTriggerArea == null) return;

		ArenaTriggerArea.Monitoring = true;
		GD.Print("[Boss] Arena trigger armed.");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
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

		_player = player;
		_chasing = true;

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

		GD.Print("[Boss] Fight started; gate locked.");
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

			if (!_phase2RoarDone && state != BossState.Dead)
				StartPhase2Roar();
		}
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
		stateTimer = Phase2RoarTelegraph;

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
				stateTimer -= dt;
				if (stateTimer <= 0f)
					DoRoarPulseAndSpawnAdds();
				break;

			case BossState.Chasing:
				Chase(dt);
				TryPickAttack();
				break;

			case BossState.Telegraph:
			case BossState.Active:
			case BossState.Recover:
				if (!(state == BossState.Active && currentAttack == BossAttack.Charge))
					Velocity = Vector2.Zero;

				stateTimer -= dt;
				if (stateTimer <= 0f)
					AdvanceAttackPhase();
				break;
		}
	}

	private void DoRoarPulseAndSpawnAdds()
	{
		GD.Print("[Boss] ROAR pulse (global knockback + stun + adds).");

		if (_player != null && IsInstanceValid(_player))
		{
			Vector2 pushDir = (_player.GlobalPosition - GlobalPosition);
			if (pushDir == Vector2.Zero) pushDir = Vector2.Right;
			pushDir = pushDir.Normalized();

			_player.TriggerHitRecoil(pushDir, RoarKnockbackDistance, RoarKnockbackTime);

			if (_player is IStunnable stunnable)
				stunnable.ApplyStun(RoarStunSeconds);
			else
				GD.PushWarning("[Boss] Player does not implement IStunnable; stun was skipped.");
		}

		SpawnArenaAdds(AddsToSpawnOnRoar);

		state = BossState.Recover;
		stateTimer = Phase2RoarRecover;
	}

	private List<EnemySpawner> ResolveAddSpawners()
	{
		var results = new List<EnemySpawner>();

		// 1) Group lookup
		var groupNodes = GetTree().GetNodesInGroup(ADD_SPAWNER_GROUP);
		if (groupNodes != null)
		{
			foreach (var n in groupNodes)
				if (n is EnemySpawner sp) results.Add(sp);
		}

		// 2) Fallback: explicit root search
		if (results.Count == 0 && AddsSpawnerRootPath != null && !AddsSpawnerRootPath.IsEmpty)
		{
			var root = GetNodeOrNull(AddsSpawnerRootPath);
			if (root != null)
			{
				foreach (var child in root.FindChildren("*", "EnemySpawner", true, false))
					if (child is EnemySpawner sp) results.Add(sp);
			}
		}

		return results;
	}

	private void SpawnArenaAdds(int count)
	{
		if (count <= 0)
		{
			GD.Print("[Boss] AddsToSpawnOnRoar <= 0; skipping add spawns.");
			return;
		}

		var spawners = ResolveAddSpawners();
		GD.Print($"[Boss] Add spawners found: {spawners.Count} (group='{ADD_SPAWNER_GROUP}', rootPath='{AddsSpawnerRootPath}')");

		if (spawners.Count == 0)
		{
			GD.PushWarning("[Boss] No EnemySpawner found for adds. Put spawners in group 'ArenaAddSpawner' OR assign AddsSpawnerRootPath.");
			return;
		}

		int spawned = 0;
		foreach (var spawner in spawners)
		{
			if (spawned >= count) break;
			if (spawner == null || !IsInstanceValid(spawner)) continue;
			if (spawner.IsBossSpawner) continue;

			GD.Print($"[Boss] Spawning add via spawner '{spawner.Name}' at {spawner.GlobalPosition}");
			spawner.ForceRespawn();
			spawned++;
		}

		GD.Print($"[Boss] Spawned adds: {spawned}/{count}");
	}

	private void Chase(float dt)
	{
		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float dist = toPlayer.Length();
		if (dist < 1f)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 dir = toPlayer / dist;
		Velocity = dir * Speed;
	}

	private void TryPickAttack()
	{
		if (_player == null) return;
		if (state != BossState.Chasing) return;

		float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);
		bool chargeUnlocked = phase2 && UnlockChargeAt40Percent;

		if (dist <= BiteRange && biteCd <= 0f)
		{
			StartAttack(BossAttack.Bite);
			biteCd = BiteCooldown;
			return;
		}

		if (dist <= TailSweepRange && tailCd <= 0f)
		{
			StartAttack(BossAttack.TailSweep);
			tailCd = TailCooldown;
			return;
		}

		if (chargeUnlocked && dist >= MinChargeRange && chargeCd <= 0f)
		{
			StartAttack(BossAttack.Charge);
			chargeCd = ChargeCooldown;
			return;
		}
	}

	private void StartAttack(BossAttack attack)
	{
		currentAttack = attack;
		state = BossState.Telegraph;

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

			EnableAttackHitbox(currentAttack, true);

			if (currentAttack == BossAttack.Charge)
				Velocity = chargeDir * (Speed * 2.2f);

			return;
		}

		if (state == BossState.Active)
		{
			EnableAttackHitbox(currentAttack, false);
			ShowTelegraph(currentAttack, false);

			Velocity = Vector2.Zero;

			state = BossState.Recover;
			stateTimer = GetRecoverTime(currentAttack);
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

		int dmg = a switch
		{
			BossAttack.Bite => BiteDamage,
			BossAttack.TailSweep => TailDamage,
			BossAttack.Charge => ChargeDamage,
			_ => 0
		};

		if (hitbox == null || dmg <= 0) return;
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
			damageable.TakeDamage(dmg);

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
		if (_contactDamageCdByTarget.TryGetValue(id, out float cd) && cd > 0f)
			return;

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

	private CanvasItem ResolveTelegraph(NodePath path, string label)
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

		base.Die();
	}

	public override void ResetEnemy()
	{
		base.ResetEnemy();

		state = BossState.Idle;
		currentAttack = BossAttack.None;
		fightStarted = false;
		phase2 = false;
		_phase2RoarDone = false;
		Speed = baseSpeed;

		biteCd = 0f;
		tailCd = 0f;
		chargeCd = 0f;
		hitTargetsThisActive.Clear();
		_contactDamageCdByTarget.Clear();

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
			ArenaTriggerArea.Monitoring = false;
			CallDeferred(nameof(ArmArenaTriggerNextFrame));
		}
	}
}
