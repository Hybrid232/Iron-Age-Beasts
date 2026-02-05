using Godot;
using System;

public partial class MainMenu : Control
{
	private Control _menuContainer;
	private Control _optionsBG;

	public override void _Ready()
	{
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

	private void OnStartPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Sandbox/TestWorld.tscn");
	}

	private void OnSettingsPressed()
	{
		_menuContainer.Visible = false;
		_optionsBG.Visible = true;
	}

	private void OnQuitPressed()
	{
		GD.Print("Quitting Game...");
		GetTree().Quit();
	}
	
	private void OnBackPressed()
	{
		_menuContainer.Visible = true;
		_optionsBG.Visible = false;
	}
}
