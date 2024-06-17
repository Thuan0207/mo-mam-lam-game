using System;
using Godot;

public partial class ImpactHit : Node2D
{
    [Signal]
    public delegate void OnCriticalFrameEventHandler();
    AnimationPlayer _animationPlayer;

    [Export]
    double _criticalFramePosition;

    bool isSignalEmitted;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _animationPlayer.AnimationFinished += OnAnimationFinished;
        _animationPlayer.Play("Thrust");
        GD.Print("Impact hit get instant: ");
    }

    public override void _Process(double delta)
    {
        // only allow to fire once signal per life cycle
        if (
            !isSignalEmitted
            && Math.Round(_animationPlayer.CurrentAnimationPosition, 3) == _criticalFramePosition
        )
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
            GD.Print("Thrust animation finish");
        }
        GD.Print("Finished animation name: ", animName);
    }
}
