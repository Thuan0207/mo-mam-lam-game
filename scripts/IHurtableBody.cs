using Godot;

interface IHurtableBody
{
    public bool Hurt(int _dmg, HitTypes hitTypes = HitTypes.NORMAL_HIT);
}

public enum HitTypes
{
    STRONG_HIT,
    NORMAL_HIT
}
