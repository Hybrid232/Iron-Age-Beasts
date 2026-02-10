using Godot;

public class MovementSystem
{
	private int playerSpeed;
	private Vector2 currentVelocity;
	private Vector2 lastMoveDirection = Vector2.Down;

	public Vector2 LastMoveDirection => lastMoveDirection;

	public MovementSystem(int speed)
	{
		playerSpeed = speed;
	}

	public Vector2 HandleInput()
	{
		currentVelocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * playerSpeed;

		if (currentVelocity != Vector2.Zero)
		{
			lastMoveDirection = currentVelocity.Normalized();
		}

		return lastMoveDirection;
	}

	public Vector2 GetVelocity() => currentVelocity;
}
