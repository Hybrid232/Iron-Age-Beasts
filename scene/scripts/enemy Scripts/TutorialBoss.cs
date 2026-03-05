using Godot;
using System;

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
	[Export] public float MinChargeRange = 120f; // only charge if player is far enough

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

	private IBossUI bossUI;

	private BossState state = BossState.Chasing;
	private BossAttack currentAttack = BossAttack.None;

	private float stateTimer;
	private float biteCd, tailCd, chargeCd;

	private bool phase2;
	private float baseSpeed;

	private Vector2 chargeDir = Vector2.Zero;

	public override void _Ready()
	{
		base._Ready();

		// BaseEnemy uses _player + _chasing; ensure we have player reference immediately
		_player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		_chasing = _player != null;

		baseSpeed = Speed;

		SetupHitbox(BiteHitbox, BiteDamage);
		SetupHitbox(TailHitbox, TailDamage);
		SetupHitbox(ChargeHitbox, ChargeDamage);

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
		// keep BaseEnemy knockback behavior
		// NOTE: BaseEnemy _PhysicsProcess includes a full chase/attack routine we don't want.
		// So we copy its knockback early-return behavior by calling base first would run its AI.
		// Instead, we rely on BaseEnemy.ApplyKnockback() data by checking Velocity override:
		// Easiest approach: DO NOT call base._PhysicsProcess here; we implement boss AI entirely.

		float dt = (float)delta;

		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
			_chasing = _player != null;
		}

		if (state == BossState.Dead)
			return;

		// If you want boss knockback support like BaseEnemy:
		// BaseEnemy has private knockback fields, so boss can't read them.
		// If knockback is important for the boss, we should refactor BaseEnemy later.
		// For now: boss ignores knockback (or we implement boss-local knockback).

		UpdateState(dt);
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

			// Optional: make him “meaner” in phase2
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
				Velocity = Vector2.Zero; // except charge sets velocity during active
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
		float dist = GlobalPosition.DistanceTo(_player.GlobalPosition);

		bool chargeUnlocked = phase2 && UnlockChargeAt40Percent;

		// Priority order (Souls-like):
		// 1) Bite if very close
		// 2) Tail if close/mid
		// 3) Charge if far AND unlocked
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

		// cache charge direction at commit time
		if (attack == BossAttack.Charge)
			chargeDir = (_player.GlobalPosition - GlobalPosition).Normalized();

		// all hitboxes off at start
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

			// enable correct hitbox and behavior
			EnableAttackHitbox(currentAttack, true);

			if (currentAttack == BossAttack.Charge)
			{
				// move during charge active
				Velocity = chargeDir * (Speed * 2.2f);
			}

			return;
		}

		if (state == BossState.Active)
		{
			// end active window
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

	private void SetupHitbox(Area2D hitbox, int damage)
	{
		if (hitbox == null) return;

		hitbox.BodyEntered += body =>
		{
			if (state != BossState.Active) return; // safety
			if (body is IDamageable dmg)
				dmg.TakeDamage(damage);
		};
	}

	private void SetHitboxEnabled(Area2D hitbox, bool enabled)
	{
		if (hitbox == null) return;
		hitbox.Monitoring = enabled;
		hitbox.Monitorable = enabled;
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
