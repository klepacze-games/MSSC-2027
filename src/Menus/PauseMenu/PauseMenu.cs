using Godot;

namespace MSSC2027.Menus.PauseMenu;

public partial class PauseMenu : CanvasLayer
{
    private Button _resumeButton;

    public override void _Ready()
    {
        Visible = false;
        _resumeButton = GetNode<VBoxContainer>("ButtonContainer").GetNode<Button>("ResumeButton");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Escape"))
            TogglePause();
    }

    private void OnResumeButtonPressed() => TogglePause();

    private void OnQuitButtonPressed()
    {
        GetTree().Paused = false;
        GetTree().Quit();
    }

    private void TogglePause()
    {
        bool newPausedState = !GetTree().Paused;
        GetTree().Paused = newPausedState;
        Visible = newPausedState;
        if (newPausedState)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _resumeButton.GrabFocus();
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }
}
