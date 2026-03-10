using Godot;
using System.Collections.Generic;

public class MeleeSystem
{
	private Node2D attackPivot;
	private Area2D attackHitbox;
	private float attackDuration;
	private float attackRange;
	private float enemyKnockbackDistance;
	private float enemyKnockbackTime;
	private int meleeDamage;
	private int staminaCost;
	private int staminaBuffer;

	private HealthSystem healthSystem;
	private Player player;
	
	//private string targetGroup;
	//private Node2D owner;

	private float attackTimer;
	private bool isAttacking;

	// Re-used as "once per attack" recoil gate
	private bool usedHitRecoilThisAttack;

	// Only track which enemies were hit this attack to prevent multi-hits
	private readonly HashSet<ulong> enemiesHitThisAttack = new();

	// Tunable offsets
	private float horizontalAttackOffset = 24f;
	private float verticalAttackOffset = 40f;
	private float verticalBias = 1.25f;

	public bool IsAttacking => isAttacking;

	public MeleeSystem(
		Node2D pivot,
		Area2D hitbox,
		float duration,
		float range,
		float knockbackDist,
		float knockbackTime,
		int damage,
		int staminaCost,
		int staminaBuffer,
		Player playerRef,
		HealthSystem healthSys)
	{
		attackPivot = pivot;
		attackHitbox = hitbox;
		attackDuration = duration;
		attackRange = range;
		enemyKnockbackDistance = knockbackDist;
		enemyKnockbackTime = knockbackTime;
		meleeDamage = damage;

		this.staminaCost = staminaCost;
		this.staminaBuffer = staminaBuffer;

		player = playerRef;
		healthSystem = healthSys;
	}

	public void Initialize()
	{
		EnableAttackHitbox(false);
		attackHitbox.AreaEntered += OnAttackAreaEntered;
		attackHitbox.BodyEntered += OnAttackBodyEntered;
	}

	public void TryAttack(Vector2 direction, RecoilSystem recoilSystem)
	{
		if (!Input.IsActionJustPressed("attack") || direction == Vector2.Zero)
			return;

		if (!healthSystem.CanAct())
		{
			GD.Print("Not enough Stamina!");
			return;
		}

		healthSystem.ChangeStamina(-staminaCost);

		isAttacking = true;
		attackTimer = attackDuration;
		enemiesHitThisAttack.Clear();
		usedHitRecoilThisAttack = false;

		StartAttack(direction);
	}

	private void StartAttack(Vector2 direction)
	{
		Vector2 dir = direction.Normalized();

		Vector2 offset = new Vector2(
			dir.X * horizontalAttackOffset,
			dir.Y * verticalAttackOffset * verticalBias
		);

		attackPivot.Position = offset;
		attackPivot.Rotation = dir.Angle();
		EnableAttackHitbox(true);
	}

	public void UpdateAttack(float dt)
	{
		attackTimer -= dt;
		if (attackTimer <= 0f)
		{
			isAttacking = false;
			EnableAttackHitbox(false);
		}
	}

	private void EnableAttackHitbox(bool enabled)
	{
		attackHitbox.Monitoring = enabled;
		attackHitbox.Monitorable = enabled;
	}

	private void OnAttackAreaEntered(Area2D area) => HandleMeleeHit(area);
	private void OnAttackBodyEntered(Node2D body) => HandleMeleeHit(body);

	private void HandleMeleeHit(Node hitNode)
	{
		if (!isAttacking || hitNode == null || hitNode == player)
			return;

		Node2D enemyRoot = GetEnemyRootFromHit(hitNode);
		if (enemyRoot == null)
			return;

		ulong id = enemyRoot.GetInstanceId();

		if (enemiesHitThisAttack.Contains(id))
			return;

		enemiesHitThisAttack.Add(id);

		Vector2 toEnemy = enemyRoot.GlobalPosition - player.GlobalPosition;
		Vector2 towardEnemy = toEnemy == Vector2.Zero ? Vector2.Zero : toEnemy.Normalized();

		// NEW: Push player back slightly (attacker recoil), once per attack.
		// Toggle off in Player inspector via enableAttackerRecoilOnHit.
		if (!usedHitRecoilThisAttack)
		{
			player.TriggerAttackerRecoil(towardEnemy);
			usedHitRecoilThisAttack = true;
		}

		// Apply knockback to enemy
		ApplyEnemyKnockback(enemyRoot, towardEnemy);

		// Deal damage
		if (enemyRoot is IDamageable damageable)
		{
			damageable.TakeDamage(meleeDamage);
			GD.Print($"Melee attack dealt {meleeDamage} damage to {enemyRoot.Name}");
		}
		else
		{
			GD.PrintErr($"Enemy {enemyRoot.Name} does not implement IDamageable!");
		}
	}

	private void ApplyEnemyKnockback(Node enemyNode, Vector2 pushDir)
	{
		if (pushDir == Vector2.Zero)
			return;

		if (enemyNode is BaseEnemy baseEnemy)
		{
			baseEnemy.ApplyKnockback(pushDir, enemyKnockbackDistance, enemyKnockbackTime);
		}
		else if (enemyNode is CharacterBody2D enemyBody)
		{
			enemyBody.Velocity += pushDir * (enemyKnockbackDistance * 10f);
		}
		else if (enemyNode is Node2D enemy2D)
		{
			enemy2D.GlobalPosition += pushDir * enemyKnockbackDistance;
		}
	}

	private Node2D GetEnemyRootFromHit(Node hit)
	{
		Node current = hit;
		while (current != null)
		{
			if (current is BaseEnemy enemy)
				return enemy;

			current = current.GetParent();
		}
		return null;
	}
}
