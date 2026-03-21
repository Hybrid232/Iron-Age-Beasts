using Godot;

public partial class NPC : Node2D
{
	[Export] private Control _menu;

	// Panels
	[ExportGroup("Panels")]
	[Export] private Control _dialoguePanel;
	[Export] private Control _shopPanel;

	// Dialogue UI
	[ExportGroup("Dialogue UI")]
	[Export] private Label _dialogueLabel;
	[Export] private Button _speakButton;
	[Export] private Button _purchaseButton;   // opens shop
	[Export] private Button _exitButton;       // optional: closes menu

	// Shop UI
	[ExportGroup("Shop UI")]
	[Export] private Button _buyPotionButton;
	[Export] private Button _upgradeDamageButton;
	[Export] private Button _upgradeHealthButton;
	[Export] private Button _upgradeStaminaButton;
	[Export] private Button _shopBackButton;   // returns to dialogue

	// Prompt shown in the world (press interact)
	[ExportGroup("World Prompt")]
	[Export] private Control _interactPrompt;

	// Portraits
	[ExportGroup("Portraits")]
	[Export] private Control _portraitLayer;          // parent container
	[Export] private TextureRect _booginsPortrait;    // or Sprite2D if you prefer
	[Export] private TextureRect _playerPortrait;

	// Optional: set these to swap portrait textures at runtime
	[Export] private Texture2D _booginsPortraitTexture;
	[Export] private Texture2D _playerPortraitTexture;

	// Audio
	[ExportGroup("Audio")]
	[Export] private AudioStreamPlayer booginsThemeSFX;
	[Export] private AudioStream booginsThemeFile;

	// XP source
	[ExportGroup("Economy")]
	[Export] private XPManager _xpManager;
	[Export] private int potionCostXp = 10;
	[Export] private int upgradeBaseCostXp = 25;

	[ExportGroup("Dialogue Text")]
	[Export(PropertyHint.MultilineText)]
	private string[] _randomDialogues =
	{
		"Hello! I am CHUNKY boogins!",
		"I am so FAT, but I love you buddy!",
		"Pick an upgrade, lil bro.",
		"Potions max at 5. Don’t be greedy.",
		"Spend that XP. I know you got it."
	};

	private readonly RandomNumberGenerator _rng = new();
	private Player _activePlayer;

	private enum MenuState { Dialogue, Shop }
	private MenuState _state = MenuState.Dialogue;

	public override void _Ready()
	{
		_rng.Randomize();

		// Basic validation
		if (_menu == null || _dialogueLabel == null || _interactPrompt == null)
			GD.PrintErr("NPC Error: Missing required exported nodes on NPC.");

		// Find XPManager if not assigned
		if (_xpManager == null)
			_xpManager = GetTree().GetFirstNodeInGroup(XPManager.GROUP_NAME) as XPManager;

		// Init visibility
		if (_menu != null) _menu.Visible = false;
		if (_interactPrompt != null) _interactPrompt.Visible = false;

		// Panels default
		SetMenuState(MenuState.Dialogue);

		// Portrait setup
		if (_portraitLayer != null) _portraitLayer.Visible = false;
		if (_booginsPortrait != null && _booginsPortraitTexture != null) _booginsPortrait.Texture = _booginsPortraitTexture;
		if (_playerPortrait != null && _playerPortraitTexture != null) _playerPortrait.Texture = _playerPortraitTexture;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("interact") && _activePlayer != null)
		{
			if (_menu == null) return;

			bool opening = !_menu.Visible;
			_menu.Visible = opening;

			_activePlayer.CanMove = !opening ? true : false;
			_dialogueLabel.Text = "";
			_interactPrompt.Visible = !_menu.Visible;

			if (opening)
			{
				SetMenuState(MenuState.Dialogue);
				ShowPortraits(true);

				_speakButton?.GrabFocus();

				if (booginsThemeSFX != null && booginsThemeFile != null)
				{
					booginsThemeSFX.Stream = booginsThemeFile;
					booginsThemeSFX.Play();
				}
			}
			else
			{
				// closing
				ShowPortraits(false);
			}
		}

