using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialBoss : BaseEnemy
{
	private enum BossState
	{
		Chasing,
		Telegraph,
		Active,
		Recover,
		Dead
	}

	private enum BossAttack
	{
		None,
		Bite,
		TailSweep,
		Charge
	}

	[ExportGroup("Boss UI")]
	[Export] public NodePath BossUIPath;

	[ExportGroup("Phase 2 (<=40% HP)")]
	[Export] public float Phase2SpeedMultiplier = 1.35f;
	[Export] public bool UnlockChargeAt40Percent = true;

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

	[ExportGroup("Knockback")]
	[Export] public bool DebugPrintHitboxOverlaps = false;

	private IBossUI bossUI;

	private BossState state = BossState.Chasing;
	private BossAttack currentAttack = BossAttack.None;

	private float stateTimer;
	private float biteCd, tailCd, chargeCd;

	private bool phase2;
	private float baseSpeed;

	private Vector2 chargeDir = Vector2.Zero;

	// Prevents multi-hits per active window
	private readonly HashSet<ulong> hitTargetsThisActive = new();

	public override void _Ready()
	{
		base._Ready();

		// Your player group is "Player"
		PlayerGroup = "Player";

		_player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Node2D;
		_chasing = _player != null;

		baseSpeed = Speed;

		// NEW: Make range checks match the actual hitbox collision size
		AutoFillRangesFromHitboxes();

		// Start hitboxes disabled (including their shapes, recursively)
		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		if (BossUIPath != null && !BossUIPath.IsEmpty)
		{
			var uiNode = GetNodeOrNull(BossUIPath);
			bossUI = uiNode as IBossUI;
		}

		bossUI?.InitializeBoss(MaxHealth, _currentHealth);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		float dt = (float)delta;
		biteCd = Math.Max(0, biteCd - dt);
		tailCd = Math.Max(0, tailCd - dt);
		chargeCd = Math.Max(0, chargeCd - dt);

		HandlePhase2();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Reacquire player if needed
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Node2D;
			_chasing = _player != null;
		}

		if (state == BossState.Dead)
			return;

		UpdateState(dt);

		// Deal damage during Active (works even if player was already inside the area)
		if (state == BossState.Active)
			ApplyActiveHitboxDamage(currentAttack);

		MoveAndSlide();
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
		}
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
			case BossState.Chasing:
				Chase(dt);
				TryPickAttack();
				break;

			case BossState.Telegraph:
			case BossState.Active:
			case BossState.Recover:
				// stop during telegraph/recover; during active only charge moves
				if (!(state == BossState.Active && currentAttack == BossAttack.Charge))
					Velocity = Vector2.Zero;

				stateTimer -= dt;
				if (stateTimer <= 0f)
					AdvanceAttackPhase();
				break;
		}
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

		// Disable everything until Active
		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

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
			EnableAttackHitbox(currentAttack, true);

			if (currentAttack == BossAttack.Charge)
				Velocity = chargeDir * (Speed * 2.2f);

			return;
		}

		if (state == BossState.Active)
		{
			EnableAttackHitbox(currentAttack, false);
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
			case BossAttack.Bite:
				SetHitboxEnabled(BiteHitbox, enabled);
				break;
			case BossAttack.TailSweep:
				SetHitboxEnabled(TailHitbox, enabled);
				break;
			case BossAttack.Charge:
				SetHitboxEnabled(ChargeHitbox, enabled);
				break;
		}
	}

	/// <summary>
	/// Tail sweep should be 360 degrees -> no rotation needed, just keep it centered.
	/// Charge should face player -> rotate hitbox if its shape is directional.
	/// </summary>
	private void PrepareHitboxForAttack(BossAttack a)
	{
		if (_player == null) return;

		switch (a)
		{
			case BossAttack.TailSweep:
				if (TailHitbox != null)
				{
					TailHitbox.Rotation = 0f;
				}
				break;

			case BossAttack.Charge:
				if (ChargeHitbox != null)
				{
					var dir = _player.GlobalPosition - GlobalPosition;
					if (dir.Length() > 0.001f)
						ChargeHitbox.Rotation = dir.Angle();
				}
				break;

			case BossAttack.Bite:
				if (BiteHitbox != null)
				{
					var dir = _player.GlobalPosition - GlobalPosition;
					if (dir.Length() > 0.001f)
						BiteHitbox.Rotation = dir.Angle();
				}
				break;
		}
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
		if (DebugPrintHitboxOverlaps)
			GD.Print($"{a} bodies overlapping: {bodies.Count}");

		foreach (var obj in bodies)
		{
			if (obj is not Node2D body) continue;
			if (body == this) continue;

			// Only hit player (group is "Player")
			if (!body.IsInGroup("Player")) continue;

			if (body is not IDamageable damageable) continue;

			ulong id = body.GetInstanceId();
			if (hitTargetsThisActive.Contains(id)) continue;

			hitTargetsThisActive.Add(id);

			damageable.TakeDamage(dmg);

			if (a == BossAttack.TailSweep && body is Player p)
			{
				Vector2 pushDir = (p.GlobalPosition - GlobalPosition).Normalized();
				p.TriggerHitRecoil(pushDir);
			}
		}
	}

	/// <summary>
	/// NEW: Set BiteRange/TailSweepRange/MinChargeRange from the actual hitbox collision sizes.
	/// This keeps "start attack" range consistent with "can hit" range.
	/// </summary>
	private void AutoFillRangesFromHitboxes()
	{
		BiteRange = Math.Max(BiteRange, GetHitboxReachPixels(BiteHitbox));
		TailSweepRange = Math.Max(TailSweepRange, GetHitboxReachPixels(TailHitbox));

		// Charge "minimum distance to start charging" is gameplay-y, but if you want it tied to the hitbox:
		MinChargeRange = Math.Max(MinChargeRange, GetHitboxReachPixels(ChargeHitbox));

		GD.Print($"[Boss Ranges] BiteRange={BiteRange}, TailSweepRange={TailSweepRange}, MinChargeRange={MinChargeRange}");
	}

	private float GetHitboxReachPixels(Area2D hitbox)
	{
		if (hitbox == null) return 0f;

		float best = 0f;

		// CollisionShape2D (direct or nested)
		foreach (var node in hitbox.FindChildren("*", "CollisionShape2D", true, false))
		{
			if (node is not CollisionShape2D cs) continue;
			if (cs.Shape == null) continue;

			best = Math.Max(best, ShapeReach(cs.Shape));
		}

		// CollisionPolygon2D (optional)
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

	private float ShapeReach(Shape2D shape)
	{
		return shape switch
		{
			CircleShape2D c => c.Radius,
			RectangleShape2D r => r.Size.Length() * 0.5f, // half diagonal
			CapsuleShape2D cap => cap.Radius + (cap.Height * 0.5f),
			_ => 0f
		};
	}

	private void SetHitboxEnabled(Area2D hitbox, bool enabled)
	{
		if (hitbox == null) return;

		// Toggle detection
		hitbox.Monitoring = enabled;
		hitbox.Monitorable = enabled;

		// Toggle shapes recursively (fixes nested shapes + debug visibility)
		foreach (var node in hitbox.FindChildren("*", "CollisionShape2D", true, false))
			((CollisionShape2D)node).Disabled = !enabled;

		foreach (var node in hitbox.FindChildren("*", "CollisionPolygon2D", true, false))
			((CollisionPolygon2D)node).Disabled = !enabled;
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

		SetHitboxEnabled(BiteHitbox, false);
		SetHitboxEnabled(TailHitbox, false);
		SetHitboxEnabled(ChargeHitbox, false);

		base.Die();
	}
}
