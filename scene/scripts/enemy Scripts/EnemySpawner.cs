using Godot;

public partial class EnemySpawner : Node2D
{
	[ExportGroup("Spawn")]
	[Export] private PackedScene _enemyScene;

	// Optional: spawn under a container node for organization
	[Export] private NodePath _enemyContainerPath;

	// If true, checkpoints won't respawn this spawner unless allowed
	[ExportGroup("Rules")]
	[Export] private bool _isBossSpawner = false;

	public const string GROUP_NAME = "EnemySpawner";

	private Node _enemyContainer;
	private BaseEnemy _currentEnemy;

	public bool IsBossSpawner => _isBossSpawner;

	public override void _Ready()
	{
		AddToGroup(GROUP_NAME);

		_enemyContainer = (_enemyContainerPath != null && !_enemyContainerPath.IsEmpty)
			? GetNodeOrNull(_enemyContainerPath)
			: GetParent();

		SpawnIfNeeded();
	}

	public void SpawnIfNeeded()
	{
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
	}

	public void ForceRespawn()
	{
		if (_currentEnemy != null && IsInstanceValid(_currentEnemy))
		{
			_currentEnemy.QueueFree();
			_currentEnemy = null;
		}

		SpawnIfNeeded();
	}
}
