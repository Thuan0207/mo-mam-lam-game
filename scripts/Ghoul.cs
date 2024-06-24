using System.Collections.Generic;
using Godot;
using MEC;

public partial class Ghoul : CharacterBody2D, IHurtableBody
{
    #region GENERAL
    const float JUMP_HEIGHT_THRESHOLD = 60.0f;

    [Export]
    public CharacterData Data;

    [Export]
    public double Health = 3;

    [Export]
    public int Damage = 1;

    [Export]
    public float Speed = 200.0f;

    [Export]
    public float RunLerp = 0.75f;

    [Export]
    public float JumpVelocity = -450.0f;

    [Export]
    public float TinyJumpVelocity = -300.0f;

    [Export]
    const float SmallJumpVelocity = -350f;

    [Export]
    public bool IsMovementAllowed = true;

    public Vector2 Direction;

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    AnimatedSprite2D _animatedSprite;

    Area2D _hitBox;
    #endregion

    #region PATH FIND
    [Export]
    int _targetXThreshold = 30;

    [Export]
    int _targetYThreshold = 30;

    [Export]
    TileMapPathFind _tileMapPathFind;
    Queue<PointInfo> _pathQueue = new();
#nullable enable
    PointInfo? _target = null;
    PointInfo? _prevTarget = null;
    Player? _player;
#nullable disable
    Area2D _detectionArea;
    Area2D _targetDetectionArea;

    void GoToNextPointInPath()
    {
        if (_pathQueue.Count <= 0)
        {
            _prevTarget = null;
            _target = null;

            _targetDetectionArea.GlobalPosition = Vector2.Zero;
            return;
        }

        _prevTarget = _target;
        _target = _pathQueue.Dequeue();

        var distance = _target.Position - GlobalPosition;

        // if the we are within the threshold of current target we skip to the next one when possible
        if (
            _pathQueue.Count > 0
            && Mathf.Abs(distance.X) <= _targetXThreshold
            && Mathf.Abs(distance.Y) <= _targetYThreshold
        ) // Enter target threshold
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
            // GoToNextPointInPath();
        }
    }

    void PathFinding()
    {
        if (_player != null && IsOnFloor() && Health > 0)
        {
            _pathQueue = _tileMapPathFind.GetPlatform2DPath(GlobalPosition, _player.GlobalPosition);
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
        #region NODES ASSIGNMENTS
        _hitBox = GetNode<Area2D>("HitBox");
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _detectionArea = GetNode<Area2D>("DetectionArea");
        #endregion

        #region EVENTS ASSIGNMENTS
        _detectionArea.BodyEntered += OnBodyEnteredDetectionArea;
        _detectionArea.BodyExited += OnBodyExitedDetectionArea;
        _hitBox.BodyEntered += OnBodyEnteredHitBox;
        _animatedSprite.AnimationFinished += OnAnimationFinished;
        #endregion
    }

    public override void _Process(double delta)
    {
        PathFinding();

        if (Direction.X < 0)
            _animatedSprite.FlipH = true;
        else
            _animatedSprite.FlipH = false;

        if (Mathf.Round(Velocity.X) == 0)
            _animatedSprite.Play("idle");
        else
            _animatedSprite.Play("running");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        if (IsMovementAllowed)
            MoveToTargetLocation(ref velocity, delta);

        Velocity = velocity;
        MoveAndSlide();
    }
    #endregion


    #region MOVEMENT

    #region jump
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
        if (_target == null || _target.IsPositionPoint)
            return;

        // if the target can be drop to reach then we don't need to jump
        if (GlobalPosition.Y < _target.Position.Y && _target.IsFallTile)
            return;

        if (
            (
                GlobalPosition.Y < _target.Position.Y
                && GlobalPosition.DistanceTo(_target.Position) < JUMP_HEIGHT_THRESHOLD
            )
        )
            velocity.Y = -Mathf.Sqrt(2 * 4 * _tileMapPathFind.TileSet.TileSize.Y * Gravity);

        if (
            GlobalPosition.Y > _target.Position.Y // can jump up
            || CanJumpDownFromLeftToRightEdge()
            || CanJumpDownFromRightToLeftEdge()
        )
        {
            int heightDistance = Mathf.Abs(
                _tileMapPathFind.LocalToMap(_target.Position).Y
                    - _tileMapPathFind.LocalToMap(GlobalPosition).Y
            );

            if (heightDistance <= 2)
                velocity.Y = Mathf.Sqrt(2 * 4 * _tileMapPathFind.TileSet.TileSize.Y * Gravity);
            else if (heightDistance == 3)
                velocity.Y = Mathf.Sqrt(2 * 6 * _tileMapPathFind.TileSet.TileSize.Y * Gravity);
            else
                velocity.Y = Mathf.Sqrt(2 * 8 * _tileMapPathFind.TileSet.TileSize.Y * Gravity);

            velocity.Y = -1 * velocity.Y;
        }
    }
    #endregion

    void MoveToTargetLocation(ref Vector2 velocity, double delta)
    {
        if (_target != null)
        {
            var distance = _target.Position - Position;

            // the player have reach the target threshold
            if (
                Mathf.Abs(distance.X) <= _targetXThreshold // meet x threshold
                && Mathf.Abs(distance.Y) <= _targetYThreshold //  meet y threshold
                && IsOnFloor()
            )
            {
                // we call jump here because every jumpable point must also be a target point

                Jump(ref velocity);
                GoToNextPointInPath();

                if (_target != null)
                    distance = _target.Position - Position;
            }

            if (
                Mathf.Abs(distance.X) > _targetXThreshold //doesn't meet target x threshold
            )
                Direction = distance.Normalized();
        }
        else
            Direction = Vector2.Zero;

        velocity.X = Mathf.Lerp(Velocity.X, Mathf.Sign(Direction.X) * Speed, RunLerp);

        GD.Print("Velocity: ", Velocity);
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
        if (body is Player player && Health > 0)
        {
            player.GetHit(Damage);

            BounceBack(
                _player.IsFacingRight,
                _bounceBackOffsetX,
                _bounceBackOffsetY,
                _bounceBackDuration
            );
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
            .SetDelay(_stunDurationAfterBounceBack);
    }
    #endregion

    #region CONTACT
    public void GetHit(int _dmg)
    {
        if (Health == 0)
            return;

        Health -= _dmg;

        if (Health == 0)
        {
            _animatedSprite.Play("die");
            SetProcess(false);
        }

        BounceBack(
            _player.IsFacingRight,
            _bounceBackOffsetX,
            _bounceBackOffsetY,
            _bounceBackDuration
        );
    }

    #endregion

    #region EVENT HANDLER
    private void OnBodyExitedDetectionArea(Node2D body)
    {
        // we don't want to delete player while the character still chasing it.
        // _player = null;
    }

    private void OnBodyEnteredDetectionArea(Node2D body)
    {
        if (body is Player player)
            _player = player;
    }

    void OnAnimationFinished()
    {
        if (_animatedSprite.Animation == "die")
        {
            var tween = GetTree().CreateTween();
            tween
                .TweenProperty(_animatedSprite, "modulate:a", 0, 1)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(this.QueueFree)).SetDelay(1);
        }
    }

    #endregion
}
