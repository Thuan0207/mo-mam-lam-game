using System.Collections.Generic;
using Godot;
using MEC;

public partial class Dog : HurtableBody
{
    private float JumpDistanceHeightThreshold = 60.0f;

    [Export]
    public float Speed = 200.0f;

    [Export]
    public float JumpVelocity = -450.0f;

    [Export]
    public float TinyJumpVelocity = -300.0f;

    [Export]
    public float SmallJumpVelocity = -350f;

    [Export]
    int _targetXThreshold = 30;

    [Export]
    int _targetYThreshold = 30;

    [Export]
    float _directionChangedDelay = 1f; // in seconds
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
    Queue<PointInfo> _pathQueue = new();
    PointInfo _target = null;
    PointInfo _prevTarget = null;
    bool _isDirectionChangable = true;

    public Vector2 Direction;

    Player _player;
    Area2D _detectionArea;
    Vector2 _prevPlayerPosition;
    Area2D _targetDetectionArea;
    bool _isTargetReached = false;

    void GoToNextPointInPath()
    {
        if (_pathQueue.Count <= 0)
        {
            _prevTarget = null;
            _target = null;

            _targetDetectionArea.Position = Vector2.Zero;
            return;
        }

        _prevTarget = _target;
        _target = _pathQueue.Dequeue();

        AddTargetDetectionArea(_target.Position);
    }

    private void AddTargetDetectionArea(Vector2 pos)
    {
        _targetDetectionArea.Position = pos;
    }

    private void OnBodyEnteredTargetDetectionArea(Node2D body)
    {
        if (body == this)
        {
            _isTargetReached = true;
            GoToNextPointInPath();
        }
    }

    void PathFinding()
    {
        if (_player != null && IsOnFloor() && _prevPlayerPosition != _player.Position)
        {
            _pathQueue = _tileMapPathFind.GetPlatform2DPath(Position, _player.Position);
            GoToNextPointInPath();
        }
    }

    IEnumerator<double> _PathFindingWithDelay(float duration)
    {
        if (_player != null && IsOnFloor() && _prevPlayerPosition != _player.Position)
        {
            _pathQueue = _tileMapPathFind.GetPlatform2DPath(Position, _player.Position);
            yield return Timing.WaitForSeconds(duration);
            GoToNextPointInPath();
        }
    }
    #endregion

    #region GAME PROCESS
    public override void _EnterTree()
    {
        _targetDetectionArea = new()
        {
            CollisionMask = 4, // can only collide with enemy
            CollisionLayer = 128 // area layer
        };
        CollisionShape2D collisionShape =
            new()
            {
                Shape = new RectangleShape2D() { Size = new(_targetXThreshold, _targetYThreshold) },
            };
        _targetDetectionArea.BodyEntered += OnBodyEnteredTargetDetectionArea;
        _targetDetectionArea.AddChild(collisionShape);

        GetParent().CallDeferred("add_child", _targetDetectionArea);
    }

    public override void _ExitTree()
    {
        GetParent().CallDeferred("remove_child", _targetDetectionArea);
    }

    public override void _Ready()
    {
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite2D.Play("idle");
        _tileMapPathFind = GetParent().GetNode<TileMapPathFind>("TileMap");
        Health = 3;
        _detectionArea = GetNode<Area2D>("DetectionArea");
        _detectionArea.BodyEntered += OnBodyEntered;
        _detectionArea.BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        PathFinding();
        if (Direction.X < 0)
            animatedSprite2D.FlipH = true;
        else
            animatedSprite2D.FlipH = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;

        if (_target != null)
        {
            var distance = _target.Position - Position;
            var currDirection = distance.Normalized();

            // the player have reach the target threshold
            if (_isTargetReached && IsOnFloor())
            {
                // we call jump here because every jumpable point must also be a target point
                Jump(ref velocity);
                _isTargetReached = false;
                _isDirectionChangable = true;
                Timing.KillCoroutines("KeepDirection");
            }

            if (
                Mathf.Abs(distance.X) <= _targetXThreshold // meet x threshold
                && Mathf.Abs(distance.Y) > _targetYThreshold // doesn't meet y threshold
                && Direction != Vector2.Zero
                && _isDirectionChangable
            )
            {
                // // this case happen when character perform a jump and fail
                // if (Position.Y > _target.Position.Y) // if character is below the target
                //     PathFinding();
                if (Position.Y != _target.Position.Y)
                // this help to fix with edge case where the character is a the edge and about to fall down. But its  horizontal position have to go past the target position in order for its collision shape to not in contact with the ground so it can fall down. When this happen, direction is recalculate every physic frame so it will apply a opposite force in order for the character to return to the horizontal threshold. Effectively put the chracter in a stand still loop. This is a fix for that. We delay direction change when we standing above our target
                {
                    Timing.RunCoroutine(
                        _KeepDirection(_directionChangedDelay).CancelWith(this),
                        segment: Segment.PhysicsProcess,
                        tag: "KeepDirection"
                    );
                }
            }

            if (
                _isDirectionChangable
                && Mathf.Abs(distance.X) > _targetXThreshold //doesn't meet target x threshold
            )
                Direction = currDirection;
        }
        else
        {
            Direction = Vector2.Zero;
        }

        if (Direction != Vector2.Zero)
            velocity.X = Mathf.Sign(Direction.X) * Speed;
        else
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);

        if (_player != null)
            _prevPlayerPosition = _player.Position;

        Velocity = velocity;
        MoveAndSlide();
    }

    IEnumerator<double> _KeepDirection(float duration)
    {
        _isDirectionChangable = false;
        yield return Timing.WaitForSeconds(duration);
        _isDirectionChangable = true;
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
            _prevTarget.Position.Y < _target.Position.Y
            && _prevTarget.Position.DistanceTo(_target.Position) < JumpDistanceHeightThreshold
        )
            velocity.Y = SmallJumpVelocity;

        if (
            _prevTarget.Position.Y > _target.Position.Y // can jump up
            || CanJumpDownFromLeftToRightEdge()
            || CanJumpDownFromRightToLeftEdge()
        )
        {
            int heightDistance = Mathf.Abs(
                _tileMapPathFind.LocalToMap(_target.Position).Y
                    - _tileMapPathFind.LocalToMap(_prevTarget.Position).Y
            );
            if (heightDistance <= 2)
                velocity.Y = TinyJumpVelocity;
            else if (heightDistance == 3)
                velocity.Y = SmallJumpVelocity;
            else
                velocity.Y = JumpVelocity;
        }
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
            QueueFree();
        }
    }
    #endregion

    #region EVENT HANDLER
    private void OnBodyExited(Node2D body)
    {
        // we don't want to delete player while the character still chasing it.
        _player = null;
    }

    private void OnBodyEntered(Node2D body)
    {
        _player = (Player)body;
    }

    #endregion
}
