using Godot;

public abstract partial class HurtableBody : CharacterBody2D
{
    // Called when the node enters the scene tree for the first time.
    public float Health;
    public abstract void GetHit(float _dmg);
}
