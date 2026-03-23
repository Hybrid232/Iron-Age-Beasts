using Godot;
using System;

public partial class InputDeviceService : Node
{
	public enum DeviceKind
	{
		KeyboardMouse,
		Gamepad
	}

	public DeviceKind LastDevice { get; private set; } = DeviceKind.KeyboardMouse;
	public int LastGamepadId { get; private set; } = 0;

	public event Action<DeviceKind>? DeviceChanged;

	public override void _Ready()
	{
		// Helps ensure this keeps running even if you pause / change scenes.
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Input(InputEvent e)
	{
		// DEBUG: uncomment if you want to see EVERYTHING coming in.
		// GD.Print($"INPUT: {e.GetType().Name} :: {e.AsText()}");

		if (e is InputEventJoypadButton jb)
		{
			GD.Print($"JOY BUTTON: device={jb.Device} button={jb.ButtonIndex} pressed={jb.Pressed}");

			if (jb.Device >= 0)
				LastGamepadId = jb.Device;

			if (LastDevice != DeviceKind.Gamepad)
			{
				LastDevice = DeviceKind.Gamepad;
				DeviceChanged?.Invoke(LastDevice);
			}
			return;
		}

		if (e is InputEventJoypadMotion jm)
		{
			// Only treat "real movement" as gamepad usage to avoid constant switching.
			if (Mathf.Abs(jm.AxisValue) > 0.25f)
			{
				GD.Print($"JOY MOTION: device={jm.Device} axis={jm.Axis} value={jm.AxisValue}");

				if (jm.Device >= 0)
					LastGamepadId = jm.Device;

				if (LastDevice != DeviceKind.Gamepad)
				{
					LastDevice = DeviceKind.Gamepad;
					DeviceChanged?.Invoke(LastDevice);
				}
			}
			return;
		}

		if (e is InputEventKey k && k.Pressed && !k.Echo)
		{
			GD.Print($"KEY: {OS.GetKeycodeString(k.Keycode)}");

			if (LastDevice != DeviceKind.KeyboardMouse)
			{
				LastDevice = DeviceKind.KeyboardMouse;
				DeviceChanged?.Invoke(LastDevice);
			}
			return;
		}

		if (e is InputEventMouseButton mb && mb.Pressed)
		{
			GD.Print($"MOUSE BUTTON: {mb.ButtonIndex}");

			if (LastDevice != DeviceKind.KeyboardMouse)
			{
				LastDevice = DeviceKind.KeyboardMouse;
				DeviceChanged?.Invoke(LastDevice);
			}
			return;
		}

		if (e is InputEventMouseMotion)
		{
			// Optional: comment this out if tiny mouse movement causes unwanted switching.
			if (LastDevice != DeviceKind.KeyboardMouse)
			{
				LastDevice = DeviceKind.KeyboardMouse;
				DeviceChanged?.Invoke(LastDevice);
			}
		}
	}
}
