using System;
using Godot;

public partial class JumpEffect : AnimatedSprite2D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AnimationFinished += OnAnimationFinish;
    }

    void OnAnimationFinish()
    {
        QueueFree();
    }
}
