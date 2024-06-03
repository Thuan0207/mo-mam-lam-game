using System;
using Godot;

public partial class SkeletonSword : Node2D
{
    // Called when the node enters the scene tree for the first time.
    float health;

    public override void _Ready()
    {
        health = 3;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GD.Print("monster health " + health);
    }

    public void GetHurt(float _dmg)
    {
        health -= _dmg;
    }
}
