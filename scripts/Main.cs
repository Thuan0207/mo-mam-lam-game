using System;
using Godot;

public partial class Main : Node
{
    HeartsContainer heartContainer;
    Player player;
    Label enemmiesLeft;
    Label timesLeft;
    GameManager manager;
    Timer timer;
    #region SCENES
    PauseMenu pauseMenu;
    #endregion

    public override void _Ready()
    {
        player = GetNode<Player>("Player");
        heartContainer = GetNode<HeartsContainer>("CanvasLayer/HeartsContainer");
        enemmiesLeft = GetNode<Label>("CanvasLayer/EnemiesLeft");
        timesLeft = GetNode<Label>("CanvasLayer/TimesLeft");
        player.HealthChanged += OnHealthChanged;
        heartContainer.MaxHealth = player.Data.MaxHealth;
        manager = GetNode<GameManager>("/root/GameManager");
        pauseMenu = GetNode<PauseMenu>("CanvasLayer/PauseMenu");
        manager.OnEnemiesCountChange += OnEnemiesCountChange;
        timer = new()
        {
            OneShot = true,
            Autostart = true,
            WaitTime = 120
        };
        timer.Timeout += OnTimeout;

        GetTree().Root.CallDeferred(MethodName.AddChild, timer);
        SetLabelEnemiesCount(manager.EnemiesCount);
        SetLabelTimeLeft();
    }

    private void OnTimeout()
    {
        GetTree().ChangeSceneToFile("res://scenes/TimeoutMenu.tscn");
    }

    private void Win()
    {
        GetTree().ChangeSceneToFile("res://scenes/WinMenu.tscn");
    }

    public override void _Process(double delta)
    {
        SetLabelTimeLeft();
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            pauseMenu.TogglePaused();
        }
        if (manager.EnemiesCount <= 0)
            Win();
    }

    void OnHealthChanged(int currentHealth)
    {
        heartContainer.UpdateHearts(currentHealth);
    }

    void SetLabelEnemiesCount(int count)
    {
        enemmiesLeft.Text = $"Enemies Left: {count}";
    }

    void SetLabelTimeLeft()
    {
        timesLeft.Text = $"Time left: {timer.TimeLeft:F2}";
    }

    void OnEnemiesCountChange(int curr, int prev)
    {
        if (!IsInstanceValid(this))
            return;

        SetLabelEnemiesCount(curr);
        if (curr < prev) { }
        timer.Start(timer.TimeLeft + 5);
    }
}
