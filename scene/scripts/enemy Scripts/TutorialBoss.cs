using Godot;
using System;

public partial class TutorialBoss : CharacterBody2D
{
	public enum BossState
	{
		Idle,
		Chasing,
		TailSweep,
		Bite,
		Charge,
		Recover,
		Dead
	}

	private BossState currentState = BossState.Idle;

	[Export] public int MaxHealth = 300;
	private int currentHealth;

	[Export] public float MoveSpeed = 100f;

	private Node2D player;

	public override void _Ready()
	{
		currentHealth = MaxHealth;
		player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (currentState == BossState.Dead)
			return;

		HandleState((float)delta);
	}

	private void HandleState(float delta)
	{
		switch (currentState)
		{
			case BossState.Idle:
				ChooseAttack();
				break;

			case BossState.Chasing:
				ChasePlayer(delta);
				break;

			case BossState.Recover:
				Velocity = Vector2.Zero;
				break;
		}

		MoveAndSlide();
	}

	private void ChasePlayer(float delta)
	{
		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * MoveSpeed;
	}

	private void ChooseAttack()
	{
		int attackChoice = GD.RandRange(0, 2);

		if (attackChoice == 0)
			currentState = BossState.TailSweep;
		else if (attackChoice == 1)
			currentState = BossState.Bite;
		else
			currentState = BossState.Charge;
	}

	public void TakeDamage(int amount)
	{
		currentHealth -= amount;

		if (currentHealth <= 0)
			Die();
	}

	private void Die()
	{
		currentState = BossState.Dead;
		QueueFree();
	}
}
