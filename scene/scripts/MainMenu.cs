using Godot;
using System;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Button startButton = GetNode<Button>("StartButton");
		startButton.Pressed += OnStartPressed;
		
		Button quitButton = GetNode<Button>("QuitButton");
		quitButton.Pressed += OnQuitPressed;
	}

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Sandbox/TestWorld.tscn");
	}
	
	private void OnQuitPressed()
	{
		GD.Print("Quitting Game...");
		GetTree().Quit();
		
	}
}
