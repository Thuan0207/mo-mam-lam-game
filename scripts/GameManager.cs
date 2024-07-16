using Godot;

public partial class GameManager : Node
{
    int _enemiesCount;

    public int EnemiesCount
    {
        get => _enemiesCount;
        set
        {
            EmitSignal(SignalName.OnEnemiesCountChange, value, _enemiesCount);
            _enemiesCount = value;

            if (_enemiesCount <= 0)
                EmitSignal(SignalName.OnWin);
        }
    }

    [Signal]
    public delegate void OnEnemiesCountChangeEventHandler(int curr, int prev);

    [Signal]
    public delegate void OnWinEventHandler();
}
