using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MEC;

public partial class Ghoul : CharacterBody2D, IHurtableBody
{
    #region GENERAL
    const float JUMP_HEIGHT_THRESHOLD = 60.0f;

    [Export]
    GhoulData _data;

    bool _isVelocityOverrided;

    public Vector2 Direction;

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    AnimatedSprite2D _animatedSprite;

    Area2D _bodyHitBox;

    Area2D _hitBoxRight;

    Area2D _hitBoxLeft;
    GameManager _gameManager;

    bool _isTargetWithinAtkRange;
    #endregion

    #region COMBAT
    bool _isAtkRefilling;
    bool _isRecoiling;
    const string STOP_X_MOVEMENT_TAG = "STOP_X_MOVEMENT_TAG";
    const string ATTACK_TAG = "ATTACK_TAG";
    double _health;

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

    void PathFinding()
    {
        if (_player != null && IsOnFloor() && _health > 0)
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
        if (_health > 0)
            _gameManager.EnemiesCount -= 1;
    }

    public override void _Ready()
    {
        #region NODES ASSIGNMENTS
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.EnemiesCount += 1;
        _bodyHitBox = GetNode<Area2D>("HitBox");
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _detectionArea = GetNode<Area2D>("DetectionArea");
        _hitBoxLeft = GetNode<Area2D>("HitBoxLeft");
        _hitBoxRight = GetNode<Area2D>("HitBoxRight");
        #endregion

        #region VAR INIT
        _health = _data.MaxHealth;
        #endregion

        #region EVENTS ASSIGNMENTS
        _detectionArea.BodyEntered += OnBodyEnteredDetectionArea;
        _detectionArea.BodyExited += OnBodyExitedDetectionArea;
        _animatedSprite.AnimationFinished += OnAnimationFinished;
        #endregion

        _animatedSprite.Play("idle");
    }

    public override void _Process(double delta)
    {
        PathFinding();

        if (_animatedSprite.Animation != "attack")
            if (Direction.X < 0)
                _animatedSprite.FlipH = true;
            else
                _animatedSprite.FlipH = false;

        if (_animatedSprite.Animation != "attack" && _health > 0)
            if (Mathf.Round(Velocity.X) == 0)
                _animatedSprite.Play("idle");
            else if (_health > 0)
                _animatedSprite.Play("running");

        if (_animatedSprite.FlipH)
            AttackCheck(_hitBoxLeft);
        else
            AttackCheck(_hitBoxRight);

        if (_isTargetWithinAtkRange && CanAtk())
            Attack();

        BodyHitBoxCheck();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        if (!_isVelocityOverrided && _health > 0)
            MoveToTargetLocation(ref velocity);

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
        if (_prevTarget == null || _target == null || _target.IsPositionPoint)
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

    void MoveToTargetLocation(ref Vector2 velocity)
    {
        if (_target != null)
        {
            var distance = _target.Position - GlobalPosition;

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

        velocity.X = Mathf.Lerp(
            velocity.X,
            Mathf.Sign(Direction.X) * _data.MaxSpeed,
            _data.AccelerationLerp
        );
    }
    #endregion

    #region COMBAT METHODS
    void Recoil(
        bool isRecoilRight,
        float offsetX = 1,
        float offsetY = 0,
        float durationX = 1,
        float durationY = 0.5f
    )
    {
        // set check
        _isVelocityOverrided = true;
        _isRecoiling = true;

        var directionX = isRecoilRight ? 1 : -1;
        float directionY = -1;
        var targetPosition =
            GlobalPosition + new Vector2(directionX * offsetX, directionY * offsetY);
        var distanceX = targetPosition.X - GlobalPosition.X;
        var distanceY = targetPosition.Y - GlobalPosition.Y;
        var targetVelocity = new Vector2(distanceX / durationX, distanceY / durationY);

        var tween = GetTree().CreateTween();

        tween.SetParallel(true).SetProcessMode(Tween.TweenProcessMode.Physics);
        tween
            .TweenProperty(this, "velocity:x", targetVelocity.X, durationX)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(this, "velocity:y", targetVelocity.Y, durationY)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween
            .Chain()
            .TweenProperty(this, "velocity:x", 0, durationX)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        tween
            .TweenProperty(this, "velocity:y", 0, durationY)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        tween.SetParallel(false);

        // reset check
        tween
            .TweenCallback(
                Callable.From(() =>
                {
                    _isRecoiling = false;
                    _isVelocityOverrided = false;
                })
            )
            .SetDelay(_data.StunDurationAfterRecoil);
    }

    private IEnumerator<double> _RefillAtk()
    {
        _isAtkRefilling = true;
        yield return Timing.WaitForSeconds(_data.AtkCooldown);
        _isAtkRefilling = false;
    }

    private bool CanAtk()
    {
        return _animatedSprite.Animation != "attack" && _health > 0 && !_isAtkRefilling;
    }

    private void Attack()
    {
        Timing.RunCoroutine(_Attack(), ATTACK_TAG);
    }

    /// <summary>
    ///  <see langword="this doesn't stop the animation"/>
    /// </summary>
    private void CancelAtk()
    {
        _isVelocityOverrided = false;
        Timing.KillCoroutines(ATTACK_TAG);
        Timing.RunCoroutine(_RefillAtk());
    }

    private IEnumerator<double> _Attack()
    {
        bool isStrikeConnected = false;
        // we stop the character only when it isn't recoiling.
        _isVelocityOverrided = true;
        while (Velocity.X != 0 && !_isRecoiling)
        {
            Velocity = Velocity.Lerp(new(0, Velocity.Y), _data.DeccelerationLerp);
            yield return Timing.WaitForOneFrame;
        }
        _animatedSprite.Play("attack");
        //then attack
        while (_animatedSprite.Animation == "attack")
        {
            GD.Print(_isTargetWithinAtkRange);
            if (IsCurrentFrame(4, 5) && _isTargetWithinAtkRange && !isStrikeConnected)
            {
                isStrikeConnected = _player.Hurt(_data.Damage);
            }

            yield return Timing.WaitForOneFrame;
        }

        if (!_isRecoiling)
            _isVelocityOverrided = false;
        Timing.RunCoroutine(_RefillAtk());
    }

    private void AttackCheck(Area2D hitbox)
    {
        var bodies = hitbox.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is Player)
            {
                _isTargetWithinAtkRange = true;
                return;
            }
        }
        _isTargetWithinAtkRange = false;
    }

