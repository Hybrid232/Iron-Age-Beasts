using Godot;

public class DodgeSystem
{
	private int dodgeSpeed;
	private float dodgeTime;
	private float dodgeCooldown;
	private int staminaCost;
	private bool isDodging;
	private float dodgeTimer;
	private float cooldownTimer;
	private Vector2 dodgeDirection;

	// ===== I-FRAMES (Dark Souls style) =====
	// Normalized to the dodge duration (0..1)
	private float iFrameStartNormalized;
	private float iFrameDurationNormalized;

	public bool IsDodging => isDodging;

	public bool IsInIFrames
	{
		get
		{
			if (!isDodging) return false;

			// Progress 0..1 over the dodge
			float elapsed = dodgeTime - dodgeTimer; // 0..dodgeTime
			float t = dodgeTime <= 0 ? 1f : (elapsed / dodgeTime);

			return t >= iFrameStartNormalized && t <= (iFrameStartNormalized + iFrameDurationNormalized);
		}
	}

	public DodgeSystem(
		int speed,
		float time,
		float cooldown,
		int staminaCost,
		float iFrameStartNormalized = 0.10f,
		float iFrameDurationNormalized = 0.50f
	)
	{
		dodgeSpeed = speed;
		dodgeTime = time;
		dodgeCooldown = cooldown;
		this.staminaCost = staminaCost;

		this.iFrameStartNormalized = Mathf.Clamp(iFrameStartNormalized, 0f, 1f);
		this.iFrameDurationNormalized = Mathf.Clamp(iFrameDurationNormalized, 0f, 1f);
	}

	public void UpdateCooldown(float dt)
	{
		if (cooldownTimer > 0)
			cooldownTimer -= dt;
	}

	public bool TryDodge(Vector2 direction, HealthSystem health)
	{
		if (Input.IsActionJustPressed("dodge") &&
			cooldownTimer <= 0 &&
			direction != Vector2.Zero &&
			health.CanAct())
		{
			health.ChangeStamina(-staminaCost); // Consume stamina

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
