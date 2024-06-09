using System;
using System.Linq;
using Godot;

public partial class SkeletonSword : HurtableBody
{
    [Export]
    public float Speed = 300.0f;

    [Export]
    public float JumpVelocity = -400.0f;
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    public AnimatedSprite2D animatedSprite2D;

    float _health;
    public override float Health
    {
        get => _health;
        set { _health = value; }
    }

    #region PATH FIND
    TileMapPathFind _tileMapPathFind;
    System.Collections.Generic.Queue<PointInfo> _pathQueue = new();
    PointInfo _target = null;
    PointInfo _prevTarget = null;
    float _jumpDistanceHeightThreshold = 120.0f;

    void GoToNextPointInPath()
    {
        if (_pathQueue.Count <= 0)
        {
            _prevTarget = null;
            _target = null;
            return;
        }
        _prevTarget = _target;
        _target = _pathQueue.Dequeue();
    }

    void PathFinding()
    {
        _pathQueue = _tileMapPathFind.GetPlatform2DPath(Position, GetGlobalMousePosition());
        GoToNextPointInPath();
    }
    #endregion

    #region GAME PROCESS
    public override void _Ready()
    {
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite2D.Play("idle");
        _tileMapPathFind = GetParent().GetNode<TileMapPathFind>("TileMap");
        Health = 3;
    }

    public override void _Process(double delta)
    {
        if (IsOnFloor() && Input.IsActionJustPressed("left_mouse"))
        {
            PathFinding();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        Vector2 direction = Vector2.Zero;

        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;

        if (_target != null)
        {
            if (_target.Position.X - 5 > Position.X)
                direction.X = 1f;
            else if (_target.Position.X + 5 < Position.X)
                direction.X = -1f;
            else if (IsOnFloor())
            {
                GoToNextPointInPath();
                Jump(ref velocity);
            }
        }

        if (direction != Vector2.Zero)
            velocity.X = direction.X * Speed;
        else
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);

        Velocity = velocity;

        // GD.Print("velocity" + velocity);
        MoveAndSlide();
    }

    #endregion

    #region MOVEMENT
    bool CanJumpDownFromRightToLeftEdge()
    {
        return _prevTarget.IsRightEdge
            && _target.IsLeftEdge
            && _prevTarget.Position.Y <= _target.Position.Y // previous target is above the current target
            && _prevTarget.Position.X < _target.Position.X; // previous is to the left of target
    }

    bool CanJumpDownFromLeftToRightEdge()
    {
        return _prevTarget.IsLeftEdge
            && _target.IsRightEdge
            && _prevTarget.Position.Y <= _target.Position.Y // previous target is above the current target
            && _prevTarget.Position.X > _target.Position.X; // previous is to the right of target
    }

    private void Jump(ref Vector2 velocity)
    {
        if (_prevTarget == null || _target == null || _target.IsPositionPoint)
            return;

        // if the target can be drop to reach then we don't need to jump
        if (_prevTarget.Position.Y < _target.Position.Y && _target.IsFallTile)
            return;

        if (
            _prevTarget.Position.Y > _target.Position.Y // can jump up
            || CanJumpDownFromLeftToRightEdge()
            || CanJumpDownFromRightToLeftEdge()
        )
            velocity.Y = JumpVelocity;
    }
    #endregion


    #region CONTACT
    public override void GetHit(float _dmg)
    {
        Health -= _dmg;
        CheckToDie();
        EmitSignal(SignalName.OnGetHit);
    }

    [Signal]
    public delegate void OnGetHitEventHandler();

    void CheckToDie()
    {
        if (Health == 0)
        {
            this.QueueFree();
        }
    }
    #endregion
}
