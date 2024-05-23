using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;

public partial class Player : CharacterBody2D
{
    public float playerGravity;

    [Export]
    public PlayerData Data;

    #region STATE PARAMETERS
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }

    public float LastOnGroundTime { get; private set; }
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
    bool isGroundedAnimationPlaying = false;
    bool isStopAnimationPlaying = false;
    #endregion

    public override void _Ready()
    {
        if (Data is PlayerData playerRunData)
        {
            Data = playerRunData;
        }
        IsFacingRight = true;
        IsFalling = false;
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        SetPlayerGravity(Data.GravityScale);
    }

    public override void _Process(double delta)
    {
        #region TIMERS
        LastOnGroundTime -= (float)delta;
        LastPressedJumpTime -= (float)delta;
        #endregion


        #region ANIMATION CHECKS
        if (_moveInput.X != 0)
            HandleAnimation("forward");
        else
            HandleAnimation("neutral");
        #endregion

        #region INPUT HANDLER
        _moveInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (_moveInput.X != 0)
            CheckDirectionToFace(_moveInput.X > 0);

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
        // MoveAndCollide(velocityCopied);
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

    #region ANIMTION METHODS
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
            animatedSprite2D.Play($"jump_{jumpType}_up");
        else if (IsFalling)
            animatedSprite2D.Play($"jump_{jumpType}_down");
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
            animatedSprite2D.Play($"{type}");
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
