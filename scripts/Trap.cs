using Godot;
using MEC;

public partial class Trap : Node2D
{
    #region NODES
    public Area2D Hitbox;
    public Sprite2D Sprite;
    #endregion

    #region COLLISION VARIABLES
    public Godot.Collections.Array<Node2D> UnhurtableBodies;

    #endregion


    public override void _Ready()
    {
        Hitbox = GetNode<Area2D>("Hitbox");
        Sprite = GetNode<Sprite2D>("Sprite2D");
        UnhurtableBodies = new();
    }

    public override void _Process(double delta)
    {
        HitCheck(Hitbox);
    }

    #region COLLISON METHODS
    public void HitCheck(Area2D hitbox)
    {
        var bodies = hitbox.GetOverlappingBodies();
        foreach (var body in bodies)
            Hit(body);
    }

    public void Hit(Node2D body)
    {
        if (body is IHurtableBody hurtableBody && !UnhurtableBodies.Contains(body))
        {
            if (hurtableBody is Player)
            {
                hurtableBody.Hurt(1);
            }
            else if (hurtableBody.Hurt(1, HitTypes.STRONG_HIT))
            {
                UnhurtableBodies.Add(body);
                Timing.CallDelayed(1, () => UnhurtableBodies.Remove(body));
            }
        }
    }

    #endregion
}
