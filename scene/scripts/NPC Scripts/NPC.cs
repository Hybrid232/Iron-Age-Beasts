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
	[Export] private Button _purchaseButton;
	[Export] private Button _exitButton;

	// Shop UI
	[ExportGroup("Shop UI")]
	[Export] private Label _shopDialogue;
	[Export] private Button _buyPotionButton;
	[Export] private Button _upgradeDamageButton;
	[Export] private Button _upgradeHealthButton;
	[Export] private Button _upgradeStaminaButton;
	[Export] private Button _shopBackButton;

	// Prompt shown in the world (press interact)
	[ExportGroup("World Prompt")]
	[Export] private Control _interactPrompt;

	// Portraits
	[ExportGroup("Portraits")]
	[Export] private Control _portraitLayer;
	[Export] private Sprite2D _booginsPortrait;
	[Export] private Sprite2D _playerPortrait;

	[Export] private Texture2D _booginsPortraitTexture;
	[Export] private Texture2D _playerPortraitTexture;

	// Audio
	[ExportGroup("Audio")]
	[Export] private AudioStreamPlayer booginsThemeSFX;
	[Export] private AudioStream booginsThemeFile;

	// Audio Fade
	[ExportGroup("Audio Fade")]
	[Export] private float _themeFadeInSeconds = 0.25f;
	[Export] private float _themeFadeOutSeconds = 1.25f;
	[Export] private float _themeVolumeDb = 0.0f;
	[Export] private float _themeSilentDb = -40.0f;

	private Tween _themeTween;

	// XP source
	[ExportGroup("Economy")]
	[Export] private XPManager _xpManager;

	// Per-item costs
	private enum ShopItem
	{
		Potion,
		UpgradeDamage,
		UpgradeHealth,
		UpgradeStamina
	}

	[ExportGroup("Economy - Item Costs")]
	[Export]
	private Godot.Collections.Dictionary _itemCosts = new()
	{
		{ (int)ShopItem.Potion, 10 },
		{ (int)ShopItem.UpgradeDamage, 25 },
		{ (int)ShopItem.UpgradeHealth, 25 },
		{ (int)ShopItem.UpgradeStamina, 25 },
	};

	[ExportGroup("Economy - Options")]
	[Export] private bool _scaleUpgradeCostsByLevel = true;

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

		if (_xpManager == null)
			_xpManager = GetTree().GetFirstNodeInGroup(XPManager.GROUP_NAME) as XPManager;

		// Hard reset UI
		if (_menu != null) _menu.Visible = false;
		if (_dialoguePanel != null) _dialoguePanel.Visible = false;
		if (_shopPanel != null) _shopPanel.Visible = false;
		if (_interactPrompt != null) _interactPrompt.Visible = false;
		if (_portraitLayer != null) _portraitLayer.Visible = false;

		// Clear text outputs at boot
		if (_dialogueLabel != null) _dialogueLabel.Text = "";
		if (_shopDialogue != null) _shopDialogue.Text = "";

		SetMenuState(MenuState.Dialogue);

		if (_booginsPortrait != null && _booginsPortraitTexture != null)
			_booginsPortrait.Texture = _booginsPortraitTexture;
		if (_playerPortrait != null && _playerPortraitTexture != null)
			_playerPortrait.Texture = _playerPortraitTexture;

		if (booginsThemeSFX != null)
		{
			booginsThemeSFX.Stop();
			booginsThemeSFX.VolumeDb = _themeSilentDb;
		}

		SetProcessInput(false);
	}

	public override void _Input(InputEvent @event)
	{
		if (_activePlayer == null) return;

		if (@event.IsActionPressed("interact"))
		{
			if (_menu == null) return;

			bool opening = !_menu.Visible;
			_menu.Visible = opening;

			_activePlayer.CanMove = !opening;

			if (_dialogueLabel != null) _dialogueLabel.Text = "";
			if (_shopDialogue != null) _shopDialogue.Text = "";

			if (_interactPrompt != null) _interactPrompt.Visible = !_menu.Visible;

			if (opening)
			{
				SetMenuState(MenuState.Dialogue);
				ShowPortraits(true);
				_speakButton?.GrabFocus();
				PlayThemeWithFadeIn();
			}
			else
			{
				ShowPortraits(false);
				FadeOutAndStopTheme();
			}
		}

		if (@event.IsActionPressed("ui_cancel") && _menu != null && _menu.Visible)
			CloseMenu();
	}

	private void CloseMenu()
	{
		if (_menu != null) _menu.Visible = false;
		if (_interactPrompt != null) _interactPrompt.Visible = _activePlayer != null;

		if (_activePlayer != null)
			_activePlayer.CanMove = true;

		SetMenuState(MenuState.Dialogue);
		ShowPortraits(false);
		FadeOutAndStopTheme();
	}

	private void SetMenuState(MenuState newState)
	{
		_state = newState;
		if (_dialoguePanel != null) _dialoguePanel.Visible = _state == MenuState.Dialogue;
		if (_shopPanel != null) _shopPanel.Visible = _state == MenuState.Shop;
	}

	private void ShowPortraits(bool show)
	{
		if (_portraitLayer != null)
			_portraitLayer.Visible = show;
	}

	// ===== Theme audio helpers =====

	private void PlayThemeWithFadeIn()
	{
		if (booginsThemeSFX == null || booginsThemeFile == null) return;

		_themeTween?.Kill();
		_themeTween = null;

		booginsThemeSFX.Stream = booginsThemeFile;

		if (!booginsThemeSFX.Playing)
		{
			booginsThemeSFX.VolumeDb = _themeSilentDb;
			booginsThemeSFX.Play();
		}

		_themeTween = CreateTween();
		_themeTween.TweenProperty(booginsThemeSFX, "volume_db", _themeVolumeDb, _themeFadeInSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void FadeOutAndStopTheme()
	{
		if (booginsThemeSFX == null) return;
		if (!booginsThemeSFX.Playing) return;

		_themeTween?.Kill();
		_themeTween = null;

		_themeTween = CreateTween();
		_themeTween.TweenProperty(booginsThemeSFX, "volume_db", _themeSilentDb, _themeFadeOutSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);

		_themeTween.TweenCallback(Callable.From(() =>
		{
			if (booginsThemeSFX != null)
				booginsThemeSFX.Stop();
		}));
	}

	// ===== Area enter/exit =====

	private void OnAreaBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			_activePlayer = p;
			SetProcessInput(true);

			if (_menu == null || !_menu.Visible)
				if (_interactPrompt != null) _interactPrompt.Visible = true;
		}
	}

	private void OnAreaBodyExited(Node2D body)
	{
		if (body == _activePlayer)
		{
			_activePlayer = null;
			CloseMenu();
			if (_interactPrompt != null) _interactPrompt.Visible = false;
			SetProcessInput(false);
		}
	}

	// ===== Economy helpers =====

	private int GetBaseCost(ShopItem item, int fallbackCost = 0)
	{
		int key = (int)item;
		if (_itemCosts != null && _itemCosts.ContainsKey(key))
			return (int)_itemCosts[key];

		return fallbackCost;
	}

	private int GetEffectiveCost(ShopItem item)
	{
		if (_activePlayer == null) return 0;

		int baseCost = GetBaseCost(item);

		if (!_scaleUpgradeCostsByLevel)
			return baseCost;

		return item switch
		{
			ShopItem.UpgradeDamage => baseCost * _activePlayer.DamageUpgradeLevel,
			ShopItem.UpgradeHealth => baseCost * _activePlayer.HealthUpgradeLevel,
			ShopItem.UpgradeStamina => baseCost * _activePlayer.StaminaUpgradeLevel,
			_ => baseCost,
		};
	}

	private bool TrySpendXp(int cost, string failMessage)
	{
		if (_activePlayer == null || _xpManager == null)
		{
			if (_shopDialogue != null) _shopDialogue.Text = "Shop isn't ready yet.";
			return false;
		}

		if (!_xpManager.TrySpendXp(cost))
		{
			if (_shopDialogue != null) _shopDialogue.Text = failMessage;
			return false;
		}

		return true;
	}

	// ===== Dialogue panel buttons =====

	private void OnSpeakPressed()
	{
		if (_randomDialogues == null || _randomDialogues.Length == 0)
		{
			if (_dialogueLabel != null) _dialogueLabel.Text = "...";
			return;
		}

		int i = _rng.RandiRange(0, _randomDialogues.Length - 1);
		if (_dialogueLabel != null) _dialogueLabel.Text = _randomDialogues[i];
		ShowPortraits(true);
	}

	private void OnPurchasePressed()
	{
		if (_shopDialogue != null) _shopDialogue.Text = "Whatchu want, lil bro?";
		SetMenuState(MenuState.Shop);
		ShowPortraits(true);
		_buyPotionButton?.GrabFocus();
	}

	private void OnExitPressed()
	{
		CloseMenu();
	}

	// ===== Shop panel buttons =====

	private void OnShopBackPressed()
	{
		SetMenuState(MenuState.Dialogue);
		_speakButton?.GrabFocus();

		// Optional: clear shop dialogue when leaving shop
		if (_shopDialogue != null) _shopDialogue.Text = "";
	}

	private void OnBuyPotionPressed()
	{
		if (_activePlayer == null || _xpManager == null)
		{
			if (_shopDialogue != null) _shopDialogue.Text = "Shop isn't ready yet.";
			return;
		}

		if (!_activePlayer.CanBuyPotion())
		{
			if (_shopDialogue != null) _shopDialogue.Text = "You already have 5 potions (max).";
			return;
		}

		int cost = GetEffectiveCost(ShopItem.Potion);

		if (!TrySpendXp(cost, $"Not enough XP. Potion costs {cost} XP."))
			return;

		_activePlayer.TryAddPotionFromShop(1);
		if (_shopDialogue != null) _shopDialogue.Text = $"Potion purchased! (-{cost} XP)";
	}

	private void OnUpgradeDamagePressed()
	{
		if (_activePlayer == null) return;

		int cost = GetEffectiveCost(ShopItem.UpgradeDamage);
		if (!TrySpendXp(cost, $"Not enough XP. Upgrade costs {cost} XP.")) return;

		_activePlayer.UpgradeDamageFromShop();
		if (_shopDialogue != null) _shopDialogue.Text = $"Damage upgraded! (-{cost} XP)";
	}

	private void OnUpgradeHealthPressed()
	{
		if (_activePlayer == null) return;

		int cost = GetEffectiveCost(ShopItem.UpgradeHealth);
		if (!TrySpendXp(cost, $"Not enough XP. Upgrade costs {cost} XP.")) return;

		_activePlayer.UpgradeHealthFromShop();
		if (_shopDialogue != null) _shopDialogue.Text = $"Health upgraded! (-{cost} XP)";
	}

	private void OnUpgradeStaminaPressed()
	{
		if (_activePlayer == null) return;

		int cost = GetEffectiveCost(ShopItem.UpgradeStamina);
		if (!TrySpendXp(cost, $"Not enough XP. Upgrade costs {cost} XP.")) return;

		_activePlayer.UpgradeStaminaFromShop();
		if (_shopDialogue != null) _shopDialogue.Text = $"Stamina upgraded! (-{cost} XP)";
	}
}
