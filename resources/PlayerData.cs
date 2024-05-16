using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;

[Tool]
public partial class PlayerData : Resource
{
    [ExportCategory("Movement")]
    [ExportGroup("Gravity")]
    // use to calculate jumpHeight and jumpTimeToApex
    float _gravityStrength;
    public float GravityStrength
    {
        get => _gravityStrength;
        set
        {
            _gravityStrength = value;
            GravityScale = CalculateGravityScale(_gravityStrength);
            JumpForce = CalculateJumpForce(_gravityStrength, _jumpTimeToApex);
        }
    }

    //Strength of the player's gravity as a multiplier of gravity (set in ProjectSettings/Physics2D).
    //Also the value the player's rigidbody2D.gravityScale is set to.
    public float GravityScale;

    [Export]
    //Multiplier to the player's gravityScale when falling.
    public float FallGravityMult;

    [Export]
    public float MaxFallSpeed;

    [Export]
    //Larger multiplier to the player's gravityScale when they are falling and a downwards input is pressed.
    //Seen in games such as Celeste, lets the player fall extra fast if they wish.df
    public float FastFallGravityMult;

    [Export]
    public float MaxFastFallSpeed;

    [ExportGroup("Jump")]
    float _jumpHeight = 300.0f;

    [Export]
    public float JumpHeight
    {
        get => _jumpHeight;
        set
        {
            _jumpHeight = value;
            GravityStrength = CalculateGravityStrength(_jumpHeight, _jumpTimeToApex);
        }
    }
    float _jumpTimeToApex = 1;

    [Export]
    //Time between applying the jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
    public float JumpTimeToApex
    {
        get => _jumpTimeToApex;
        set
        {
            _jumpTimeToApex = value;
            GravityStrength = CalculateGravityStrength(_jumpHeight, _jumpTimeToApex);
            JumpForce = CalculateJumpForce(_gravityStrength, _jumpTimeToApex);
        }
    }
    public float JumpForce;

    [ExportGroup("Jump Juice")]
    [Export]
    //Multiplier to increase gravity if the player releases thje jump button while still jumping
    public float JumpCutGravityMult;

    [Export(PropertyHint.Range, "0.0f,1.0f,0.1f")]
    //Reduces gravity while close to the apex (desired max height) of the jump
    public float JumpHangGravityMult;

    [Export]
    //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)

    public float JumpHangTimeThreshold;

    [Export]
    public float JumpHangAccelerationMult;

    [Export]
    public float JumpHangMaxSpeedMult;

    [ExportGroup("Run")]
    [Export]
    float _runMaxSpeed = 250.0f;
    public float RunMaxSpeed
    {
        get => _runMaxSpeed;
        set
        {
            _runMaxSpeed = value;
            RunAccelAmount = CalculateAccelAmount(_accelTime, value);
            RunDeccelAmount = CalculateAccelAmount(_deccelTime, value);
            AccelTime = Clamp(_accelTime, value);
            DeccelTime = Clamp(_deccelTime, value);
        }
    }

    // the actual force (multiplied with `speedDiff`) apply to the player
    public float RunAccelAmount { get; private set; }

    // the time it take to reach max speed
    float _accelTime = 0.5f;

    [Export]
    public float AccelTime
    {
        get => _accelTime;
        set
        {
            _accelTime = Clamp(value, _runMaxSpeed);
            RunAccelAmount = CalculateAccelAmount(_accelTime, _runMaxSpeed);
        }
    }

    public float RunDeccelAmount { get; private set; }

    // the time it take to deccelerate player from max speed to 0
    float _deccelTime = 0.2f;

    [Export]
    public float DeccelTime
    {
        get => _deccelTime;
        set
        {
            _deccelTime = Clamp(value, _runMaxSpeed);
            RunDeccelAmount = CalculateAccelAmount(_deccelTime, _runMaxSpeed);
        }
    }

    [Export(PropertyHint.Range, "0.01f,1.0f,0.01f")]
    //Multipliers applied to acceleration rate when airborne.
    public float AccelInAir { get; set; } = 0.5f;

    [Export(PropertyHint.Range, "0.01f,1.0f,0.01f")]
    public float DeccelInAir { get; set; } = 0.5f;
    public bool doConserveMomentum = true;

    [ExportGroup("Assists")]
    [Export(PropertyHint.Range, "0.01f,0.5f,0.01f")]
    public float CoyoteTime; //Grace period after falling off a platform, where you can still jum

    [Export(PropertyHint.Range, "0.01f,0.5f,0.01f")]
    public float JumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.

    public PlayerData()
        : this(
            0.5f,
            50.0f,
            0.5f,
            70.0f,
            100.0f,
            1.5f,
            0.5f,
            0.5f,
            0.1f,
            0.5f,
            1.0f,
            250.0f,
            0.5f,
            0.2f,
            0.5f,
            0.5f,
            0.2f,
            0.5f
        ) { }

    public PlayerData(
        float fallGravityMult = 0.5f,
        float maxFallSpeed = 50,
        float fastFallGravityMult = 0.5f,
        float maxFastFallSpeed = 70,
        float jumpHeight = 100,
        float jumpTimeToApex = 1.5f,
        float jumpCutGravityMult = 0.5f,
        float jumpHangGravityMult = 0.5f,
        float jumpHangTimeThreshold = 0.1f,
        float jumpHangAccelerationMult = 0.5f,
        float jumpHangMaxSpeedMult = 1f,
        float runMaxSpeed = 250,
        float runAcceleration = 0.5f,
        float runDecceleration = 0.2f,
        float accelInAir = 0.5f,
        float deccelInair = 0.5f,
        float coyoteTime = 0.2f,
        float jumpInputBufferTime = 0.5f
    )
    {
        RunMaxSpeed = runMaxSpeed;
        AccelTime = runAcceleration;
        DeccelTime = runDecceleration;
        AccelInAir = accelInAir;
        DeccelInAir = deccelInair;
        FallGravityMult = fallGravityMult;
        MaxFallSpeed = maxFallSpeed;
        FastFallGravityMult = fastFallGravityMult;
        MaxFastFallSpeed = maxFastFallSpeed;
        JumpHeight = jumpHeight;
        JumpTimeToApex = jumpTimeToApex;
        JumpCutGravityMult = jumpCutGravityMult;
        JumpHangGravityMult = jumpHangGravityMult;
        JumpHangTimeThreshold = jumpHangTimeThreshold;
        JumpHangAccelerationMult = jumpHangAccelerationMult;
        JumpHangMaxSpeedMult = jumpHangMaxSpeedMult;
        CoyoteTime = coyoteTime;
        JumpInputBufferTime = jumpInputBufferTime;
    }

    static float CalculateAccelAmount(float time, float runMaxSpeed)
    {
        return 60 * time / runMaxSpeed;
    }

    static float Clamp(float number, float runMaxSpeed)
    {
        return Mathf.Clamp(number, 0.01f, runMaxSpeed);
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }

    private static float CalculateGravityStrength(float jumpHeight, float jumpTimeToApex)
    {
        //Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2)
        return -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
    }

    private static float CalculateGravityScale(float gravityStrength)
    {
        float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        return gravityStrength / gravity;
    }

    private static float CalculateJumpForce(float gravityStrength, float jumpTimeToApex)
    {
        return Mathf.Abs(gravityStrength) * jumpTimeToApex;
    }
}
