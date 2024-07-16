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
        manager.OnEnemiesCountChange += OnEnemiesCountChange;
        pauseMenu = GetNode<PauseMenu>("CanvasLayer/PauseMenu");
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
        // pauseMenu.Size = new();

        SetLabelEnemiesCount(manager.EnemiesCount);
        SetLabelTimeLeft();
    }

    public override void _Process(double delta)
    {
        SetLabelTimeLeft();
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            pauseMenu.TogglePaused();
        }
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
