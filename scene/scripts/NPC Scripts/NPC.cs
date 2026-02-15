using Godot;

public partial class NPC : Node2D
{
    [Export] private Control _menu;
    [Export] private Label _dialogueLabel;
    private bool _canInteract = false;

    public override void _Ready()
    {
        _menu.Visible = false; // Hide menu on start
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_canInteract && Input.IsActionJustPressed("interact"))
        {
            _menu.Visible = !_menu.Visible; // Toggle menu
            _dialogueLabel.Text = ""; // Clear dialogue when opening menu
        }
    }

    private void OnAreaBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
            _canInteract = true;
    }

    private void OnAreaBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            _canInteract = false;
            _menu.Visible = false; // Auto-close menu when walking away
        }
    }

    private void OnSpeakPressed()
    {
        _dialogueLabel.Text = "Hello! I am CHUNKY boogins! I am so FAT";
    }

    private void OnPurchasePressed()
    {
        _dialogueLabel.Text = "I don't have anything to sell yet. Sorry lil bro";
    }
}