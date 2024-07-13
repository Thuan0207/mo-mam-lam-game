using Godot;
using MEC;

public partial class AnimatedTrap : Trap
{
    #region VARIABLES
    [Export]
    float delay;
    #endregion
    #region NODES
    AnimationPlayer animater;
    #endregion


    public override void _Ready()
    {
        Hitbox = GetNode<Area2D>("Hitbox");
        Sprite = GetNode<Sprite2D>("Sprite2D");
        UnhurtableBodies = new();

        animater = GetNode<AnimationPlayer>("AnimationPlayer");

        Timing.CallDelayed(delay, () => animater.Play("attack"));
    }

    public override void _Process(double delta)
    {
        HitCheck(Hitbox);
        EmptyUnhurtablesCheck();
    }

    #region COLLISON METHODS
    private void EmptyUnhurtablesCheck()
    {
        int count = 1;

        if (Sprite.Hframes > 1)
        {
            count = Sprite.Hframes;
        }
        else if (Sprite.Vframes > 1)
            count = Sprite.Vframes;

        if (Sprite.Frame == count - 1)
        {
            UnhurtableBodies.Clear();
        }
    }
    #endregion
}
