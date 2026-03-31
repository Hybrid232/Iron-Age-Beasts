using Godot;
using System;

public partial class SceneTransition : CanvasLayer
{
	private AnimationPlayer _anim;

	public override void _Ready()
	{
		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public async void ChangeScene(string path)
	{
		_anim.Play("Fade_In");
		await ToSignal(_anim, "animation_finished");

		GetTree().ChangeSceneToFile(path);

		_anim.Play("Fade_Out");
	}
}
