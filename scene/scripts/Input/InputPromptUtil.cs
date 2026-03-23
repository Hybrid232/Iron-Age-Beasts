using Godot;

public static class InputPromptUtil
{
	public static string GetKeyboardLabelForAction(string actionName)
	{
		var events = InputMap.ActionGetEvents(actionName);
		foreach (var ev in events)
		{
			if (ev is InputEventKey k)
				return OS.GetKeycodeString(k.Keycode);
		}
		return "?";
	}

	public static string GetGamepadLabelForAction(string actionName, int deviceId)
	{
		var events = InputMap.ActionGetEvents(actionName);
		foreach (var ev in events)
		{
			if (ev is InputEventJoypadButton jb)
			{
				var btn = (JoyButton)jb.ButtonIndex;
				return IsPlayStationPad(deviceId) ? PsName(btn) : XboxName(btn);
			}
		}
		return "?";
	}

	public static bool IsPlayStationPad(int deviceId)
	{
		var n = Input.GetJoyName(deviceId).ToLower();
		return n.Contains("dualsense") || n.Contains("dualshock") || n.Contains("playstation");
	}

	private static string PsName(JoyButton btn) => btn switch
	{
		JoyButton.Y => "Triangle",
		JoyButton.B => "Circle",
		JoyButton.A => "Cross",
		JoyButton.X => "Square",
		JoyButton.LeftShoulder => "L1",
		JoyButton.RightShoulder => "R1",
		JoyButton.Back => "Create",
		JoyButton.Start => "Options",
		_ => btn.ToString()
	};

	private static string XboxName(JoyButton btn) => btn switch
	{
		JoyButton.Y => "Y",
		JoyButton.B => "B",
		JoyButton.A => "A",
		JoyButton.X => "X",
		JoyButton.LeftShoulder => "LB",
		JoyButton.RightShoulder => "RB",
		JoyButton.Back => "View",
		JoyButton.Start => "Menu",
		_ => btn.ToString()
	};
}
