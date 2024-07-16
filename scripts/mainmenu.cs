using System;
using Godot;

public partial class mainmenu : Control
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Find the buttons and connect their pressed signal to methods
        Button startButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/StartGameButton"
        );
        Button quitButton = GetNode<Button>(
            "MarginContainer/VBoxContainer/HBoxContainer/VBoxContainer/QuitGameButton"
        );

        // Connect the buttons to methods using Callable
        startButton.Connect("pressed", new Callable(this, nameof(OnStartButtonPressed)));
        quitButton.Connect("pressed", new Callable(this, nameof(OnQuitButtonPressed)));
    }

    // Method to handle Start button press
    private void OnStartButtonPressed()
    {
        // Transition to your game scene
        GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
    }

    // Method to handle Quit button press
    private void OnQuitButtonPressed()
    {
        // Quit the game
        GetTree().Quit();
    }
}
