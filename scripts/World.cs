using System;
using Godot;

public partial class World : Node2D
{
    // Called when the node enters the scene tree for the first time.
    [ExportGroup("Nodes")]
    [Export]
    HeartsContainer _heartContainer;

    [Export]
    Player _player;

    public override void _Ready()
    {
        _heartContainer.MaxHealth = _player.Data.MaxHealth;
        _player.HealthChanged += OnHealthChanged;
    }

    void OnHealthChanged(int currentHealth)
    {
        _heartContainer.UpdateHearts(currentHealth);
    }
}
