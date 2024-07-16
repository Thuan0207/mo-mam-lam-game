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
        }
    }

    [Signal]
    public delegate void OnEnemiesCountChangeEventHandler(int curr, int prev);
}
