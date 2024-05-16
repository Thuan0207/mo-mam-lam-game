using System;
using Godot;

/*public partial class Character : CharacterBody2D
{
    private AnimatedSprite2D _animatedSprite;
    public override _Process(float _delta)
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        
    }
    public override void _PhysicsProcess(double delta){
         if (Input.IsActionPressed("ui_right")) {
            _animatedSprite.Play("run");
         }
         else{
            _animatedSprite.Stop();
         }
    }
}*/

public partial class Player : CharacterBody2D
{
    // Khai báo trọng lực ( trọng lực mặc định của godot và có thể thay đổi ở giao diện)

    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    [Export]
    public float JumpVelocity = -450.0f;

    [Export]
    public PlayerData Data;

    #region STATE PARAMETERS
    [Export]
    public bool IsFacingRight { get; private set; }

    [Export]
    public float LastOnGroundTime { get; private set; }
    #endregion

    #region INPUT PARAMETERS
    Vector2 _moveInput;
    #endregion

    #region CHECK PARAMETERS
    [ExportGroup("Checks")]
    [Export]
    Transform2D _groundCheckPoint;

    [Export]
    Vector2 _groundCheckSize = new(0.49f, 0.03f);
    #endregion

    AnimatedSprite2D animatedSprite2D;

    public override void _Ready()
    {
        if (Data is PlayerData playerRunData)
        {
            Data = playerRunData;
            GD.Print(playerRunData.CoyoteTime);
        }
        IsFacingRight = true;
        animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _Process(double delta)
    {
        #region TIMERS
        LastOnGroundTime -= (float)delta;
        #endregion

        #region INPUT HANDLER
        _moveInput.X = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").X;

        if (_moveInput.X != 0)
        {
            CheckDirectionToFace(_moveInput.X > 0);
            animatedSprite2D.Animation = "run";
        }
        else
            animatedSprite2D.Animation = "idle";
        #endregion

        #region COLLISION CHECKS
        // Ground Check
        if (IsOnFloor())
            LastOnGroundTime = 0.1f;
        #endregion
    }

    public override void _PhysicsProcess(double delta)
    {
        // khai báo tọa độ của player
        Vector2 velocity = Velocity;

        var animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        // Áp dụng trọng lực khi ở trên không
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;
        //animatedSprite2D.Animation = "jump";

        // nhảy khi player bấm space và ở trên sàn
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        if (!IsOnFloor())
        {
            animatedSprite2D.Animation = "jump";
        }

        Run(delta);

        // Velocity = velocity;
        // MoveAndSlide();
    }

    // Velocity METHODS
    #region RUN METHODS
    void Run(double delta)
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

        // Convert this to a vector and assign it to the player body then move it
        Velocity += movement * Vector2.Right;
        MoveAndSlide();
    }

    void Turn()
    {
        animatedSprite2D.FlipH = !animatedSprite2D.FlipH;
        IsFacingRight = !IsFacingRight;
    }
    #endregion

    #region Check Methods;
    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }
    #endregion
}
