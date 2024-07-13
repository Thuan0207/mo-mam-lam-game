using System;
using Godot;

public partial class Main : Node
{
    HeartsContainer _heartContainer;
    Player _player;
    Label _enemmiesLeft;
    Label _timesLeft;
    GameManager _manager;

    Timer timer;

    public override void _Ready()
    {
        _player = GetNode<Player>("Player");
        _heartContainer = GetNode<HeartsContainer>("CanvasLayer/HeartsContainer");
        _enemmiesLeft = GetNode<Label>("CanvasLayer/EnemiesLeft");
        _timesLeft = GetNode<Label>("CanvasLayer/TimesLeft");
        _player.HealthChanged += OnHealthChanged;
        _heartContainer.MaxHealth = _player.Data.MaxHealth;
        _manager = GetNode<GameManager>("/root/GameManager");
        _manager.OnEnemiesCountChange += OnEnemiesCountChange;
        timer = new()
        {
            OneShot = true,
            Autostart = true,
            WaitTime = 30
        };
        timer.Timeout += () =>
        {
            GD.Print("You loose");
        };
        GetTree().Root.CallDeferred(MethodName.AddChild, timer);

        SetLabelEnemiesCount(_manager.EnemiesCount);
        SetLabelTimeLeft();
    }

    public override void _Process(double delta)
    {
        SetLabelTimeLeft();
    }

    void OnHealthChanged(int currentHealth)
    {
        _heartContainer.UpdateHearts(currentHealth);
    }

    void SetLabelEnemiesCount(int count)
    {
        _enemmiesLeft.Text = $"Enemies Left: {count}";
    }

    void SetLabelTimeLeft()
    {
        _timesLeft.Text = $"Time left: {timer.TimeLeft:F2}";
    }

    void OnEnemiesCountChange(int curr, int prev)
    {
        SetLabelEnemiesCount(curr);
        if (curr < prev) { }
        timer.Start(timer.TimeLeft + 5);
    }
}
