using System;
using Godot;

public partial class Explosion : Node2D
{
    CpuParticles2D CpuParticles2D;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        CpuParticles2D = GetNode<CpuParticles2D>("CPUParticles2D");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
		if (Input.IsActionJustPressed("ui_accept"))
            CpuParticles2D.Emitting = true;
    }
}
