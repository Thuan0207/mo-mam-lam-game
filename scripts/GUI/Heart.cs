using Godot;

public partial class Heart : Panel
{
    // Called when the node enters the scene tree for the first time.
    [ExportGroup("Nodes")]
    [Export]
    Sprite2D _sprite;

    [Export]
    AnimationPlayer _animationPlayer;
    bool _isBroken = false;
    public bool IsBroken
    {
        get => _isBroken;
        set
        {
            if (!_isBroken && value) // only play broken animation if the heart is not already broken
                _animationPlayer.Play("Broken");
            else if (_isBroken)
                _sprite.Frame = 5;
            else
                _sprite.Frame = 0;

            _isBroken = value;
        }
    }
}
