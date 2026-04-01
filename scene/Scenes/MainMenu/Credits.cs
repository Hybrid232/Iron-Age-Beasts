using Godot;
using System;

public partial class Credits : Control
{
	private AnimationPlayer _creditsAnim;
	
	public override async void _Ready()
	{
		_creditsAnim = GetNode<AnimationPlayer>("AnimationPlayer");
		_creditsAnim.Play("Credits");
		
	}
	
	private void BackToMenu()
	{
		GetTree().ChangeSceneToFile("res://scene/Scenes/MainMenu/MainMenu.tscn");
	}
	
}
