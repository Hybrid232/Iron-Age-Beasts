using Godot;
using System;

public partial class PauseMenu : Control
{
	[Export] private Control _armoryPanel;
	[Export] private Control _inventoryPanel;
	[Export] private Control _settingsPanel;
	[Export] private Control _pauseBar;

	private bool _menuOpen = false;
	private Player _player;

	public override void _Ready()
	{
		AddToGroup("pause_menu");

		_armoryPanel = GetNode<Control>("PauseBar/Armory/ArmoryBar");
		_inventoryPanel = GetNode<Control>("PauseBar/Inventory/InventoryBar");
		_settingsPanel = GetNode<Control>("PauseBar/Menu/SystemBar");

		_pauseBar = GetNode<Control>("PauseBar");

		// Always process (no pausing in this game style)
		ProcessMode = ProcessModeEnum.Always;

		// 🔥 CRITICAL: do NOT block input when menu is closed
		MouseFilter = MouseFilterEnum.Ignore;

		HideAllPanels();
		Visible = false;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;

		// Button hookups
		GetNode<Button>("PauseBar/Armory").Pressed += OnArmoryPressed;
		GetNode<Button>("PauseBar/Inventory").Pressed += OnInventoryPressed;
		GetNode<Button>("PauseBar/Menu").Pressed += OnMenuPressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Resume").Pressed += OnUnpausePressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Options").Pressed += OnOptionsPressed;
		GetNode<Button>("PauseBar/Menu/SystemBar/Menu").Pressed += OnMainMenuPressed;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("pause"))
		{
			// 🔥 Prevent opening when dead
			if (_player != null && _player.IsDead)
				return;

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

		// 🔥 NOW block input (only while open)
		MouseFilter = MouseFilterEnum.Stop;

		// Release mouse for UI
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Optional: stop player movement
		if (_player != null)
			_player.CanMove = false;

		float screenWidth = GetViewportRect().Size.X;
		float panelWidth = _pauseBar.Size.X;

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

			// 🔥 STOP blocking input when closed
			MouseFilter = MouseFilterEnum.Ignore;

			// Return mouse to gameplay
			Input.MouseMode = Input.MouseModeEnum.Captured;

			// Re-enable movement if alive
			if (_player != null && !_player.IsDead)
				_player.CanMove = true;
		}));
	}

	public void ForceClose()
	{
		_menuOpen = false;
		Visible = false;

		// 🔥 Ensure it never blocks input when forced closed
		MouseFilter = MouseFilterEnum.Ignore;

		Input.MouseMode = Input.MouseModeEnum.Captured;

		if (_player != null && !_player.IsDead)
			_player.CanMove = true;
	}

	private void HideAllPanels()
	{
		_armoryPanel.Visible = false;
		_inventoryPanel.Visible = false;
		_settingsPanel.Visible = false;
	}

	// ===== BUTTONS =====

	private void OnArmoryPressed()
	{
		HideAllPanels();
		_armoryPanel.Visible = true;
	}

	public void OnInventoryPressed()
	{
		HideAllPanels();
		_inventoryPanel.Visible = true;
	}

	public void OnMenuPressed()
	{
		HideAllPanels();
		_settingsPanel.Visible = true;
	}

	public void OnUnpausePressed()
	{
		HideAllPanels();
		CloseMenu();
	}

	public void OnOptionsPressed()
	{
		GD.Print("Options Menu");
	}

	public void OnMainMenuPressed()
	{
		GetTree().ChangeSceneToFile("res://scene/Scenes/MainMenu/MainMenu.tscn");
	}
}
