using System;
using Godot;

public partial class GhoulData : Resource
{
    #region STAT
    [ExportGroup("Stat")]
    [Export]
    public double MaxHealth = 3;

    [Export]
    public int Damage = 1;

    [Export]
    public float MaxSpeed = 200.0f;

    [Export]
    public float AccelerationLerp = 0.75f;

    [Export]
    public float DeccelerationLerp = 0.9f;

    [Export]
    public float JumpVelocity = -450.0f;

    [Export]
    public float TinyJumpVelocity = -300.0f;

    [Export]
    public float SmallJumpVelocity = -350f;
    #endregion

    #region  COMBAT
    [ExportGroup("Combat")]
    [Export(PropertyHint.Range, "0,100,0.1,,or_greater")]
    public float RecoilOffsetX = 1;

    [Export(PropertyHint.Range, "0,100,0.1,,or_greater")]
    public float RecoilOffsetY = 10;

    [Export]
    public float RecoilDurationX = 1;

    [Export]
    public float RecoilDurationY = 0.5f;

    [Export]
    public float StunDurationAfterRecoil = 1;

    [Export]
    public float AtkCooldown = 0.3f;
    #endregion

    [ExportGroup("Shader")]
    [Export]
    public float HitBlinkEffectDuration;

    public GhoulData()
        : this(3, 1, 200, 0.75f, 0.9f, -450, -300, -350, 1, 10, 1, 0.5f, 1, 0.3f, 0.2f) { }

    public GhoulData(
        double maxHealth = 3,
        int dmg = 1,
        float maxSpeed = 200.0f,
        float accelerationLerp = 0.75f,
        float deccelerationLerp = 0.9f,
        float jumpVelocity = -450f,
        float tinyJumpVelocity = -300.0f,
        float smallJumpVelocity = -350f,
        float recoilOffsetX = 1,
        float recoilOffsetY = 10,
        float recoilDurationX = 1,
        float recoilDurationY = 0.5f,
        float stunDurationAfterBounceBack = 1,
        float atkCooldown = 0.3f,
        float hitBlinkEffectDuration = 0.2f
    )
    {
        MaxHealth = maxHealth;
        Damage = dmg;
        MaxSpeed = maxSpeed;
        AccelerationLerp = accelerationLerp;
        DeccelerationLerp = deccelerationLerp;
        JumpVelocity = jumpVelocity;
        TinyJumpVelocity = tinyJumpVelocity;
        SmallJumpVelocity = smallJumpVelocity;
        RecoilOffsetX = recoilOffsetX;
        RecoilOffsetY = recoilOffsetY;
        RecoilDurationX = recoilDurationX;
        RecoilDurationY = recoilDurationY;
        StunDurationAfterRecoil = stunDurationAfterBounceBack;
        AtkCooldown = atkCooldown;
        HitBlinkEffectDuration = hitBlinkEffectDuration;
    }
}
