using Godot;
using System;

public partial class PlayerRunData : Resource
{
    [Export] public float Speed {get;set;} 
    [Export] public float JumpVelocity{get;set;}

    public PlayerRunData (): this(250.0f, -450.0f){}

    public PlayerRunData(float speed, float jumpVelocity) {
        Speed = speed;
        JumpVelocity = jumpVelocity;
    }

}
