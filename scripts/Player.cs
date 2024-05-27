using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;

public partial class Player : CharacterBody2D
{
    #region EDTIOR VARIABLES
    [Export]
    public AnimationData AnimationData;

    [Export]
    public PlayerData Data;
    float playerGravity;

    #endregion

    #region STATE PARAMETERS
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }

    public float LastOnGroundTime { get; private set; }
    public bool IsReadyForDeccelAtMaxSpeed { get; private set; }
    #endregion

    #region INPUT PARAMETERS
    Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }
    #endregion

    #region Jump
    bool _isJumpCut;
    bool _isJumpFalling;

    #endregion

    #region ANIMATION PARAMETERS
    AnimatedSprite2D animatedSprite2D;
    bool isGroundedAnimationPlaying;
    bool isStopAnimationPlaying;
    #endregion

    #region NODE
    CpuParticles2D runningDustLeft;
    CpuParticles2D runningDustRight;
    CpuParticles2D walkingDust;
    CpuParticles2D jumpingDust;
    #endregion


    public override void _Ready()
    {
        if (Data is PlayerData playerData)
        {
            Data = playerData;
        }
        if (AnimationData is AnimationData animationData)
        {
            AnimationData = animationData;
        }
        isGroundedAnimationPlaying = false;
        isStopAnimationPlaying = false;
        IsFacingRight = true;
        IsFalling = false;
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        defaultScale = animatedSprite2D.Scale;
        runningDustLeft = GetNode<CpuParticles2D>("RunningDustLeft");
        runningDustRight = GetNode<CpuParticles2D>("RunningDustRight");
        walkingDust = GetNode<CpuParticles2D>("WalkingDust");
        jumpingDust = GetNode<CpuParticles2D>("JumpingDust");
        SetPlayerGravity(Data.GravityScale);
    }

    public override void _Process(double delta)
    {
        #region TIMERS
        LastOnGroundTime -= (float)delta;
        LastPressedJumpTime -= (float)delta;
        #endregion


        #region MOVING CHECKS
        if (_moveInput.X != 0)
        {
            HandleAnimation("forward");
            CheckDirectionToFace(_moveInput.X > 0);
        }
        else
        {
            HandleAnimation("neutral");
        }
        #endregion

        #region INPUT HANDLER
        _moveInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (Input.IsActionJustPressed("jump"))
            OnJumpInput();

        if (Input.IsActionJustReleased("jump"))
            OnJumpUpInput();
        #endregion

        #region COLLISION CHECKS
        // Ground Check
        if (IsOnFloor() && !IsJumping)
            LastOnGroundTime = Data.CoyoteTime;

        #endregion

        #region JUMP CHECKS
        // jump falling
        if (IsJumping && Velocity.Y > 0)
        {
            IsJumping = false;
            _isJumpFalling = true;
        }
        // falling
        if (LastOnGroundTime < 0 && Velocity.Y > 0)
        {
            IsFalling = true;
        }
        // grounded
        if (LastOnGroundTime > 0 && !IsJumping)
        {
            _isJumpCut = false;
            _isJumpFalling = false;
            IsFalling = false;
        }
        #endregion


        #region GRAVITY
        // fast fall by holding down move down button
        if (Velocity.Y > 0 && _moveInput.Y > 0)
        {
            SetPlayerGravity(Data.GravityScale * Data.FastFallGravityMult);
            // cap max fall speed
            Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFastFallSpeed));
        }
        // increase gravity when player release the jump button
        else if (_isJumpCut)
        {
            SetPlayerGravity(Data.GravityScale * Data.JumpCutGravityMult);
            Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFallSpeed));
        }
        // reduce gravity when player at the peak of their jump => make the jump feel floaty
        else if ((IsJumping || _isJumpFalling) && Mathf.Abs(Velocity.Y) < Data.JumpHangThreshold)
        {
            SetPlayerGravity(Data.GravityScale * Data.JumpHangGravityMult);
        }
        // increase gravity when falling
        else if (Velocity.Y > 0)
        {
            SetPlayerGravity(Data.GravityScale * Data.FallGravityMult);
            Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFallSpeed));
        }
        else
        {
            SetPlayerGravity(Data.GravityScale);
        }
        #endregion
    }

    #region INPUT CALLBACK
    private void OnJumpUpInput()
    {
        if (CanJumpCut())
            _isJumpCut = true;
    }

    private void OnJumpInput()
    {
        LastPressedJumpTime = Data.JumpInputBufferTime;
        jumpingDust.Emitting = true;
    }
    #endregion

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocityCopied = Velocity;
        // apply gravity
        if (!IsOnFloor())
            velocityCopied.Y += playerGravity * (float)delta;
        else
            velocityCopied.Y = 0;

        // run
        velocityCopied.X += CalculateRunForce() * (float)delta;
        // jump
        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            _isJumpCut = false;
            _isJumpFalling = false;
            velocityCopied.Y -= CalculateJumpForce();
        }

        Velocity = velocityCopied;
        MoveAndSlide();

        if (Math.Round(Velocity.X) != 0)
            EmitRunningDust();
    }

    // Velocity METHODS
    #region RUN METHODS
    float CalculateRunForce()
    {
        // calculate direction
        float targetSpeed = _moveInput.X * Data.RunMaxSpeed;

        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning)
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (LastOnGroundTime > 0)
            accelRate =
                (Mathf.Abs(targetSpeed) > 0.01f) ? Data.RunAccelAmount : Data.RunDeccelAmount;
        else
            accelRate =
                (Mathf.Abs(targetSpeed) > 0.01f)
                    ? Data.RunAccelAmount * Data.AccelInAir
                    : Data.RunDeccelAmount * Data.DeccelInAir;
        #endregion

        #region  JUMP APEX ACCELERATION
        if (IsJumping && Mathf.Abs(Velocity.Y) < Data.JumpHangThreshold)
        {
            accelRate *= Data.JumpHangThreshold * 0.1f;
            targetSpeed *= Data.JumpHangMaxSpeedMult;
        }
        #endregion


        #region CONVERSE MOMENTUM
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (
            Data.doConserveMomentum
            && Mathf.Abs(Velocity.X) > Mathf.Abs(targetSpeed)
            && Mathf.Sign(Velocity.X) == Mathf.Sign(targetSpeed)
            && Mathf.Abs(targetSpeed) > 0.01f
            && LastOnGroundTime > 0
        )
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        float speedDiff = targetSpeed - Velocity.X;
        float force = speedDiff * accelRate;

        return force;
    }

    void Turn()
    {
        animatedSprite2D.FlipH = !animatedSprite2D.FlipH;
        IsFacingRight = !IsFacingRight;
    }
    #endregion

    #region Jump methods
    private bool CanJump()
    {
        return LastOnGroundTime > 0 && !IsJumping;
    }

    private bool CanJumpCut()
    {
        return IsJumping && Velocity.Y < 0;
    }

    Vector2 defaultScale;

    #region ANIMATION METHODS

    void StretchSprite(Vector2 scaleAddend)
    {
        Tween tween = GetTree().CreateTween();
        Vector2 stretchScale = defaultScale + scaleAddend;
        tween
            .TweenProperty(animatedSprite2D, "scale", stretchScale, 0.1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    void SquatchSprite(Vector2 scaleAddend)
    {
        Tween tween = GetTree().CreateTween();
        Vector2 squashScale = defaultScale + scaleAddend;
        tween
            .TweenProperty(animatedSprite2D, "scale", squashScale, 0.1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    void ScaleSpriteBackToDefault()
    {
        Tween tween = GetTree().CreateTween();
        tween
            .TweenProperty(animatedSprite2D, "scale", defaultScale, 0.01)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    private void HandleAnimation(string type)
    {
        bool isAirborne = LastOnGroundTime < 0;
        string prevAnimationName = animatedSprite2D.Animation.ToString();
        string[] parts = prevAnimationName.Split("_");
        string prevType = parts.Length > 1 ? parts[1] : parts[0];
        bool isJumpGroundedAnimation = parts[0] == "jump" && parts[2] == "grounded";
        bool isJumpDownAnimation = parts[0] == "jump" && parts[2] == "down";
        string jumpType = isAirborne || isJumpGroundedAnimation ? prevType : type; // jump related aninamtion need to use the same type of animation through out its life cycle. If it start with jump_forward_up it need to end in jump_forward_down

        if (IsJumping)
        {
            animatedSprite2D.Play($"jump_{jumpType}_up");
            if (jumpType == "neutral")
                SquatchSprite(AnimationData.NeutralSquashScaleAddend);
            else
                SquatchSprite(AnimationData.ForwardSquashScaleAddend);
        }
        else if (IsFalling)
        {
            animatedSprite2D.Play($"jump_{jumpType}_down");
            if (jumpType == "neutral")
                StretchSprite(AnimationData.NeutralStretchScaleAddend);
            else
                StretchSprite(AnimationData.ForwardStretchScaleAddend);
        }
        else if (isJumpDownAnimation && LastOnGroundTime > 0)
        {
            animatedSprite2D.Play($"jump_{jumpType}_grounded");
            isGroundedAnimationPlaying = true;
        }
        // wait for the animation to finish
        else if (isGroundedAnimationPlaying)
        {
            // 3 is the last frame
            isGroundedAnimationPlaying = animatedSprite2D.Frame != 3;
            animatedSprite2D.Play($"jump_{jumpType}_grounded");
        }
        else
        {
            animatedSprite2D.Play($"{type}");
            if (animatedSprite2D.Scale != defaultScale)
                ScaleSpriteBackToDefault();
        }
    }
    #endregion

    #region VFX & SFX METHODS
    private void EmitRunningDust()
    {
        bool isAtMaxSpeed = Mathf.Round(Mathf.Abs(Velocity.X)) >= Data.RunMaxSpeed;

        if (isAtMaxSpeed)
            IsReadyForDeccelAtMaxSpeed = true;

        // emit cloud of dust when change direction at max speed
        if (
            _moveInput.X != 0
            && LastOnGroundTime > 0
            && IsReadyForDeccelAtMaxSpeed
            && Mathf.Sign(Velocity.X) != _moveInput.X
        )
        {
            runningDustLeft.Emitting = !IsFacingRight;
            runningDustRight.Emitting = IsFacingRight;
        }
        // emit trailing dust when running at max speed
        walkingDust.Emitting =
            LastOnGroundTime > 0
            && isAtMaxSpeed
            && !runningDustRight.Emitting
            && !runningDustLeft.Emitting;

        if (runningDustRight.Emitting || runningDustLeft.Emitting)
            IsReadyForDeccelAtMaxSpeed = false;
    }

    private void EmitWalkingDust()
    {
        if (_moveInput.X != 0)
            walkingDust.Emitting = true;
        else
            walkingDust.Emitting = false;
    }

    #endregion

    private float CalculateJumpForce()
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = Data.JumpForce;
        // increase force when the player falling, help with coyote time
        if (Velocity.Y > 0)
            force += Velocity.Y;
        return force;
    }
    #endregion

    #region Check Methods;
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }
    #endregion

    #region General Methods
    private void SetPlayerGravity(float gravityScale)
    {
        float globalGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        playerGravity = globalGravity * gravityScale;
    }
    #endregion
}
