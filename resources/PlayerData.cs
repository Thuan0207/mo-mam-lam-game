using Godot;

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
    float _jumpHeight;

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
    float _jumpTimeToApex;

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

    public float JumpHangThreshold;

    [Export]
    public float JumpHangAccelerationMult;

    [Export]
    public float JumpHangMaxSpeedMult;

    [ExportGroup("Run")]
    [Export]
    float _runMaxSpeed;
    public float RunMaxSpeed
    {
        get => _runMaxSpeed;
        set
        {
            _runMaxSpeed = value;
            RunAccelAmount = CalculateAccelAmount(_acceleration, value);
            RunDeccelAmount = CalculateAccelAmount(_deceleration, value);
            Acceleration = Clamp(_acceleration, value);
            Decceleration = Clamp(_deceleration, value);
        }
    }

    // the actual force (multiplied with `speedDiff`) apply to the player
    public float RunAccelAmount { get; private set; }

    // the time it take to reach max speed
    float _acceleration;

    [Export]
    public float Acceleration
    {
        get => _acceleration;
        set
        {
            _acceleration = Clamp(value, _runMaxSpeed);
            RunAccelAmount = CalculateAccelAmount(_acceleration, _runMaxSpeed);
        }
    }

    public float RunDeccelAmount { get; private set; }

    // the time it take to deccelerate player from max speed to 0
    float _deceleration;

    [Export]
    public float Decceleration
    {
        get => _deceleration;
        set
        {
            _deceleration = Clamp(value, _runMaxSpeed);
            RunDeccelAmount = CalculateAccelAmount(_deceleration, _runMaxSpeed);
        }
    }

    [Export(PropertyHint.Range, "0.01f,1.0f,0.01f")]
    //Multipliers applied to acceleration rate when airborne.
    public float AccelInAir { get; set; }

    [Export(PropertyHint.Range, "0.01f,1.0f,0.01f")]
    public float DeccelInAir { get; set; }
    public bool doConserveMomentum = true;

    [ExportGroup("Assists")]
    [Export(PropertyHint.Range, "0.01f,0.5f,0.01f")]
    public float CoyoteTime; //Grace period after falling off a platform, where you can still jum

    [Export(PropertyHint.Range, "0.01f,0.5f,0.01f")]
    public float JumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.

    public PlayerData()
        : this(
            0.95f,
            20f,
            1.28f,
            40f,
            90f,
            0.36f,
            1.1f,
            0.8f,
            16.0f,
            1.0f,
            1.1f,
            200f,
            24.0f,
            42.0f,
            1.0f,
            1.0f,
            0.2f,
            0.088f
        ) { }

    public PlayerData(
        float fallGravityMult = 0.95f,
        float maxFallSpeed = 20,
        float fastFallGravityMult = 1.28f,
        float maxFastFallSpeed = 40,
        float jumpHeight = 90,
        float jumpTimeToApex = 0.36f,
        float jumpCutGravityMult = 1.1f,
        float jumpHangGravityMult = 0.8f,
        float jumpHangThreshold = 16.0f,
        float jumpHangAccelerationMult = 1.0f,
        float jumpHangMaxSpeedMult = 1.1f,
        float runMaxSpeed = 200,
        float acceleration = 24.0f,
        float decceleration = 42.0f,
        float accelInAir = 1.0f,
        float deccelInair = 1.0f,
        float coyoteTime = 0.2f,
        float jumpInputBufferTime = 0.088f
    )
    {
        RunMaxSpeed = runMaxSpeed;
        Acceleration = acceleration;
        Decceleration = decceleration;
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
        JumpHangThreshold = jumpHangThreshold;
        JumpHangAccelerationMult = jumpHangAccelerationMult;
        JumpHangMaxSpeedMult = jumpHangMaxSpeedMult;
        CoyoteTime = coyoteTime;
        JumpInputBufferTime = jumpInputBufferTime;
    }

    #region assist
    static float Clamp(float number, float runMaxSpeed)
    {
        return Mathf.Clamp(number, 0.01f, runMaxSpeed);
    }
    #endregion

    #region Data calculation
    static float CalculateAccelAmount(float acceleration, float runMaxSpeed)
    {
        return 60 * acceleration / runMaxSpeed;
    }

    private static float CalculateGravityStrength(float jumpHeight, float jumpTimeToApex)
    {
        //Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2)
        return 2 * jumpHeight / (jumpTimeToApex * jumpTimeToApex);
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
    #endregion
}
