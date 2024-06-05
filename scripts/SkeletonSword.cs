using Godot;
using static Godot.GD;

public partial class SkeletonSword : HurtableBody
{
    public override void _Ready()
    {
        Health = 3;
        OnGetHit += CheckToDie;
    }

    public override void _Process(double delta)
    {
        Print("Health: ", Health);
    }

    public override void GetHit(float _dmg)
    {
        Health -= _dmg;
        EmitSignal(SignalName.OnGetHit);
    }

    [Signal]
    public delegate void OnGetHitEventHandler();

    void CheckToDie()
    {
        if (Health == 0)
        {
            this.QueueFree();
        }
    }
}