		// Optional: ESC closes menu if open
		if (@event.IsActionPressed("ui_cancel") && _menu != null && _menu.Visible)
		{
			CloseMenu();
		}
	}

	private void CloseMenu()
	{
		if (_menu != null) _menu.Visible = false;
		if (_interactPrompt != null) _interactPrompt.Visible = _activePlayer != null;

		if (_activePlayer != null)
			_activePlayer.CanMove = true;

		SetMenuState(MenuState.Dialogue);
		ShowPortraits(false);
	}

	private void SetMenuState(MenuState newState)
	{
		_state = newState;

		if (_dialoguePanel != null) _dialoguePanel.Visible = _state == MenuState.Dialogue;
		if (_shopPanel != null) _shopPanel.Visible = _state == MenuState.Shop;

		// You can also change which portrait is emphasized here if you want:
		// e.g. dim player portrait when boogins is talking, etc.
	}

	private void ShowPortraits(bool show)
	{
		if (_portraitLayer != null)
			_portraitLayer.Visible = show;
	}

	private void OnAreaBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
			if (_menu == null || !_menu.Visible)
				_interactPrompt.Visible = true;
		}
	}

	private void OnAreaBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			_activePlayer = null;
			CloseMenu();
			if (_interactPrompt != null) _interactPrompt.Visible = false;
		}
	}

	// ========== Dialogue panel buttons ==========

	private void OnSpeakPressed()
	{
		if (_randomDialogues == null || _randomDialogues.Length == 0)
		{
			_dialogueLabel.Text = "...";
			return;
		}

		int i = _rng.RandiRange(0, _randomDialogues.Length - 1);
		_dialogueLabel.Text = _randomDialogues[i];

		// Fire Emblem feel: show portraits during dialogue
		ShowPortraits(true);
	}

	private void OnPurchasePressed()
	{
		_dialogueLabel.Text = "Whatchu want, lil bro?";
		SetMenuState(MenuState.Shop);
		ShowPortraits(true);

		_buyPotionButton?.GrabFocus();
	}

	private void OnExitPressed()
	{
		CloseMenu();
	}

	// ========== Shop panel buttons ==========

	private void OnShopBackPressed()
	{
		SetMenuState(MenuState.Dialogue);
		_speakButton?.GrabFocus();
	}

	private void OnBuyPotionPressed()
	{
		if (_activePlayer == null || _xpManager == null)
		{
			_dialogueLabel.Text = "Shop isn't ready yet.";
			return;
		}

		if (!_activePlayer.CanBuyPotion())
		{
			_dialogueLabel.Text = "You already have 5 potions (max).";
			return;
		}

		if (!_xpManager.TrySpendXp(potionCostXp))
		{
			_dialogueLabel.Text = $"Not enough XP. Potion costs {potionCostXp} XP.";
			return;
		}

		_activePlayer.TryAddPotionFromShop(1);
		_dialogueLabel.Text = $"Potion purchased! (-{potionCostXp} XP)";
	}

	private int GetDamageUpgradeCost() => upgradeBaseCostXp * _activePlayer.DamageUpgradeLevel;
	private int GetHealthUpgradeCost() => upgradeBaseCostXp * _activePlayer.HealthUpgradeLevel;
	private int GetStaminaUpgradeCost() => upgradeBaseCostXp * _activePlayer.StaminaUpgradeLevel;

	private bool TrySpendForUpgrade(int cost)
	{
		if (_activePlayer == null || _xpManager == null)
		{
			_dialogueLabel.Text = "Shop isn't ready yet.";
			return false;
		}

		if (!_xpManager.TrySpendXp(cost))
		{
			_dialogueLabel.Text = $"Not enough XP. Upgrade costs {cost} XP.";
			return false;
		}

		return true;
	}

	private void OnUpgradeDamagePressed()
	{
		if (_activePlayer == null) return;

		int cost = GetDamageUpgradeCost();
		if (!TrySpendForUpgrade(cost)) return;

		_activePlayer.UpgradeDamageFromShop();
		_dialogueLabel.Text = $"Damage upgraded! (-{cost} XP)";
	}

	private void OnUpgradeHealthPressed()
	{
		if (_activePlayer == null) return;

		int cost = GetHealthUpgradeCost();
		if (!TrySpendForUpgrade(cost)) return;

		_activePlayer.UpgradeHealthFromShop();
		_dialogueLabel.Text = $"Health upgraded! (-{cost} XP)";
	}

	private void OnUpgradeStaminaPressed()
	{
		if (_activePlayer == null) return;

		int cost = GetStaminaUpgradeCost();
		if (!TrySpendForUpgrade(cost)) return;

		_activePlayer.UpgradeStaminaFromShop();
		_dialogueLabel.Text = $"Stamina upgraded! (-{cost} XP)";
	}
}
