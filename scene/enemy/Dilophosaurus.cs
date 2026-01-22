using Godot;
using System;

int speed = 25;
bool playerChase = false;
bool player = null;




privade void OnDetectionAreaBodyEntered(body)
{
	player = body;
	playerChase = true;
}

private void OnDetectionAreaBodyExited(body)
{
	player = null;
	playerChase = false
}

public partial class dilophosaurus : CharactherBody2D
{
	private bool playerDetected = false;
	{
		private void _on_detection_area_body_entered(Node2D body)
		{
			GD.Print("$Entered: {body,Name}");
			
			if (body.IsInGroup("Player"))
			{
				playerDetected = true;
				GD.Print("Player detected");
			}
		}
		private void _on_detection_area_body_exited(Node2D body)
		{
			GD.Print($"Exited: {body.Name}");
			
			if (body.IsInGroup("Player"))
			{
				playerDetected = false;
				GD.Print("plater left detection area")
			}
		}
	}
}
