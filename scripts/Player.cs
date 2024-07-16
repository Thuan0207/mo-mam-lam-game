using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using MEC;

public partial class Player : CharacterBody2D, IHurtableBody
{
    #region EDTIOR VARIABLES
    [Export]
    public AnimationData AnimationData;

    [Export]
    public CharacterData Data;
    float playerGravity;

    #endregion
    #region
    public AudioStreamPlayer2D AudioStreamPlayerJump;
    public AudioStreamPlayer2D AudioStreamPlayerRunning;
    #endregion

    #region STATE PARAMETERS
    public bool IsFacingRight { get; private set; }
    public bool IsJumping { get; private set; }

    public bool IsFalling { get; private set; }
    public bool IsDashing { get; private set; }
    public float LastOnGroundTime { get; private set; }
    public bool IsReadyForDeccelAtMaxSpeed { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsAtkConnected { get; private set; }
    public bool IsStriking { get; private set; }
    public bool _isRecoiling;
    #endregion

    #region INPUT PARAMETERS
    Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }
    public float AttackCd { get; private set; }
    bool _isAllInputDisabled;
    bool _isActionInputDisabled;
    #endregion

    #region Jump
    bool _isJumpCut;
    bool _isJumpFalling;

    #endregion

    #region ANIMATION PARAMETERS
    bool isGroundedAnimationPlaying;
    bool isAttackAnimationPlaying;
    bool isHurtAnimationPlaying;
    bool isDieAnimationPlaying;
    bool isDashAnimationPlaying;
    #endregion

    #region NODE
    AudioStreamPlayer2D hitAudio;
    AudioStreamPlayer2D hurtAudio;
    AudioStreamPlayer2D dashAudio;
    Joystick _joyStick;
    GameManager _gameManager;
    CpuParticles2D runningDustLeft;
    CpuParticles2D runningDustRight;
    CpuParticles2D walkingDust;
    CpuParticles2D jumpingDust;
    CpuParticles2D explosionDust;
    public AnimatedSprite2D CharacterSprite;
    AnimatedSprite2D attackFxRight;
    AnimatedSprite2D attackFxLeft;
    Area2D hitboxLeft;
    Area2D hitboxRight;
    #endregion

    #region SCENE
    PackedScene _dashGhostTscn;
    PackedScene _impactHitTscn;
    #endregion

    #region DASH
    Vector2 lastDashDir;
    int dashesLeft;
    bool dashRefilling;
    bool isDashAttacking; // Dash attack is the first phase in dash (the first few ms) where player can not do change anything
    #endregion

    #region GENERAL VARIABLES
    Vector2 defaultScale;
    double localTimeScale;
    readonly Regex attackRegex = new(@"^attack\d+$");
    readonly Regex jumpUpRegex = new(@"(?=.*jump)(?=.*up)");
    readonly Regex jumpDownRegex = new(@"(?=.*jump)(?=.*down)");
    readonly Regex jumpNeutralRegex = new(@"\bjump\b.*\bneutral\b");
    readonly Regex jumpForwardRegex = new(@"\bjump\b.*\bforward\b");
    float runLerp;
    #endregion

