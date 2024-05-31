using System;
using System.Reflection;
using Godot;

public partial class DashGhost : Sprite2D
{
    // Called when the node enters the scene tree for the first time.
    [Export]
    public float duration = 0.5f;
    ColorRect colorRect;

    public override void _Ready()
    {
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0.3f);
        // colorRect = GetNode<ColorRect>("ColorRect");
        FadeOut();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    void FadeOut()
    {
        Tween tween = GetTree().CreateTween();
        tween
            .TweenProperty(this, "modulate:a", 0.0f, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(this.QueueFree));
    }
}