    #endregion

    #region CONTACT
    public bool Hurt(int _dmg)
    {
        if (_health <= 0)
            return false;

        _health -= _dmg;

        Recoil(
            _player.IsFacingRight,
            _data.RecoilOffsetX / 2,
            _data.RecoilOffsetY / 2,
            _data.RecoilDurationX,
            _data.RecoilDurationY
        );

        // hurt flash
        var tween = GetTree().CreateTween();
        tween
            .TweenProperty(
                _animatedSprite.Material,
                "shader_parameter/flash_opacity",
                1,
                _data.HitBlinkEffectDuration
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                _animatedSprite.Material,
                "shader_parameter/flash_opacity",
                0,
                _data.HitBlinkEffectDuration
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (_health <= 0)
                    Timing.RunCoroutine(_Die());
            })
        );

        return true;
    }

    IEnumerator<double> _Die()
    {
        // First I turn off all of it hitbox and hurtbox
        HitBoxDisabled();
        _gameManager.EnemiesCount -= 1;

        // Stop the movememnt
        _isVelocityOverrided = true;

        // I need to wait till the body is on the floor and velocity is equal to zero and wait for attack animation to finish
        yield return Timing.WaitUntilDone(
            Timing.RunCoroutine(
                _StopAllHorizontalMovement(),
                Segment.PhysicsProcess,
                STOP_X_MOVEMENT_TAG
            )
        );

        while (!IsOnFloor() || _animatedSprite.Animation == "attack")
        {
            yield return Timing.WaitForOneFrame;
        }

        // I stop all the process
        SetPhysicsProcess(false);
        SetProcess(false);

        // Then I play the die animation
        _animatedSprite.Play("die");
    }

    private void HitBoxDisabled()
    {
        _bodyHitBox.SetDeferred("disable", true);
        _hitBoxLeft.SetDeferred("disable", true);
        _hitBoxRight.SetDeferred("disable", true);
    }

    private void BodyHitBoxCheck()
    {
        var bodies = _bodyHitBox.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is Player && _health > 0)
            {
                _player.Hurt(_data.Damage);
                return;
            }
        }
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
                .TweenProperty(_animatedSprite.Material, "shader_parameter/opacity", 0, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(this.QueueFree));
        }
        if (_health > 0)
            _animatedSprite.Play("idle");
    }

    #endregion

    #region GENERAL METHODS
    bool IsCurrentFrame(params int[] frames)
    {
        return frames.Contains(_animatedSprite.Frame);
    }

    IEnumerator<double> _StopAllHorizontalMovement()
    {
        while (Mathf.Abs(Velocity.X) != 0)
        {
            Velocity = new(Mathf.Lerp(Velocity.X, 0, _data.DeccelerationLerp), Velocity.Y);
            yield return Timing.WaitForOneFrame;
        }
    }
    #endregion
}
