using Godot;

public partial class GameManager : Node
{
    Timer timer;
    int enemiesCount;

	

    public override void _Ready()
    {
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
    }

    public int EnemiesCount
    {
        get => enemiesCount;
        set
        {
            EmitSignal(SignalName.OnEnemiesCountChange, value, enemiesCount);
            // if (value < enemiesCount)
            // {
            //     Timer.Start(Timer.TimeLeft + 5);
            // }
            enemiesCount = value;
            if (enemiesCount <= 0)
            {
                GD.Print("You win !!!");
            }
        }
    }

    [Signal]
    public delegate void OnEnemiesCountChangeEventHandler(int curr, int prev);
}
