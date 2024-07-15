/*using System;
using Godot;

public partial class World : Node2D
{
    // Called when the node enters the scene tree for the first time.
    [ExportGroup("Nodes")]
    
    [Export]
    HeartsContainer _heartContainer;

    [Export]
    Player _player;
    private Control pause_menu;
    private bool paused = false;

    public override void _Ready()
    {
        _heartContainer.MaxHealth = _player.Data.MaxHealth;
        _player.HealthChanged += OnHealthChanged;
        //pause_menu = GetNode<Control>("PhantomCamera2D/UIpause");
        //pause_menu.Hide();
    }

    void OnHealthChanged(int currentHealth)
    {
        _heartContainer.UpdateHearts(currentHealth);
    }

    /*public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            PauseMenu();
        }
    }

    private void PauseMenu()
    {
        if (paused)
        {
            pause_menu.Hide();
            Engine.TimeScale = 1;
        }
        else
        {
            pause_menu.Show();
            Engine.TimeScale = 0;
        }
        paused = !paused;
    }
*/

