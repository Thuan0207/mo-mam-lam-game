using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MEC;
using static Godot.GD;

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
    public bool IsDashing { get; private set; }
    public float LastOnGroundTime { get; private set; }
    public bool IsReadyForDeccelAtMaxSpeed { get; private set; }
    #endregion

    #region INPUT PARAMETERS
    Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }
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
    CpuParticles2D explosionDust;
    #endregion

    #region SCENE
    PackedScene dashGhostTscn;
    #endregion

    #region DASH
    Vector2 lastDashDir;
    int dashesLeft;
    bool dashRefilling;
    bool isDashAttacking; // Dash attack is the first phase in dash (the first few ms) where player can not do change anything
    #endregion

    #region GENERAL PARAMETERS
    Vector2 defaultScale;
    double localTimeScale;
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
        runningDustLeft = GetNode<CpuParticles2D>("RunningDustLeft");
        runningDustRight = GetNode<CpuParticles2D>("RunningDustRight");
        walkingDust = GetNode<CpuParticles2D>("WalkingDust");
        jumpingDust = GetNode<CpuParticles2D>("JumpingDust");
        explosionDust = GetNode<CpuParticles2D>("Explosion");

        localTimeScale = 1;
        defaultScale = animatedSprite2D.Scale;

        dashGhostTscn = ResourceLoader.Load<PackedScene>("res://scenes/VFX/DashGhost.tscn");

        SetGravityScale(Data.GravityScale);
    }

    public override void _Process(double _d)
    {
        float delta = (float)(_d * localTimeScale);

        #region TIMERS
        LastOnGroundTime -= delta;
        LastPressedJumpTime -= delta;
        LastPressedDashTime -= delta;
        #endregion

        #region INPUT HANDLER
        _moveInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (Input.IsActionJustPressed("jump"))
            OnJumpInput();

        if (Input.IsActionJustReleased("jump"))
            OnJumpUpInput();

        if (Input.IsActionJustPressed("dash"))
            OnDashInput();
        #endregion

        // Ground Check
        if (IsOnFloor() && !IsJumping && !IsDashing)
            LastOnGroundTime = Data.CoyoteTime;

        #region MOVING PROCESS
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


        #region GRAVITY PROCESS
        if (!isDashAttacking)
        { // fast fall by holding down move down button
            if (Velocity.Y > 0 && _moveInput.Y > 0)
            {
                SetGravityScale(Data.GravityScale * Data.FastFallGravityMult);
                // cap max fall speed
                Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFastFallSpeed));
            }
            // increase gravity when player release the jump button
            else if (_isJumpCut)
            {
                SetGravityScale(Data.GravityScale * Data.JumpCutGravityMult);
                Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFallSpeed));
            }
            // reduce gravity when player at the peak of their jump => make the jump feel floaty
            else if (
                (IsJumping || _isJumpFalling)
                && Mathf.Abs(Velocity.Y) < Data.JumpHangThreshold
            )
            {
                SetGravityScale(Data.GravityScale * Data.JumpHangGravityMult);
            }
            // increase gravity when falling
            else if (Velocity.Y > 0)
            {
                SetGravityScale(Data.GravityScale * Data.FallGravityMult);
                Velocity = new Vector2(Velocity.X, Mathf.Max(Velocity.Y, Data.MaxFallSpeed));
            }
            else
            {
                SetGravityScale(Data.GravityScale);
            }
        }
        else
        {
            //No gravity when dashing (returns to normal once initial dashAttack phase over)
            SetGravityScale(0);
        }
        #endregion

        DashCheck();
    }

    #region INPUT CALLBACK
    private void OnJumpUpInput()
    {
        if (CanJumpCut())
            _isJumpCut = true;
    }

    private void OnDashInput()
    {
        LastPressedDashTime = Data.dashInputBufferTime;
    }

    private void OnJumpInput()
    {
        LastPressedJumpTime = Data.JumpInputBufferTime;
        jumpingDust.Emitting = true;
    }
    #endregion

    public override void _PhysicsProcess(double _d)
    {
        float delta = (float)(_d * localTimeScale);
        Vector2 v = Velocity;

        // apply gravity
        if (!IsOnFloor())
            v.Y += playerGravity * delta;
        else
            v.Y = 0;

        // run
        if (!IsDashing)
            v.X += CalculateRunForce(1) * delta;
        else if (isDashAttacking)
            v.X += CalculateRunForce(Data.dashEndRunLerp) * delta;

        // jump
        if (!IsDashing && CanJump() && LastPressedJumpTime > 0)
        {
            IsJumping = true;
            _isJumpCut = false;
            _isJumpFalling = false;
            v.Y -= CalculateJumpForce();
        }

        Velocity = v;
        MoveAndSlide();

        if (Math.Round(Velocity.X) != 0)
            EmitRunningDust();
    }

    // Velocity METHODS
    #region RUN METHODS
    float CalculateRunForce(float lerpAmount)
    {
        // calculate direction
        float targetSpeed = _moveInput.X * Data.RunMaxSpeed;

        //We can reduce are control using Lerp() this smooths changes to are direction and speed
        targetSpeed = Mathf.Lerp(Velocity.X, targetSpeed, lerpAmount);

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

    private void HandleAnimation(string category)
    {
        bool isAirborne = LastOnGroundTime < 0;
        string prevAnimationName = animatedSprite2D.Animation.ToString();
        string[] prevParts = prevAnimationName.Split("_");
        string prevType = prevParts.Length == 3 ? prevParts[0] : null;
        string prevCategory = prevParts.Length == 3 ? prevParts[1] : null;
        string prevDirection = prevParts.Length == 3 ? prevParts[2] : null;
        bool jumpGroundedAnimationPlaying = prevType == "jump" && prevDirection == "grounded";
        bool jumpDownAnimationPlaying = prevType == "jump" && prevDirection == "down";
        string jumpType =
            (isAirborne || jumpGroundedAnimationPlaying) && prevCategory != null
                ? prevCategory
                : category; // jump related aninamtion need to use the same type of animation through out its life cycle. If it start with jump_forward_up it need to end in jump_forward_down

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
        else if (jumpDownAnimationPlaying && LastOnGroundTime > 0)
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
            animatedSprite2D.Play($"{category}");
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
    #endregion

    #region Jump methods
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
    private bool CanJump()
    {
        return LastOnGroundTime > 0 && !IsJumping;
    }

    private bool CanJumpCut()
    {
        return IsJumping && Velocity.Y < 0;
    }

    private bool CanDash()
    {
        if (!IsDashing && dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !dashRefilling) // refil dash
        {
            Timing.RunCoroutine(RefillDash(1), "RefillDash");
        }

        return dashesLeft > 0; // can dash
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != IsFacingRight)
            Turn();
    }

    void DashCheck()
    {
        if (CanDash() && LastPressedDashTime > 0)
        {
            Sleep(Data.dashSleepTime); //Freeze game for split second. Adds juiciness and a bit of forgiveness over directional input

            if (_moveInput != Vector2.Zero) //If not direction pressed, dash forward
            {
                lastDashDir = _moveInput;
            }
            else
                lastDashDir = IsFacingRight ? Vector2.Right : Vector2.Left;

            IsDashing = true;
            IsJumping = false;
            _isJumpCut = false;

            Timing.RunCoroutine(StartDash(lastDashDir));
        }
    }
    #endregion

    #region DASH METHODS
    private IEnumerator<double> StartDash(Vector2 dir)
    {
        //Overall this method of dashing aims to mimic Celeste, if you're looking for
        // a more physics-based approach try a method similar to that used in the jump
        LastOnGroundTime = 0;
        LastPressedDashTime = 0;

        Timing.RunCoroutine(InstanceGhostDash(3));

        float startTime = Time.GetTicksMsec();

        dashesLeft--;
        isDashAttacking = true;

        SetGravityScale(0);

        //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
        while (Time.GetTicksMsec() - startTime <= Data.dashAttackTime * 1000)
        {
            Velocity = Data.dashSpeed * (float)localTimeScale * dir.Normalized();
            //Pauses the loop until the next frame, creating something of a Update loop.
            //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
            yield return Timing.WaitForOneFrame;
        }

        startTime = Time.GetTicksMsec();

        isDashAttacking = false;

        //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
        SetGravityScale(Data.GravityScale);
        Velocity = Data.dashEndSpeed * (float)localTimeScale * dir.Normalized();

        while (Time.GetTicksMsec() - startTime <= Data.dashEndTime * 1000)
        {
            yield return Timing.WaitForOneFrame;
        }

        //Dash over
        IsDashing = false;
    }

    [Export]
    public float sizeScale;

    // create ghost effect for dash animation
    private IEnumerator<double> InstanceGhostDash(int amount)
    {
        bool isFirstFrame = true;
        float dashDuration = Data.dashAttackTime + Data.dashEndTime;
        float lapse = dashDuration / amount;
        while (IsDashing)
        {
            var _ghost = dashGhostTscn.Instantiate<Sprite2D>();
            _ghost.Scale = animatedSprite2D.Scale;
            _ghost.Texture = animatedSprite2D.SpriteFrames.GetFrameTexture(
                animatedSprite2D.Animation,
                animatedSprite2D.Frame
            );

            _ghost.FlipH = animatedSprite2D.FlipH;
            _ghost.GlobalPosition = animatedSprite2D.GlobalPosition;
            _ghost.Material = !isFirstFrame ? null : _ghost.Material;

            if (_ghost.Material != null && _ghost.Material is ShaderMaterial shaderMaterial)
            {
                Tween tween = GetTree().CreateTween();
                float size = (float)shaderMaterial.GetShaderParameter("size");
                float outeredgeOffset = (float)shaderMaterial.GetShaderParameter("outeredgeOffset");

                explosionDust.Emitting = true;
                tween
                    .TweenMethod(
                        Callable.From(
                            (float size) => shaderMaterial.SetShaderParameter("size", size)
                        ),
                        size,
                        size * sizeScale,
                        Data.dashAttackTime
                    )
                    .SetTrans(Tween.TransitionType.Expo)
                    .SetEase(Tween.EaseType.In);
                tween
                    .Parallel()
                    .TweenMethod(
                        Callable.From(
                            (float offset) =>
                                shaderMaterial.SetShaderParameter("outeredgeOffset", offset)
                        ),
                        outeredgeOffset,
                        0.1f,
                        Data.dashAttackTime
                    )
                    .SetTrans(Tween.TransitionType.Expo)
                    .SetEase(Tween.EaseType.In);
                tween.TweenCallback(
                    Callable.From(() =>
                    {
                        shaderMaterial.SetShaderParameter("size", size);
                        shaderMaterial.SetShaderParameter("outeredgeOffset", outeredgeOffset);
                    })
                );
            }
            isFirstFrame = false;
            GetTree().Root.AddChild(_ghost);
            yield return Timing.WaitForSeconds(lapse);
        }
    }

    //Short period before the player is able to dash again
    private IEnumerator<double> RefillDash(int amount)
    {
        //cooldown, so we can't constantly dash along the ground, again this is the implementation in Celeste, feel free to change it up
        dashRefilling = true;
        yield return Timing.WaitForSeconds(Data.dashRefillTime);
        dashRefilling = false;
        dashesLeft = Mathf.Min(Data.dashAmount, dashesLeft + amount);
    }
    #endregion

    #region GENERAL METHODS
    private void SetGravityScale(float gravityScale)
    {
        float globalGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        playerGravity = globalGravity * gravityScale;
    }

    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        Timing.RunCoroutine(PerformSleep(duration));
    }

    private IEnumerator<double> PerformSleep(float duration)
    {
        localTimeScale = 0;
        yield return Timing.WaitForSeconds(duration);
        localTimeScale = 1;
    }

    #endregion
}
