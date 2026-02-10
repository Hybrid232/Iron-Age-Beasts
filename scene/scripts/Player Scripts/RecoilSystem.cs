using Godot;

public class RecoilSystem
{
	private float hitRecoilDistance;
	private float hitRecoilTime;
	private float playerRecoilDistance;
	private float recoilTime;

	private float hitRecoilTimer;
	private float recoilTimer;
	private Vector2 hitRecoilVelocity;
	private Vector2 recoilVelocity;

	public RecoilSystem(float hitRecoilDist, float hitRecoilT, float playerRecoilDist, float recoilT)
	{
		hitRecoilDistance = hitRecoilDist;
		hitRecoilTime = hitRecoilT;
		playerRecoilDistance = playerRecoilDist;
		recoilTime = recoilT;
	}

	public void Update(float dt)
	{
		if (hitRecoilTimer > 0f)
		{
			hitRecoilTimer -= dt;
			if (hitRecoilTimer <= 0f)
				hitRecoilVelocity = Vector2.Zero;
		}

		if (recoilTimer > 0f)
		{
			recoilTimer -= dt;
			if (recoilTimer <= 0f)
				recoilVelocity = Vector2.Zero;
		}
	}

	public bool IsInRecoil() => hitRecoilTimer > 0f || recoilTimer > 0f;

	public Vector2 GetRecoilVelocity()
	{
		if (hitRecoilTimer > 0f)
			return hitRecoilVelocity;
		if (recoilTimer > 0f)
			return recoilVelocity;
		return Vector2.Zero;
	}

	public void StartHitRecoil(Vector2 pushDirection)
	{
		if (pushDirection == Vector2.Zero) return;
		Vector2 awayFromEnemy = -pushDirection.Normalized();
		float speed = hitRecoilTime > 0f ? (hitRecoilDistance / hitRecoilTime) : 0f;
		hitRecoilVelocity = awayFromEnemy * speed;
		hitRecoilTimer = hitRecoilTime;
	}

	public void StartPlayerRecoil(Vector2 attackDirection)
	{
		if (attackDirection == Vector2.Zero) return;
		Vector2 dir = attackDirection.Normalized();
		float speed = recoilTime > 0f ? (playerRecoilDistance / recoilTime) : 0f;
		recoilVelocity = -dir * speed;
		recoilTimer = recoilTime;
	}
}
