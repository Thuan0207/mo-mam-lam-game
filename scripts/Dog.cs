using System.Collections.Generic;
using Godot;
using MEC;

public partial class Dog : CharacterBody2D, IHurtableBody
{
    #region GENERAL
    private float JumpDistanceHeightThreshold = 60.0f;

    [Export]
    public double Health = 3;

    [Export]
    public double Damage = 1;

    [Export]
    public float Speed = 200.0f;

    [Export]
    public float JumpVelocity = -450.0f;

    [Export]
    public float TinyJumpVelocity = -300.0f;

    [Export]
    public float SmallJumpVelocity = -350f;

    [Export]
    public bool IsMovementAllowed = true;

    public Vector2 Direction;

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    public AnimatedSprite2D Sprite;

    Area2D _hitBox;
    #endregion

    #region PATH FIND
    [Export]
    int _targetXThreshold = 30;

    [Export]
    int _targetYThreshold = 30;

    [Export]
    float _directionChangedDelay = 1f; // in seconds
    TileMapPathFind _tileMapPathFind;
    Queue<PointInfo> _pathQueue = new();
    PointInfo _target = null;
    PointInfo _prevTarget = null;
    bool _isDirectionChangable = true;

    Player _player;
    Area2D _detectionArea;
    Area2D _targetDetectionArea;
    Vector2 _prevPlayerPosition;
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
        if (_player != null && IsOnFloor())
        {
            _pathQueue = _tileMapPathFind.GetPlatform2DPath(Position, _player.Position);
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
        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        Sprite.Play("idle");
        _tileMapPathFind = GetParent().GetNode<TileMapPathFind>("TileMap");
        _detectionArea = GetNode<Area2D>("DetectionArea");
        _detectionArea.BodyEntered += OnBodyEnteredDetectionArea;
        _detectionArea.BodyExited += OnBodyExitedDetectionArea;
        _hitBox = GetNode<Area2D>("HitBox");
        _hitBox.BodyEntered += OnBodyEnteredHitBox;
    }

    public override void _Process(double delta)
    {
        if (_player != null && _prevPlayerPosition != _player.Position)
            PathFinding();
        if (Direction.X < 0)
            Sprite.FlipH = true;
        else
            Sprite.FlipH = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        if (_player != null)
            _prevPlayerPosition = _player.Position;

        if (IsMovementAllowed)
            MoveToTargetLocation(ref velocity);

        Velocity = velocity;
        MoveAndSlide();
    }

    #endregion


    #region MOVEMENT
    IEnumerator<double> _KeepDirection(float duration)
    {
        _isDirectionChangable = false;
        yield return Timing.WaitForSeconds(duration);
        _isDirectionChangable = true;
    }

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

    void MoveToTargetLocation(ref Vector2 velocity)
    {
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
    }

    #endregion

    #region COMBAT
    [Export(PropertyHint.Range, "0,100,0.1,,or_greater")]
    int _bounceBackOffsetX = 1;

    [Export(PropertyHint.Range, "0,100,0.1,,or_greater")]
    float _bounceBackOffsetY = 10;

    [Export]
    float _bounceBackDuration = 1;

    [Export]
    float _stunDurationAfterBounceBack = 1;

    private void OnBodyEnteredHitBox(Node2D body)
    {
        if (body is Player player)
        {
            player.GetHit((float)Damage);
        }
    }

    void BounceBack(bool isBounceRight, float offsetX = 1, float offsetY = 0, float duration = 1)
    {
        IsMovementAllowed = false;
        var tween = GetTree().CreateTween();
        var directionX = isBounceRight ? 1 : -1;
        float directionY = 0;

        var targetPosition =
            GlobalPosition + new Vector2(directionX * offsetX, directionY * offsetY);
        var distance = GlobalPosition.DistanceTo(targetPosition);
        var accel = distance / duration;
        var targetVelocity = new Vector2(accel * directionX, accel * directionY);

        tween
            .TweenProperty(this, "velocity", targetVelocity, duration)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.InOut);
        tween
            .TweenCallback(
                Callable.From(() =>
                {
                    Velocity = Vector2.Zero;
                })
            )
            .SetDelay(duration);
        tween
            .TweenCallback(
                Callable.From(() =>
                {
                    IsMovementAllowed = true;
                })
            )
            .SetDelay(duration + _stunDurationAfterBounceBack);
    }

    #endregion

    #region CONTACT
    public void GetHit(float _dmg)
    {
        Health -= _dmg;
        CheckToDie();

        BounceBack(
            _player.IsFacingRight,
            _bounceBackOffsetX,
            _bounceBackOffsetY,
            _bounceBackDuration
        );
    }

    void CheckToDie()
    {
        if (Health == 0)
        {
            QueueFree();
        }
    }
    #endregion

    #region EVENT HANDLER
    private void OnBodyExitedDetectionArea(Node2D body)
    {
        // we don't want to delete player while the character still chasing it.
        _player = null;
    }

    private void OnBodyEnteredDetectionArea(Node2D body)
    {
        _player = (Player)body;
        PathFinding();
    }

    #endregion
}
