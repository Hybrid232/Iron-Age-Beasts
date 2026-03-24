using Godot;

public partial class EnemySpawner : Node2D
{
	[ExportGroup("Spawn")]
	[Export] private PackedScene _enemyScene;

	[Export] private NodePath _enemyContainerPath;

	[ExportGroup("Activation")]
	[Export] public bool StartDisabled = false;

	[Export] public bool SpawnOnReady = true;

	[ExportGroup("Rules")]
	[Export] private bool _isBossSpawner = false;

	public const string GROUP_NAME = "EnemySpawner";

	[ExportGroup("Overrides (optional)")]
	[Export] public EnemyOverrides Overrides = new EnemyOverrides();

	[GlobalClass]
	public partial class EnemyOverrides : Resource
	{
		[Export] public bool Enabled = false;

		[Export] public bool OverrideMaxHealth = false;
		[Export] public int MaxHealth = 50;

		[Export] public bool OverrideSpeed = false;
		[Export] public float Speed = 60f;

		[Export] public bool OverrideStopDistance = false;
		[Export] public float StopDistance = 8f;

		[Export] public bool OverrideAttackRange = false;
		[Export] public float AttackRange = 30f;

		[Export] public bool OverrideAttackCooldown = false;
		[Export] public float AttackCooldown = 1.5f;

		[Export] public bool OverrideAttackDamage = false;
		[Export] public int AttackDamage = 20;

		[Export] public bool OverrideXpReward = false;
		[Export] public int XpReward = 10;

		// Example: enemy-specific knobs
		[Export] public bool OverrideDiloSearchTime = false;
		[Export] public float DiloSearchTime = 3.0f;

		[Export] public bool OverrideDiloPatrolRadius = false;
		[Export] public float DiloPatrolRadius = 300f;
	}

	private Node _enemyContainer;
	private BaseEnemy _currentEnemy;

	public bool IsBossSpawner => _isBossSpawner;
	public bool IsEnabled { get; private set; } = true;

	public BaseEnemy CurrentEnemy => (_currentEnemy != null && IsInstanceValid(_currentEnemy)) ? _currentEnemy : null;

	public override void _Ready()
	{
		AddToGroup(GROUP_NAME);

		_enemyContainer = (_enemyContainerPath != null && !_enemyContainerPath.IsEmpty)
			? GetNodeOrNull(_enemyContainerPath)
			: GetParent();

		IsEnabled = !StartDisabled;

		if (IsEnabled && SpawnOnReady)
			SpawnIfNeeded();
	}

	public void EnableAndSpawn()
	{
		EnableAndSpawn(null);
	}

	// Boss uses this to spawn AND instantly aggro the player (ignore detection radius)
	public void EnableAndSpawn(Player chaseTarget)
	{
		IsEnabled = true;
		SpawnIfNeeded();

		if (chaseTarget != null && CurrentEnemy != null)
			CurrentEnemy.ForceAggro(chaseTarget);
	}

	public void DisableAndDespawn()
	{
		IsEnabled = false;

		if (_currentEnemy != null && IsInstanceValid(_currentEnemy))
		{
			_currentEnemy.QueueFree();
			_currentEnemy = null;
		}
	}

	public void SpawnIfNeeded()
	{
		if (!IsEnabled)
			return;

		if (_currentEnemy != null && IsInstanceValid(_currentEnemy))
			return;

		if (_enemyScene == null)
		{
			GD.PrintErr($"[EnemySpawner:{Name}] _enemyScene not assigned.");
			return;
		}

		Node inst = _enemyScene.Instantiate();
		if (inst is not BaseEnemy enemy)
		{
			GD.PrintErr($"[EnemySpawner:{Name}] Spawned scene is not BaseEnemy (got {inst.GetType().Name}).");
			inst.QueueFree();
			return;
		}

		_currentEnemy = enemy;

		(_enemyContainer ?? GetParent()).AddChild(enemy);
		enemy.GlobalPosition = GlobalPosition;

		ApplyOverrides(enemy);
	}

	public void ForceRespawn(Player chaseTarget = null)
	{
		if (!IsEnabled)
			return;

		if (_currentEnemy != null && IsInstanceValid(_currentEnemy))
		{
			_currentEnemy.QueueFree();
			_currentEnemy = null;
		}

		SpawnIfNeeded();

		if (chaseTarget != null && CurrentEnemy != null)
			CurrentEnemy.ForceAggro(chaseTarget);
	}

	private void ApplyOverrides(BaseEnemy enemy)
	{
		if (Overrides == null) return;
		if (!Overrides.Enabled) return;

		if (Overrides.OverrideMaxHealth) enemy.MaxHealth = Overrides.MaxHealth;
		if (Overrides.OverrideSpeed) enemy.Speed = Overrides.Speed;
		if (Overrides.OverrideStopDistance) enemy.StopDistance = Overrides.StopDistance;
		if (Overrides.OverrideAttackRange) enemy.AttackRange = Overrides.AttackRange;
		if (Overrides.OverrideAttackCooldown) enemy.AttackCooldown = Overrides.AttackCooldown;
		if (Overrides.OverrideXpReward) enemy.XpReward = Overrides.XpReward;

		// AttackDamage is public in BaseEnemy
		if (Overrides.OverrideAttackDamage) enemy.AttackDamage = Overrides.AttackDamage;

		if (enemy is Dilophosaurus dilo)
		{
			if (Overrides.OverrideDiloSearchTime) dilo.SearchTime = Overrides.DiloSearchTime;
			if (Overrides.OverrideDiloPatrolRadius) dilo.PatrolRadius = Overrides.DiloPatrolRadius;
		}

		// After overrides, reset internal state so MaxHealth applies immediately.
		enemy.ResetEnemy();
	}
}
