using Godot;

[Tool]
public partial class AnimationData : Resource
{
    [Export]
    Vector2 _neutralStretchScaleAddend;
    public Vector2 NeutralStretchScaleAddend
    {
        get => _neutralStretchScaleAddend;
        set { _neutralStretchScaleAddend = new(value.X * -1, value.Y); }
    }

    [Export]
    Vector2 _neutralSquashScaleAddend;

    public Vector2 NeutralSquashScaleAddend
    {
        get => _neutralSquashScaleAddend;
        set { _neutralSquashScaleAddend = new(value.X, value.Y * -1); }
    }

    [Export]
    Vector2 _forwardStretchScaleAddend;
    public Vector2 ForwardStretchScaleAddend
    {
        get => _forwardStretchScaleAddend;
        set { _forwardStretchScaleAddend = new(value.X * -1, value.Y); }
    }

    [Export]
    Vector2 _forwardSquashScaleAddend;

    public Vector2 ForwardSquashScaleAddend
    {
        get => _forwardSquashScaleAddend;
        set { _forwardSquashScaleAddend = new(value.X, value.Y * -1); }
    }

    public AnimationData()
        : this(
            new Vector2(0, 0.1f),
            new Vector2(0.1f, 0.1f),
            new Vector2(0, 0.115f),
            new Vector2(0.115f, 0)
        ) { }

    public AnimationData(
        Vector2 neutralStretchScaleAddend,
        Vector2 neutralSquashScaleAddend,
        Vector2 forwardStretchScaleAddend,
        Vector2 forwardSquashScaleAddend
    )
    {
        NeutralStretchScaleAddend = neutralStretchScaleAddend;
        NeutralSquashScaleAddend = neutralSquashScaleAddend;
        ForwardSquashScaleAddend = forwardSquashScaleAddend;
        ForwardStretchScaleAddend = forwardStretchScaleAddend;
    }
}
