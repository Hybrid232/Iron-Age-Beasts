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
	private int hitsToKillEnemy;
	private Player player;

	private float attackTimer;
	private bool isAttacking;
	private bool usedHitRecoilThisAttack;

	private readonly Dictionary<ulong, int> enemyHitCounts = new();
	private readonly HashSet<ulong> enemiesHitThisAttack = new();

	//  Tunable offsets
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
		int hitsToKill,
		Player playerRef)
	{
		attackPivot = pivot;
		attackHitbox = hitbox;
		attackDuration = duration;
		attackRange = range;
		enemyKnockbackDistance = knockbackDist;
		enemyKnockbackTime = knockbackTime;
		hitsToKillEnemy = hitsToKill;
		player = playerRef;
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

		ApplyEnemyKnockback(enemyRoot, pushDir);

		if (!enemyHitCounts.ContainsKey(id))
			enemyHitCounts[id] = 0;

		enemyHitCounts[id]++;
		GD.Print($"Hit enemy {enemyRoot.Name} ({id}) = {enemyHitCounts[id]}/{hitsToKillEnemy}");

		if (enemyHitCounts[id] >= hitsToKillEnemy)
		{
			enemyHitCounts.Remove(id);
			enemiesHitThisAttack.Remove(id);

			if (enemyRoot.IsInsideTree())
				enemyRoot.QueueFree();
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
		if (hit is CharacterBody2D cb) return cb;
		if (hit is Node2D n2d && n2d.IsInGroup("enemy")) return n2d;

		Node current = hit;
		while (current != null)
		{
			if (current is CharacterBody2D body) return body;
			if (current is Node2D node2D && node2D.IsInGroup("enemy")) return node2D;
			current = current.GetParent();
		}
		return null;
	}
}
