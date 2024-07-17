using Godot;

[Tool]
public partial class CharacterData : Resource
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

    [ExportGroup("Dash")]
    [Export]
    public int dashAmount; // the amount of dash player can do until the cool down start

    [Export]
    public float dashSpeed; // the distance

    [Export]
    public float dashSleepTime; //Duration for which the game freezes when we press dash but before we read directional input and apply a force

    [Export]
    public float dashAttackTime;

    [Export]
    public float dashEndTime; //Time after you finish the inital drag phase, smoothing the transition back to idle (or any standard state)

    [Export]
    public Vector2 dashEndSpeed; //Slows down player, makes dash feel more responsive (used in Celeste)

    [Export(PropertyHint.Range, "0.0f,1.0f,0.1f")]
    public float dashEndRunLerp; //Slows the affect of player movement while dashing

    [Export]
    public float dashRefillTime;

    [Export(PropertyHint.Range, "0.0f,1.0f,0.1f")]
    public float DashInputBufferTime;

    #region COMBAT
    [ExportCategory("COMBAT")]
    [ExportGroup("Stats")]
    [Export]
    public float StrongHitTime = 0.2f;

    [Export]
    public float AttackCd; // cool down

    [Export]
    public int Damage;

    [Export]
    public int MaxHealth;

    [Export]
    public float InvinciblePeriod; // * The invicible period when player just receive damage

    [ExportGroup("Recoil")]
    [Export]
    public float RecoilDurationXSecond;

    [Export]
    public float RecoilDurationYSecond;

    [Export]
    public float RecoilOffsetX;

    [Export]
    public float RecoilOffsetY;

    [Export]
    public float FreezeDuration;

    [Export]
    public float FreezeScale;

    [Export]
    public float HitBlinkDuration;
    #endregion
    public CharacterData()
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
            0.088f,
            0.2f,
            1,
            5,
            0.5f,
            0.2f,
            0.1f,
            50f,
            25f,
            0.2f,
            0.05f,
            0.01f
        ) { }

    public CharacterData(
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
        float jumpInputBufferTime = 0.088f,
        float attackCd = 0.2f,
        int dmg = 1,
        int maxHealth = 5,
        float invinciblePeriod = 0.5f,
        float recoilDurationXSecond = 0.2f,
        float recoilDurationYSecond = 0.1f,
        float recoilOffsetX = 50,
        float recoilOffsetY = 25,
        float freezeDuration = 0.2f,
        float freezeScale = 0.05f,
        float hitBlinkDuration = 0.01f,
        float strongHitTime = 0.2f
    )
    {
        StrongHitTime = strongHitTime;
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
        AttackCd = attackCd;
        Damage = dmg;
        RecoilDurationXSecond = recoilDurationXSecond;
        RecoilDurationYSecond = recoilDurationYSecond;
        RecoilOffsetX = recoilOffsetX;
        RecoilOffsetY = recoilOffsetY;
        FreezeDuration = freezeDuration;
        FreezeScale = freezeScale;
        MaxHealth = maxHealth;
        InvinciblePeriod = invinciblePeriod;
        HitBlinkDuration = hitBlinkDuration;
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
