using Godot;

public class DodgeSystem
{
	private int dodgeSpeed;
	private float dodgeTime;
	private float dodgeCooldown;
	
	private bool isDodging;
	private float dodgeTimer;
	private float cooldownTimer;
	private Vector2 dodgeDirection;

	public bool IsDodging => isDodging;

	public DodgeSystem(int speed, float time, float cooldown)
	{
		dodgeSpeed = speed;
		dodgeTime = time;
		dodgeCooldown = cooldown;
	}

	public void UpdateCooldown(float dt)
	{
		if (cooldownTimer > 0)
			cooldownTimer -= dt;
	}

	public bool TryDodge(Vector2 direction)
	{
		if (Input.IsActionJustPressed("dodge") && 
			cooldownTimer <= 0 && 
			direction != Vector2.Zero)
		{
			isDodging = true;
			dodgeDirection = direction;
			dodgeTimer = dodgeTime;
			cooldownTimer = dodgeCooldown;
			return true;
		}
		return false;
	}

	public Vector2 UpdateDodge(float dt)
	{
		if (!isDodging)
			return Vector2.Zero;

		dodgeTimer -= dt;

		if (dodgeTimer <= 0)
		{
			isDodging = false;
			return Vector2.Zero;
		}

		return dodgeDirection * dodgeSpeed;
	}

	public Vector2 GetDodgeVelocity() => dodgeDirection * dodgeSpeed;
}
