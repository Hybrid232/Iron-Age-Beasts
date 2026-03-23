using Godot;

public partial class InteractPrompt : Control
{
	[Export] public string ActionName = "interact";
	[Export] public string VerbText = "Interact";

	private Label _label = null!;
	private InputDeviceService _device = null!;

	public override void _Ready()
	{
		// Change this path if your label is not named "Label"
		// If you use a Unique Name in Scene, set the label's Unique Name and keep "%Label".
		_label = GetNode<Label>("%Label");

		_device = GetNode<InputDeviceService>("/root/scene/scripts/Input/InputDeviceService");
		_device.DeviceChanged += OnDeviceChanged;

		Refresh();
	}

	public override void _ExitTree()
	{
		if (_device != null)
			_device.DeviceChanged -= OnDeviceChanged;
	}

	private void OnDeviceChanged(InputDeviceService.DeviceKind _)
	{
		Refresh();
	}

	private void Refresh()
	{
		GD.Print($"InteractPrompt Refresh: last_device={_device.LastDevice} pad={_device.LastGamepadId} joyname='{Input.GetJoyName(_device.LastGamepadId)}'");

		if (_device.LastDevice == InputDeviceService.DeviceKind.KeyboardMouse)
		{
			var key = InputPromptUtil.GetKeyboardLabelForAction(ActionName);
			_label.Text = $"[{key}] to {VerbText}";
		}
		else
		{
			var btn = InputPromptUtil.GetGamepadLabelForAction(ActionName, _device.LastGamepadId);
			_label.Text = $"{btn} to {VerbText}";
		}
	}
}
