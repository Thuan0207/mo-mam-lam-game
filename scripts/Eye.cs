using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MEC;

public partial class Eye : CharacterBody2D, IHurtableBody
{
    #region GENERAL
    [Export]
    GhoulData _data;

    bool _isVelocityOverrided;

    public Vector2 Direction;

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    AnimatedSprite2D _animatedSprite;

    Area2D _bodyHitBox;

    [Export]
    Area2D _hitBoxRight;

    [Export]
    Area2D _hitBoxLeft;

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

    void StopPathFinding()
    {
        _pathQueue.Clear();
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
        _bodyHitBox.BodyEntered += OnBodyEnteredHitBox;
        _animatedSprite.AnimationFinished += OnAnimationFinished;
        #endregion

        _animatedSprite.Play("idle");
    }

    public override void _Process(double delta)
    {
        PathFinding();

        if (Direction.X < 0)
            _animatedSprite.FlipH = true;
        else
            _animatedSprite.FlipH = false;

        if (_animatedSprite.Animation != "attack")
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
        if (_animatedSprite.Animation == "attack")
            CancelAtk();
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
        var targetVelocity = new Vector2(distanceX / durationX, distanceY / durationX);

        var tween = GetTree().CreateTween();

        tween.SetParallel(true);
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

    private IEnumerator<double> _Attack()
    {
        bool isStrikeConnected = false;
        // we stop the character only when it isn't recoiling.
        _isVelocityOverrided = true;
        while (Velocity.X != 0 && !_isRecoiling)
        {
            Velocity = new Vector2(Mathf.Lerp(Velocity.X, 0, _data.DeccelerationLerp), Velocity.Y);
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
        _isVelocityOverrided = false;
        Timing.RunCoroutine(_RefillAtk());
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

    private void OnBodyEnteredHitBox(Node2D body)
    {
        if (body is Player player && _health > 0)
        {
            player.Hurt(_data.Damage);

            // HurtRecoil(
            //     _player.IsFacingRight,
            //     _data.RecoilOffsetX,
            //     _data.RecoilOffsetY,
            //     _data.RecoilDuration
            // );
        }
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

    IEnumerator<double> _StopMovementInOneDirection(bool isRight)
    {
        if (isRight && Velocity.X > 0)
        {
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(
                    _StopAllHorizontalMovement(),
                    Segment.PhysicsProcess,
                    STOP_X_MOVEMENT_TAG
                )
            );
        }
        else if (Velocity.X < 0)
        {
            yield return Timing.WaitUntilDone(
                Timing.RunCoroutine(
                    _StopAllHorizontalMovement(),
                    Segment.PhysicsProcess,
                    STOP_X_MOVEMENT_TAG
                )
            );
        }
    }
    #endregion
}
