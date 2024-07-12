using Godot;

public partial class Checkmap : Camera2D
{
    [Export] float _offsetAddend = 50.0f;
    [Export] float _duration = 0.2f;
    [Export] Joystick _joystick;
    Vector2 _initialOffset;

    private Vector2 _touchStartPosition;
    private bool _isPanning = false;

    public override void _Ready()
    { _initialOffset = Offset; }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenTouch inputEventTouch)
        {
            if (inputEventTouch.Pressed)
            {
                _touchStartPosition = inputEventTouch.Position;
                _isPanning = true;
            }
            else
            {
                _isPanning = false;
                ResetCameraOffset();
            }
        }
        else if (@event is InputEventScreenDrag inputEventDrag)
        {
            if (_isPanning && !_joystick.IsTouching)
            {
                var dragDistance = inputEventDrag.Position - _touchStartPosition;
                _touchStartPosition = inputEventDrag.Position;
                MoveCamera(dragDistance);
            }
        }
    }
    public override void _Process(double delta)
    {
        if (!_isPanning)
        {
        }

    }
    private void MoveCamera(Vector2 dragDistance)
    {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "offset", new Vector2(_initialOffset.X, _initialOffset.Y + Mathf.Sign(dragDistance.Y) * 50), _duration).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }

    void ResetCameraOffset()
    {
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "offset", _initialOffset, _duration).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
}
