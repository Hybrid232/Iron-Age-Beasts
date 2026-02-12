using Godot;

public class MovementSystem
{
	private int baseSpeed;

	public MovementSystem(int speed)
	{
		baseSpeed = speed;
	}

	public Vector2 HandleInput()
	{
		return Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
	}

	public Vector2 GetVelocity(Vector2 direction, float speedMultiplier)
	{
		return direction * baseSpeed * speedMultiplier;
	}
}
