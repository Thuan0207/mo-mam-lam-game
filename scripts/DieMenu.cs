using System;
using Godot;

public partial class DieMenu : Control
{
    Button restart;
    Button quit;

    public override void _Ready()
    {
        restart = GetNode<Button>("CenterContainer/VBoxContainer/Restart");
        quit = GetNode<Button>("CenterContainer/VBoxContainer/Quit");

        restart.Pressed += OnRestartPressed;
        quit.Pressed += OnQuitPressed;
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnRestartPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }
}
