using Godot;
using System;

public partial class Instructions : Control
{
	public override async void _Ready() 
	{
		Button continueButton = GetNode<Button>("ContinueButton");
		
		continueButton.GrabFocus();
		continueButton.Pressed += OnContinuePressed;
	}
	
	private void OnContinuePressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Scenes/Tutorial.tscn");
	}
}
