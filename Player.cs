using System;
using Godot;

public partial class Player : CharacterBody2D
{
    // Khai báo trọng lực ( trọng lực mặc định của godot và có thể thay đổi ở giao diện)
    public float playerGravity;

    [Export]
    public PlayerData Data;

    #region STATE PARAMETERS
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }

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

    AnimatedSprite2D animatedSprite2D;

    public override void _Ready()
    {
        if (Data is PlayerData playerRunData)
        {
            Data = playerRunData;
        }
        IsFacingRight = true;
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        SetPlayerGravity(Data.GravityScale);
    }

    public override void _Process(double delta)
    {
        #region TIMERS
        LastOnGroundTime -= (float)delta;
        LastPressedJumpTime -= (float)delta;
        #endregion


        #region Animation
        if (LastOnGroundTime < 0)
        {
            animatedSprite2D.Animation = "jump";
            CheckDirectionToFace(_moveInput.X > 0);
        }
        else if (_moveInput.X != 0)
        {
            animatedSprite2D.Animation = "run";
            CheckDirectionToFace(_moveInput.X > 0);
        }
        else
            animatedSprite2D.Animation = "idle";
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
        #region Jump checks
        // falling
        if (IsJumping && Velocity.Y > 0)
        {
            IsJumping = false;
            _isJumpFalling = true;
        }
        // grounded
        if (LastOnGroundTime > 0 && !IsJumping)
        {
            _isJumpCut = false;
            _isJumpFalling = false;
        }
        #endregion

        #region gravity
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
        else if (
            (IsJumping || _isJumpFalling)
            && Mathf.Abs(Velocity.Y) < Data.JumpHangTimeThreshold
        )
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

    #region Input callback
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
        velocityCopied.X += CalculateVelocityX() * (float)delta;
        // jump
        if (CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            _isJumpCut = false;
            _isJumpFalling = false;
            velocityCopied.Y += CalculateJumpForce();
        }
        Velocity = velocityCopied;
        MoveAndSlide();
    }

    // Velocity METHODS
    #region RUN METHODS
    float CalculateVelocityX()
    {
        // calculate direction
        float targetSpeed = _moveInput.X * Data.RunMaxSpeed;

        #region Calculate AccelRate
        float accelRate;

        //Gets an acceleration value based on if we are accelerating (includes turning)
        //or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
        if (IsOnFloor())
            accelRate =
                (Mathf.Abs(targetSpeed) > 0.01f) ? Data.RunAccelAmount : Data.RunDeccelAmount;
        else
            accelRate =
                (Mathf.Abs(targetSpeed) > 0.01f)
                    ? Data.RunAccelAmount * Data.AccelInAir
                    : Data.RunDeccelAmount * Data.DeccelInAir;
        #endregion

        #region Add bonus Jump Apex Acceleration
        if (IsJumping && Mathf.Abs(Velocity.Y) < Data.JumpHangTimeThreshold)
        {
            accelRate *= Data.JumpHangTimeThreshold;
            targetSpeed *= Data.JumpHangMaxSpeedMult;
        }
        #endregion


        #region Converse Momentum
        //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
        if (
            Data.doConserveMomentum
            && Mathf.Abs(Velocity.X) > Mathf.Abs(targetSpeed)
            && Mathf.Sign(Velocity.X) == Mathf.Sign(targetSpeed)
            && Mathf.Abs(targetSpeed) > 0.01f
            && !IsOnFloor()
        )
        {
            //Prevent any deceleration from happening, or in other words conserve are current momentum
            //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }
        #endregion

        float speedDiff = targetSpeed - Velocity.X;
        float movement = speedDiff * accelRate;

        return movement;
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

    private float CalculateJumpForce()
    {
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        float force = Data.JumpForce;
        // increase force when the player falling, help with coyote time
        if (Velocity.Y > 0)
            force += Velocity.Y;
        // negative force to propel the player
        return -force;
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
