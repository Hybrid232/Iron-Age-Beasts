using Godot;
using System;

public partial class MainMenu : Control
{
	private Control _menuContainer;
	private Control _optionsBG;
	private AnimationPlayer _fadeAnim;

	public override void _Ready()
	{
		_fadeAnim = GetNode<AnimationPlayer>("FadeLayer/AnimationPlayer");
		
		//Play animation when menu starts.
		_fadeAnim.Play("Fade_Out");
		
		_menuContainer = GetNode<Control>("MenuContainer");
		_optionsBG = GetNode<Control>("OptionsBG");

		_optionsBG.Visible = false;

		Button startButton = GetNode<Button>("MenuContainer/StartButton");
		startButton.Pressed += OnStartPressed;

		Button settingsButton = GetNode<Button>("MenuContainer/OptionsButton");
		settingsButton.Pressed += OnSettingsPressed;

		Button quitButton = GetNode<Button>("MenuContainer/QuitButton");
		quitButton.Pressed += OnQuitPressed;
		
		Button backButton = GetNode<Button>("OptionsBG/BackButton");
		backButton.Pressed += OnBackPressed;
	}

	private async void OnStartPressed()
	{
		_fadeAnim.Play("Fade_In");
		
		await ToSignal(_fadeAnim, "animation_finished");
		
		GetTree().ChangeSceneToFile("res://scene/Scenes/Tutorial.tscn");
	}

	private void OnSettingsPressed()
	{
		_menuContainer.Visible = false;
		_optionsBG.Visible = true;
	}

	private async void OnQuitPressed()
	{
		_fadeAnim.Play("Fade_In");
		await ToSignal(_fadeAnim, "animation_finished");
		GetTree().Quit();
	}
	
	private void OnBackPressed()
	{
		_menuContainer.Visible = true;
		_optionsBG.Visible = false;
	}
}
