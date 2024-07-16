using Godot;

public partial class PauseButton : Button
{
    // Called when the node enters the scene tree for the first time.
    [Export]
    PauseMenu pauseMenu;

    public override void _Ready()
    {
        Pressed += () =>
        {
            pauseMenu.TogglePaused();
        };
    }
}
