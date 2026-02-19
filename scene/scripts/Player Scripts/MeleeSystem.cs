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

	private float attackTimer;
	private bool isAttacking;
	private bool usedHitRecoilThisAttack;

	// Only track which enemies were hit this attack to prevent multi-hits
	private readonly HashSet<ulong> enemiesHitThisAttack = new();

	// Tunable offsets
	private float horizontalAttackOffset = 24f;
	private float verticalAttackOffset = 40f;
	private float verticalBias = 1.25f; // makes diagonals sit higher/lower

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
		
		// Not enough stamina, cannot attack
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

	// Diagonal-aware, tall-sprite-safe attack positioning
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
		
		// Prevent hitting the same enemy multiple times in one attack
		if (enemiesHitThisAttack.Contains(id))
			return;

		enemiesHitThisAttack.Add(id);

		Vector2 pushDir = (enemyRoot.GlobalPosition - player.GlobalPosition).Normalized();

		// ONLY apply player recoil if we hit ENEMY DAMAGE collider
		if (!usedHitRecoilThisAttack && IsEnemyDamageCollider(hitNode))
		{
			player.TriggerHitRecoil(pushDir);
			usedHitRecoilThisAttack = true;
		}

		// Apply knockback before damage (so enemy flies back even if it dies)
		ApplyEnemyKnockback(enemyRoot, pushDir);

		// Deal damage using IDamageable interface
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

	// 🔹 Group-based filtering (clean + scalable)
	private bool IsEnemyDamageCollider(Node node)
	{
		return node is Area2D area && area.IsInGroup("enemy_damage");
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
			// This checks if the node itself OR any parent has the BaseEnemy script
			if (current is BaseEnemy enemy) 
			{
				return enemy;
			}
			current = current.GetParent();
		}
		return null;
	}
}