    #region STATUS
    int _health;
    #endregion

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth);

    public override void _Ready()
    {
        if (Data is CharacterData playerData)
        {
            Data = playerData;
        }
        if (AnimationData is AnimationData animationData)
        {
            AnimationData = animationData;
        }

        runLerp = 1;

        _health = Data.MaxHealth;

        isGroundedAnimationPlaying = false;

        IsFacingRight = true;
        IsFalling = false;
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _joyStick = GetParent().GetNode<Joystick>("CanvasLayer/Joystick");
        AudioStreamPlayerJump = GetNode<AudioStreamPlayer2D>("AudioStreamPlayerJump");
        AudioStreamPlayerRunning = GetNode<AudioStreamPlayer2D>("AudioStreamPlayerRunning");
        CharacterSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        runningDustLeft = GetNode<CpuParticles2D>("RunningDustLeft");
        runningDustRight = GetNode<CpuParticles2D>("RunningDustRight");
        walkingDust = GetNode<CpuParticles2D>("WalkingDust");
        jumpingDust = GetNode<CpuParticles2D>("JumpingDust");
        explosionDust = GetNode<CpuParticles2D>("Explosion");
        hitboxLeft = GetNode<Area2D>("HitBoxLeft");
        hitboxRight = GetNode<Area2D>("HitBoxRight");
        attackFxRight = GetNode<AnimatedSprite2D>("AnimatedSprite2D/AttackFxRight");
        attackFxLeft = GetNode<AnimatedSprite2D>("AnimatedSprite2D/AttackFxLeft");
        hitAudio = GetNode<AudioStreamPlayer2D>("HitAudio");
        hurtAudio = GetNode<AudioStreamPlayer2D>("HurtAudio");
        dashAudio = GetNode<AudioStreamPlayer2D>("DashAudio");

        localTimeScale = 1;
        defaultScale = CharacterSprite.Scale;

        _dashGhostTscn = ResourceLoader.Load<PackedScene>("res://scenes/VFX/DashGhost.tscn");
        _impactHitTscn = ResourceLoader.Load<PackedScene>("uid://chxjths3qoinh");
        CharacterSprite.AnimationFinished += () =>
        {
            // if currently attack animation
            if (attackRegex.IsMatch(CharacterSprite.Animation))
            {
                IsAttacking = false;
                IsAtkConnected = false;
                AttackCd = Data.AttackCd;
            }
            ResetAllPlayingAnimationCheck();
            if (CharacterSprite.Animation == "die")
            {
                GetTree().ChangeSceneToFile("res://scenes/DieMenu.tscn");
            }
        };

        SetGravityScale(Data.GravityScale);
    }

    public override void _Process(double _d)
    {
        float delta = (float)(_d * localTimeScale);

        #region TIMERS
        LastOnGroundTime -= delta;
        LastPressedJumpTime -= delta;
        LastPressedDashTime -= delta;
        AttackCd -= delta;
        #endregion

        #region INPUT HANDLER
        if (!_isAllInputDisabled)
        {
            _moveInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

            if (_moveInput == Vector2.Zero)
                _moveInput = _joyStick.GetDirection();

            if (!_isActionInputDisabled)
            {
                if (Input.IsActionJustPressed("jump"))
                    OnJumpInput();

                if (Input.IsActionJustReleased("jump"))
                    OnJumpUpInput();

                if (Input.IsActionJustPressed("dash"))
                    OnDashInput();

                if (Input.IsActionJustPressed("attack"))
                    OnAttackInput();
            }
        }
        else
            _moveInput = Vector2.Zero;
        #endregion

        // Ground Check
        if (IsOnFloor() && !IsJumping && !IsDashing)
            LastOnGroundTime = Data.CoyoteTime;

        #region DIRECTION
        if (_moveInput.X != 0)
        {
            HandleAnimation("forward");
            CheckDirectionToFace(isMovingRight: _moveInput.X > 0);
        }
        else
        {
            HandleAnimation("neutral");
        }
        #endregion

        #region JUMP CHECKS
        // jump falling
        if (IsJumping && Velocity.Y >= 0)
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
        AttackCheck();
        AudioCheck();
    }

    #region AUDIO
    void AudioCheck()
    { // audio jump
        if (IsOnFloor() && Input.IsActionJustPressed("jump"))
        {
            AudioStreamPlayerJump.Play();
        }
        // audio run
        if (
            Input.IsActionJustPressed("ui_left")
            || Input.IsActionJustPressed("ui_right") && IsOnFloor()
        )
        {
            AudioStreamPlayerRunning.Play();
        }

        if (
            Input.IsActionJustReleased("ui_left")
            || Input.IsActionJustReleased("ui_right") && IsOnFloor()
        )
        {
            AudioStreamPlayerRunning.Stop();
        }
    }
    #endregion

    #region INPUT CALLBACK
    private void OnJumpUpInput()
    {
        if (CanJumpCut())
            _isJumpCut = true;
    }

    private void OnDashInput()
    {
        LastPressedDashTime = Data.DashInputBufferTime;
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

        if (!IsDashing) // if not dashing then player can run, the horizontal velocity of player at the end of the dash when player can regain some control is reduce by half
            runLerp = 1;
        else if (isDashAttacking)
            runLerp = Data.dashEndRunLerp;
        else
            runLerp = 0;

        v.X += CalculateRunForce(runLerp, _moveInput.X) * delta;
        if (LastOnGroundTime > 0 && IsAttacking) // stop the player momentum when attaking on ground
            v.X += CalculateRunForce(runLerp, 0) * delta;
        if (_isRecoiling)
            v.X = Velocity.X;

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

        if (Mathf.Round(Velocity.X) != 0)
            EmitRunningDust();
    }

    // Velocity METHODS
    #region RUN METHODS
    float CalculateRunForce(float lerpAmount, float direction)
    {
        // calculate direction
        float targetSpeed = direction * Data.RunMaxSpeed;

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
        CharacterSprite.FlipH = !CharacterSprite.FlipH;
        IsFacingRight = !IsFacingRight;
    }
    #endregion


    #region ANIMATION METHOD

    void ResetAllPlayingAnimationCheck()
    {
        isDashAnimationPlaying = false;
        isDieAnimationPlaying = false;
        isHurtAnimationPlaying = false;
        isAttackAnimationPlaying = false;
        isGroundedAnimationPlaying = false;
    }

    void StretchSprite(Vector2 scaleAddend)
    {
        Tween tween = GetTree().CreateTween();
        Vector2 stretchScale = defaultScale + scaleAddend;
        tween
            .TweenProperty(CharacterSprite, "scale", stretchScale, 0.1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    void SquatchSprite(Vector2 scaleAddend)
    {
        Tween tween = GetTree().CreateTween();
        Vector2 squashScale = defaultScale + scaleAddend;
        tween
            .TweenProperty(CharacterSprite, "scale", squashScale, 0.1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    void ScaleSpriteBackToDefault()
    {
        Tween tween = GetTree().CreateTween();
        tween
            .TweenProperty(CharacterSprite, "scale", defaultScale, 0.01)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    // ! this is a mess
    private void HandleAnimation(string category)
    {
        const string NEUTRAL = "neutral";
        string animName = CharacterSprite.Animation;
        bool isJumpNeutralAnimation = jumpNeutralRegex.IsMatch(animName);
        bool isJumpForwardAnimation = jumpForwardRegex.IsMatch(animName);

        // jump related aninamtion need to use the same type of animation through out its life cycle. If it start with jump_forward_up it need to end in jump_forward_down
        string jumpCategory = isJumpNeutralAnimation
            ? NEUTRAL
            : isJumpForwardAnimation
                ? "forward"
                : category;

        isAttackAnimationPlaying =
            CharacterSprite.IsPlaying() && attackRegex.IsMatch(CharacterSprite.Animation);
        isHurtAnimationPlaying = CharacterSprite.IsPlaying() && CharacterSprite.Animation == "hurt";
        isDieAnimationPlaying = CharacterSprite.IsPlaying() && CharacterSprite.Animation == "die";

        string jumpUp = $"jump_{jumpCategory}_up";
        string jumpDown = $"jump_{jumpCategory}_down";
        if (
            isAttackAnimationPlaying
            || isHurtAnimationPlaying
            || isDieAnimationPlaying
            || isDashAnimationPlaying
            || isGroundedAnimationPlaying
        ) // dont interupt these animation
            return;

        // if attacking and the current animation is not of type attack then play the animation
        if (IsAttacking)
        {
            if (CharacterSprite.Scale != defaultScale)
                ScaleSpriteBackToDefault();
            CharacterSprite.Play("attack2");
            if (CharacterSprite.FlipH)
                attackFxLeft.Play("AttackFx");
            else
                attackFxRight.Play("AttackFx");
        }
        else if (IsJumping)
        {
            if (jumpUpRegex.IsMatch(animName))
                return;
            CharacterSprite.Play(jumpUp);
            if (jumpCategory == NEUTRAL)
                SquatchSprite(AnimationData.NeutralSquashScaleAddend);
            else
                SquatchSprite(AnimationData.ForwardSquashScaleAddend);
        }
        else if (IsFalling)
        {
            if (jumpDownRegex.IsMatch(animName))
                return;
            CharacterSprite.Play(jumpDown);
            if (jumpCategory == NEUTRAL)
                StretchSprite(AnimationData.NeutralStretchScaleAddend);
            else
                StretchSprite(AnimationData.ForwardStretchScaleAddend);
        }
        // play grounded animation
        else if ((jumpDownRegex.IsMatch(animName) || jumpUpRegex.IsMatch(animName)) && IsOnFloor())
        {
            CharacterSprite.Play($"jump_{jumpCategory}_grounded");
            isGroundedAnimationPlaying = true;
        }
        else if (!isGroundedAnimationPlaying)
        {
            CharacterSprite.Play($"{category}");
            if (CharacterSprite.Scale != defaultScale)
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
        // decrease force when the player is already moving upward, help with recoil
        else if (Velocity.Y < 0)
            force -= Velocity.Y;
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
        if (
            !IsDashing
            && dashesLeft < Data.dashAmount
            && LastOnGroundTime > 0
            && !dashRefilling
            && !IsAttacking
        ) // refil dash
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

            IsDashing = true;
            IsJumping = false;
            _isJumpCut = false;

            lastDashDir = IsFacingRight ? Vector2.Right : Vector2.Left;

            Timing.RunCoroutine(Dash(lastDashDir), Segment.PhysicsProcess, "StartDash");
        }
    }
    #endregion

    #region DASH METHODS
    private IEnumerator<double> Dash(Vector2 dir)
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

        CharacterSprite.Play("start_dash");
        dashAudio.Play();
        isDashAnimationPlaying = true;
        //We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
        while (Time.GetTicksMsec() - startTime <= Data.dashAttackTime * 1000)
        {
            Velocity = Data.dashSpeed * (float)localTimeScale * dir;
            //Pauses the loop until the next frame, creating something of a Update loop.
            //This is a cleaner implementation opposed to multiple timers and this coroutine approach is actually what is used in Celeste :D
            yield return Timing.WaitForOneFrame;
        }
        CharacterSprite.Play("end_dash");

        startTime = Time.GetTicksMsec();

        isDashAttacking = false;

        //Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
        SetGravityScale(Data.GravityScale);
        Velocity = Data.dashEndSpeed * (float)localTimeScale * dir;

        while (Time.GetTicksMsec() - startTime <= Data.dashEndTime * 1000)
        {
            yield return Timing.WaitForOneFrame;
        }

        //Dash over
        IsDashing = false;
    }

    void KillDash()
    {
        isDashAttacking = false;
        IsDashing = false;
        Velocity = Vector2.Zero;
        SetGravityScale(Data.GravityScale);
        Timing.KillCoroutines("StartDash");
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
            var _ghost = _dashGhostTscn.Instantiate<Sprite2D>();
            _ghost.Scale = CharacterSprite.Scale;
            _ghost.Texture = CharacterSprite.SpriteFrames.GetFrameTexture(
                CharacterSprite.Animation,
                CharacterSprite.Frame
            );

            _ghost.FlipH = CharacterSprite.FlipH;
            _ghost.GlobalPosition = CharacterSprite.GlobalPosition;
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

    bool IsCurrentFrame(params int[] frames)
    {
        return frames.Contains(CharacterSprite.Frame);
    }

    void SharpStopAnyMovement()
    {
        _moveInput = Vector2.Zero;
    }

    private void SetGravityScale(float gravityScale)
    {
        float globalGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        playerGravity = globalGravity * gravityScale;
    }

    private void Sleep(float duration)
    {
        //Method used so we don't need to call StartCoroutine everywhere
        Timing.RunCoroutine(_PerformSleep(duration));
    }

    private IEnumerator<double> _PerformSleep(float duration)
    {
        Engine.TimeScale = 0.05f;
        yield return Timing.WaitForSeconds(duration * duration);
        Engine.TimeScale = 1;
    }

    #endregion

    #region  COMBAT
    bool _isInvincible;

    [Export]
    private float trauma;

    void OnAttackInput()
    {
        if (AttackCd <= 0)
            IsAttacking = true;
        _moveInput = Vector2.Zero;
    }

    void StartInvinciblePeriod(int hurtFlashLoop = 6)
    {
        _isInvincible = true;
        var tween = GetTree().CreateTween().SetLoops(hurtFlashLoop);
        tween
            .TweenProperty(
                CharacterSprite.Material,
                "shader_parameter/flash_opacity",
                1,
                Data.InvinciblePeriod / (hurtFlashLoop * 2)
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                CharacterSprite.Material,
                "shader_parameter/flash_opacity",
                0,
                Data.InvinciblePeriod / (hurtFlashLoop * 2)
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);

        Timing.CallDelayed(
            Data.InvinciblePeriod,
            () =>
            {
                _isInvincible = false;
            }
        );
    }

    private IEnumerator<double> _FrameFreezeNZoom(float timeScale, float duration)
    {
        Node pcam = GetParent().GetNode("PhantomCamera2D");
        Engine.TimeScale = timeScale;
        pcam.Call("set_tween_duration", timeScale * duration);
        pcam.Call("set_priority", 0);
        yield return Timing.WaitForSeconds(duration * timeScale);
        pcam.Call("set_tween_duration", duration);
        pcam.Call("set_priority", 10);
        Engine.TimeScale = 1;
    }

    void AttackCheck()
    {
        if (IsDashing)
            return;

        IsStriking = false; // this is the state where attack frames are allow to connect with the enemy body

        if (isAttackAnimationPlaying)
            _isActionInputDisabled = true;
        else
            _isActionInputDisabled = false;

        if (isAttackAnimationPlaying && !IsAtkConnected) // check for enabling the "dealing-damage" frame
        {
            IsStriking = true;
        }
        else
        {
            IsStriking = false;
        }

        if (IsStriking)
        {
            hitAudio.Play();
            if (CharacterSprite.FlipH)
                Attack(hitboxLeft);
            else
                Attack(hitboxRight);
        }
    }

    void Attack(Area2D hitbox)
    {
        var bodies = hitbox.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is IHurtableBody)
            {
                IsAtkConnected = OnAtkConnected(body);
                if (IsAtkConnected)
                {
                    Recoil(
                        isRecoilRight: CharacterSprite.FlipH,
                        offsetX: Data.RecoilOffsetX,
                        offsetY: 0,
                        durationX: Data.RecoilDurationXSecond,
                        durationY: 0
                    );
                    ImpactHitFx(globalPos: body.GlobalPosition);
                }
            }
        }
    }

    void ImpactHitFx(Vector2 globalPos, float? duration = null)
    {
        var impactHit = _impactHitTscn.Instantiate<ImpactHit>();
        GetTree().Root.AddChild(impactHit);
        impactHit.GlobalPosition = globalPos;
        impactHit.OnCriticalFrame += () =>
            Timing.RunCoroutine(
                _FrameFreezeNZoom(Data.FreezeScale, duration ?? Data.FreezeDuration)
            );
    }

    bool OnAtkConnected(Node2D body)
    {
        if (body is IHurtableBody enemy)
        {
            return enemy.Hurt(Data.Damage);
        }

        return false;
    }

    void Recoil(
        bool isRecoilRight,
        float offsetX = 1,
        float offsetY = 0,
        float durationX = 1,
        float durationY = 0.5f
    )
    {
        _isRecoiling = true;
        _isAllInputDisabled = true;

        var directionX = isRecoilRight ? 1 : -1;
        float directionY = -1;
        var targetPosition =
            GlobalPosition + new Vector2(directionX * offsetX, directionY * offsetY);
        var distanceX = targetPosition.X - GlobalPosition.X;
        var distanceY = targetPosition.Y - GlobalPosition.Y;
        var targetVelocity = new Vector2(
            durationX == 0 ? 0 : distanceX / durationX,
            durationY == 0 ? 0 : distanceY / durationY
        );

        var tween = GetTree().CreateTween();

        IEnumerator<double> _CheckToCancelTween()
        {
            while (tween.IsValid())
            {
                if (LastPressedJumpTime > 0 || _moveInput != Vector2.Zero)
                {
                    tween.Kill();
                    Velocity = Vector2.Zero;
                    LastOnGroundTime = Data.CoyoteTime;
                }
                yield return Timing.WaitForOneFrame;
            }
        }

        tween.SetParallel(true).SetProcessMode(Tween.TweenProcessMode.Physics);
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
            .TweenCallback(
                Callable.From(() =>
                {
                    _isRecoiling = false;
                    _isAllInputDisabled = false;
                    Timing.RunCoroutine(_CheckToCancelTween());
                })
            );
        tween
            .TweenProperty(this, "velocity:x", 0, durationX)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        tween.SetParallel(false);
    }

    public bool Hurt(int _dmg)
    {
        if (_isInvincible)
            return false;

        if (_health <= 0)
            return false;

        hurtAudio.Play();

        _health -= _dmg;
        StartInvinciblePeriod();

        KillDash();

        ResetAllPlayingAnimationCheck();
        CharacterSprite.Play("hurt");

        EmitSignal(SignalName.HealthChanged, _health);

        Recoil(
            isRecoilRight: CharacterSprite.FlipH,
            offsetX: Data.RecoilOffsetX,
            offsetY: Data.RecoilOffsetY,
            durationX: Data.RecoilDurationXSecond,
            durationY: Data.RecoilDurationYSecond
        );
        ImpactHitFx(globalPos: GlobalPosition);

        if (_health <= 0)
        {
            Timing.RunCoroutine(_Die().CancelWith(this));
        }

        return true;
    }

    IEnumerator<double> _Die()
    {
        // I need to wait till the body is on the floor and velocity is equal to zero and wait for attack animation to finish
        _isAllInputDisabled = true;
        while (!IsOnFloor())
        {
            yield return Timing.WaitForOneFrame;
        }

        ResetAllPlayingAnimationCheck();
        // I stop all the process
        SetPhysicsProcess(false);
        SetProcess(false);

        // Then I play the die animation
        CharacterSprite.Play("die");
        _isAllInputDisabled = false;
    }
    #endregion
}
