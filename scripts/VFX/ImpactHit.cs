using Godot;

public partial class ImpactHit : Node2D
{
    [Signal]
    public delegate void OnCriticalFrameEventHandler();
    AnimationPlayer _animationPlayer;

    [Export]
    Sprite2D _thrustSprite;

    bool isSignalEmitted;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
        _animationPlayer.Play("Thrust");
    }

    public override void _Process(double delta)
    {
        // only allow to fire once signal per life cycle
        if (!isSignalEmitted && _thrustSprite.Frame == 1)
        {
            EmitSignal(SignalName.OnCriticalFrame);
            isSignalEmitted = true;
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Thrust")
        {
            QueueFree();
        }
    }
}
