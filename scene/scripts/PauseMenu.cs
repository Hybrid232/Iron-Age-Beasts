using Godot;
using System;

public partial class PauseMenu : Control
{
	private Control _armoryPanel;
	private Control _inventoryPanel;
	private Control _settingsPanel;
	private Control _pauseBar;

	private bool _menuOpen = false;
	private Player _player;

	public override void _Ready()
	{
		_armoryPanel = GetNode<Control>("PauseBar/Armory/ArmoryBar");
		_inventoryPanel = GetNode<Control>("PauseBar/Inventory/InventoryBar");
		_settingsPanel = GetNode<Control>("PauseBar/Menu/SystemBar");

		_pauseBar = GetNode<Control>("PauseBar");

		// Start hidden
		HideAllPanels();
		Visible = false;

		// IMPORTANT: ensures input works even when invisible
		ProcessMode = ProcessModeEnum.Always;

		// Get player reference (make sure player is in group "player")
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		
		GetNode<Button>("PauseBar/Armory").Pressed += OnArmoryPressed;
   		GetNode<Button>("PauseBar/Inventory").Pressed += OnInventoryPressed;
   		GetNode<Button>("PauseBar/Menu").Pressed += OnMenuPressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Resume").Pressed += OnUnpausePressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Options").Pressed += OnOptionsPressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Menu").Pressed += OnMainMenuPressed;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("pause"))
		{
			ToggleMenu();
			GetViewport().SetInputAsHandled();
		}
	}

	private void ToggleMenu()
	{
		if (_menuOpen)
			CloseMenu();
		else
			OpenMenu();
	}

	private void OpenMenu()
{
	_menuOpen = true;
	Visible = true;

	

	float screenWidth = GetViewportRect().Size.X;
	float panelWidth = _pauseBar.Size.X;

	// Start off-screen
	_pauseBar.Position = new Vector2(screenWidth, _pauseBar.Position.Y);

	var tween = CreateTween();
	tween.TweenProperty(
		_pauseBar,
		"position:x",
		screenWidth - panelWidth,
		0.25f
	);

	HideAllPanels();
	_settingsPanel.Visible = true;
}

	private void CloseMenu()
{
	_menuOpen = false;

	float screenWidth = GetViewportRect().Size.X;

	var tween = CreateTween();
	tween.TweenProperty(
		_pauseBar,
		"position:x",
		screenWidth,
		0.25f
	);

	tween.TweenCallback(Callable.From(() =>
	{
		Visible = false;
		
	}));
}

	private void HideAllPanels()
	{
		_armoryPanel.Visible = false;
		_inventoryPanel.Visible = false;
		_settingsPanel.Visible = false;
	}



	// ===== BUTTON SIGNALS =====

	private void OnArmoryPressed()
	{
		GD.Print("Armory Pressed");
		HideAllPanels();
		_armoryPanel.Visible = true;
	}

	public void OnInventoryPressed()
	{
		GD.Print("Inventory pressed");
		HideAllPanels();
		_inventoryPanel.Visible = true;
	}

	public void OnMenuPressed()
	{
		GD.Print("Menu pressed");
		HideAllPanels();
		_settingsPanel.Visible = true;
	}
	
	public void OnUnpausePressed() {
		GD.Print("Unpaused");
		HideAllPanels();
		CloseMenu();
		
	}
	
	public void OnOptionsPressed(){
		GD.Print("Optoins Menu");
		//
	}
	
	public void OnMainMenuPressed(){
		GD.Print("To main menu!");
		GetTree().ChangeSceneToFile("res://scene/Scenes/MainMenu.tscn");
	}
}
