// Dilophosaurus.cs
using Godot;

public partial class Dilophosaurus : BaseEnemy
{
	public override void _Ready()
	{
		// Set Dilophosaurus-specific values
		Speed = 60f;
		StopDistance = 8f;
		AttackRange = 15f;
		AttackDamage = 15;
		AttackCooldown = 1.5f;
	}
	
	// Override attack start for dinosaur-specific behavior
	protected override void OnAttackStart()
	{
		GD.Print("🦖 Dilophosaurus bites ferociously!");
		// Here you can play attack animation
		// AnimationPlayer?.Play("bite_attack");
	}
	
	// Optional: Override attack end for cleanup
	protected override void OnAttackEnd()
	{
		GD.Print("Dilophosaurus attack complete");
		// Play idle/chase animation
		// AnimationPlayer?.Play("walk");
	}
	
	// Your existing signal handlers - just call the base methods
	public void _on_detection_area_body_entered(Node2D body)
	{
		// Only react if it's the player
		if (!body.IsInGroup(PlayerGroup))
			return;
		
		OnPlayerDetected(body);
	}
	
	public void _on_detection_area_body_exited(Node2D body)
	{
		// Make sure it's actually the player leaving
		if (body != _player)
			return;
		
		OnPlayerLost(body);
	}
}
