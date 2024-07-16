using System;
using Godot;

public partial class PauseMenu : Control
{
    #region NODES
    Button resume;
    Button restart;
    Button quit;
    #endregion
    private bool _paused;

    public bool Paused
    {
        get { return _paused; }
        set
        {
            _paused = value;
            if (!_paused)
            {
                Hide();
                Engine.TimeScale = 1;
            }
            else
            {
                Show();
                Engine.TimeScale = 0;
            }
        }
    }

    public override void _Ready()
    {
        Paused = false;
        resume = GetNode<Button>("CenterContainer/VBoxContainer/Resume");
        restart = GetNode<Button>("CenterContainer/VBoxContainer/Restart");
        quit = GetNode<Button>("CenterContainer/VBoxContainer/Quit");
        resume.Pressed += OnResumePressed;
        restart.Pressed += OnRestartPressed;
        quit.Pressed += OnQuitPressed;
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnRestartPressed()
    {
        TogglePaused();
        GetTree().ReloadCurrentScene();
    }

    private void OnResumePressed()
    {
        Paused = false;
    }

    public bool TogglePaused()
    {
        Paused = !Paused;
        return Paused;
    }
}
