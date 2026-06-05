using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal.Filters;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlayerLocomotionFSM : FiniteStateMachine, IKnockbackable
{
    public PlayerAnimation p_anim => Player.Instance._playerAnimation;
    public PlayerInputScript p_input => Player.Instance._playerInputScript;
    public PlayerCombat p_combat => Player.Instance._playerCombat;
    public string curr_state => CurrentState?.GetType().Name.Replace("PlayerLocomotion", "").Replace("State", "") ?? "null";   // Used to make the code cleaner
    public string next_State => NextState?.GetType().Name.Replace("PlayerLocomotion", "").Replace("State", "") ?? "null";      // Used to make the code cleaner

    public enum SurfaceToStick { Ground, Water }


    [Header("STATUS")]
    public string CurrentStateName;
    public Vector2 inputDir;
    public float verticalVelocity = 0f;
    public float MaxSpeed;
    public float turnSpeed;
    public bool casthit = false;
    public bool stickToGround = false;
    public bool inWater = false;
    public bool overridingInput = false;
    public bool overridingMovementDirection = false;
    public bool hitstopActive = false;
    private Vector3 normal;
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public Vector3 overrideMovementDirection;
    [HideInInspector] public Vector3 overrideInputDirection;
    [HideInInspector] public Vector3 SurfaceAppliedDir;
    [HideInInspector] public Vector3 NewHorizontalVel;
    [HideInInspector] public Vector3 targetVel;


    [Header("TIMER")]
    public Timer recoveryTimer = new Timer();
    public float recoveryDuration = 0.3f;
    public float heavyRecoveryDuration = 1.5f;

    [Header("Counter Confirm")]
    public float counterConfirmDuration = 0.08f;
    [Range(0f, 1f)] public float counterConfirmTimeScale = 0.03f;
    public float counterConfirmTimeBlend = 0.02f;


    [Header("GROUNDING")]
    public SurfaceToStick desiredSurfaceStick = SurfaceToStick.Ground;
    public float groundRayDist;
    [Tooltip("Something small like .3")]public float sphereRadius;
    [Tooltip("Around .2 less than groundRayDist is good")]public float MaxGroundStickDistance;
    [Tooltip("Make it the opposite of the model's y offset AND make sure collider is above feet level")]public float groundOffset = .1f;
    public float waterOffset = 0f;
    public RaycastHit Surface;
    public RaycastHit waterRayHit;
    public Transform waterRayCastOrigin;
    public float waterRayDist;
    [Tooltip("How deep we must be in the water to start swimming")]public float waterSwimThreshold = .701f;


    [Header("MOVEMENT")]
    public float RunningMaxSpeed;
    public float RunningTurnSpeed;
    public float acceleration;
    public float deceleration;
    public float gravity;
    [Space]
    public float blockingMaxSpeed;
    public float blockingTurnSpeed;
    [Space]
    public float dodgeDistance = 4f;
    public float dodgeDuration = 0.4f;
    public float dodgeAfterRollDuration = 0.2f;
    [Space]
    public float swimmingMaxSpeed;
    public float swimmingTurnSpeed;
    public float swimmingModelOffset = -.33f;
    [Space]
    public float attackDashDistance = 15f;
    public float dashDuration = 0.1f;
    [Space]
    public LayerMask nonWallLayer; // Need this so that if we collide with a trigger collider, the wallcheck is skipped


    [Header("REFERENCES")]
    public Transform orientation;
    [HideInInspector]public Rigidbody rb;
    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public bool swim = false;
    public PerfectDodgeRingFX perfectDodgeRingFX;
    public Transform LandingVFXAnchor;
    

    void Awake()
    {
        AddState("idle", new PlayerLocomotionIdleState(this));
        AddState("regular", new PlayerLocomotionRegularState(this));
        AddState("dodge", new PlayerLocomotionDodgeState(this));
        AddState("block", new PlayerLocomotionBlockState(this));
        AddState("blockedKnockback", new PlayerLocomotionBlockedKnockbackState(this));
        AddState("blockedRecovery", new PlayerLocomotionBlockedRecoveryState(this));
        AddState("counterConfirm", new PlayerLocomotionCounterConfirmState(this));
        AddState("dash", new PlayerLocomotionDashState(this));
        AddState("knockback", new PlayerLocomotionKnockbackState(this));
        AddState("recovery", new PlayerLocomotionRecoveryState(this));
        AddState("swim", new PlayerLocomotionSwimState(this));
        AddState("aim", new PlayerLocomotionAimState(this));

        rb = GetComponent<Rigidbody>();
        overridingInput = false;
        overridingMovementDirection = false;
        hitstopActive = false;
        MaxSpeed = RunningMaxSpeed;
        turnSpeed = RunningTurnSpeed;
        // desiredSurfaceStick = SurfaceToStick.Ground;
        perfectDodgeRingFX.gameObject.SetActive(true);

        // SaveSystem.DeleteSave();
    }
    void Start()
    {        
        GoToState("regular");
    }

    protected override void Update()
    {
        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) return;


        CurrentStateName = curr_state;
        if (!IsInState<PlayerLocomotionSwimState>() && !IsInState<PlayerLocomotionIdleState>()) UpdateGroundingStatus(); // Don't update grounding if swimming

        /* Hitstop can be applied in any state, so we handle it here
         * Making a state for it is not the best option because if we apply hitstop during attack
         * we interrupt the attack state, and we don't want that. we just want to pause it briefly.
         * If we do make a hitstop state, we'd have to store the previous state to return to it after hitstop ends.
         * Not only that, but we'd have to find a way to begin the state where it left off, which can add unnecessary complexity.
         */
        if (hitstopActive) // Might switch this for switch statement later if we add more global effects
        {
            CurrentStateName += " (Hitstop)";
            if (HitstopEnded()) base.Update(); // Need to update once after hitstop ends to properly resume state. If we don't, we can be a frame late
            return;
        }

        base.Update();
    }
    public void UpdateGroundingStatus()
    {
        // Get ground info
        casthit = Physics.SphereCast(transform.position + Vector3.up, sphereRadius, Vector3.down, out Surface, groundRayDist, groundLayer, QueryTriggerInteraction.Ignore);
        Surface = casthit ? Surface : default;

        // Determine if we should stick to the ground
        stickToGround = false;
        if (casthit)
        {
            float dist = Mathf.Abs(transform.position.y + 1f - Surface.point.y);
            stickToGround = dist < MaxGroundStickDistance;
        }

        // Swim check
        inWater = Physics.Raycast(waterRayCastOrigin.position, Vector3.down, out waterRayHit, waterRayDist, waterLayer, QueryTriggerInteraction.Collide);
        if (inWater)
        {
            float waterLevel = waterRayHit.point.y;
            float swimStartHeight = transform.position.y + waterSwimThreshold;

            if (waterLevel > swimStartHeight) GoToState("swim");
        }
    }

    public void MovePlayer()
    {
        moveDirection = GetMoveDirection();
        SurfaceAppliedDir = GetSlopeForward(true) * moveDirection.magnitude;

        // Calculate Horizontal Velocity
        Vector3 targetHorizontalVel = AllowMovement()? SurfaceAppliedDir * MaxSpeed : Vector3.zero; 
        float appropriateAcceleration = moveDirection != Vector3.zero ? acceleration : deceleration;
        float rad = turnSpeed * Mathf.PI * Time.deltaTime;
        Vector3 currHorizontalVel = Vector3.ProjectOnPlane(rb.linearVelocity, normal);
        NewHorizontalVel = Vector3.RotateTowards(currHorizontalVel, targetHorizontalVel, rad, appropriateAcceleration * Time.deltaTime);
        targetVel = NewHorizontalVel;

        // Calculate Vertical Velocity
        if (!stickToGround) { verticalVelocity -= gravity * Time.deltaTime; verticalVelocity = Mathf.Max(verticalVelocity, -35f);}
        else                { verticalVelocity = targetVel.y; SurfaceStick(); }
        targetVel.y = verticalVelocity;


        targetVel = WallSlideCheckAndAdjustment(SurfaceAppliedDir, targetVel);
        rb.linearVelocity = targetVel;
    }
    public Vector3 GetMoveDirection()
    {
        if (overridingMovementDirection) return overrideMovementDirection;

        Vector2 input = !overridingInput ? inputDir : overrideInputDirection;
        return (orientation.forward * input.y + orientation.right * input.x).normalized;
    }
    public Vector3 GetSlopeForward(bool useMoveDirection)
    {
        // If we're swimming, just use up vector
        normal = Surface.collider != null && !IsInState<PlayerLocomotionSwimState>()? Surface.normal : Vector3.up;
        Vector3 correctDir = useMoveDirection ? moveDirection : Player.Instance._playerAnimation.transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, correctDir).normalized;    // We only want to influence the vertical component, not the horizontal one
        Vector3 SlopeForward = Vector3.Cross(right, normal).normalized;
        return SlopeForward;
    }
    public bool AllowMovement()
    {
        return !Player.Instance.InAttack || IsInState<PlayerLocomotionDashState>(); // dash uses MovePlayer
    }
    public void SurfaceStick()
    {
        RaycastHit surface;
        float offset = desiredSurfaceStick == SurfaceToStick.Water ? waterOffset : groundOffset;

        // We use safegaurds in case UpdateGroundStatus isn't called during transitions, like from swim -> regular
        switch (desiredSurfaceStick)
        {
            case SurfaceToStick.Ground:
                if (Surface.collider == null) return;
                surface = Surface;
                break;

            case SurfaceToStick.Water:
                if (waterRayHit.collider == null) return;
                surface = waterRayHit;
                break;

            default:
                if (Surface.collider == null) return;
                surface = Surface;
                break;
        }
        
        // stick the player to the surface before applying the target velocity by moving them there
        Vector3 targetPos = new Vector3(transform.position.x, surface.point.y + offset, transform.position.z);
        transform.position = targetPos;
    }
    public Vector3 WallSlideCheckAndAdjustment(Vector3 DesiredDirection, Vector3 CalculatedDirection)
    {
        // Before we move the player, check if we hit a wall, and if we do, change the velocity vector so that it slides along the wall
        Vector3 nextPos = transform.position + CalculatedDirection * Time.deltaTime;
        float dist = Vector3.Distance(transform.position, nextPos);
        if (rb.SweepTest(CalculatedDirection.normalized, out RaycastHit hit, dist) && stickToGround)
        {
            if ((nonWallLayer.value & (1 << hit.collider.gameObject.layer)) != 0) // ignore trigger colliders
                return CalculatedDirection;

            // Only does this for walls, not slopes
            if (Vector3.Angle(Vector3.up, hit.normal) >= 90f)
            {
                Vector3 WallSlideDir = Vector3.ProjectOnPlane(DesiredDirection.normalized, hit.normal.normalized);

                // The more parallel to the wall the velocity is, the more speed the player will have
                float dot = Vector3.Dot(DesiredDirection.normalized, hit.normal.normalized);
                float slideMagnitude = MaxSpeed * (1 - Math.Abs(dot));

                return WallSlideDir * slideMagnitude;
            }
        }

        return CalculatedDirection;
    }


    public void MovePlayerByDelta(Vector3 delta, bool deltaIncludesAirMovement)
    {
        moveDirection = delta.normalized;

        // If delta includes air movement, then we don't want ground slope influence
        Vector3 slopeDelta = deltaIncludesAirMovement ? moveDirection : GetSlopeForward(true); // Using the GetSlope function messes with the intended vector
        float speed = delta.magnitude / Time.fixedDeltaTime;
        Vector3 vel = slopeDelta * speed;

        vel = WallSlideCheckAndAdjustment(slopeDelta, vel);
        
        rb.linearVelocity = vel;
        moveDirection = Vector3.zero;
    }

    // HELPERS ----------------------------------

    // movement overrides
    public void ResetVelocityFactors()
    {
        inputDir = Vector3.zero;
        moveDirection = Vector3.zero;
        targetVel = Vector3.zero;
        verticalVelocity = 0f;
        NewHorizontalVel = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }
    public void OverrideInputDirection(bool overrideI, Vector3 direction = default)
    {
        overridingInput = overrideI;
        if (overrideI)
            overrideInputDirection = direction;
    }
    public void OverrideMovementDirection(bool overrideM, Vector3 direction = default)
    {
        overridingMovementDirection = overrideM;
        if (overrideM)
            overrideMovementDirection = direction;
    }


     // Hitstop
    float hitstopTimer;
    Vector3 hitstopStoredCurrentVel, hitstopStoredMoveDir, hitstopStoredNewHorizontalVel, hitstopStoredTargetVel; float hitstopStoredVerticalVel;
    Action onHitstopEnd;
    public void ApplyHitstop(HitData hit, Action onFinished = null)
    {        
        // Debug.Log("Player applying Player Hitstop");
        if (hit.hitstop <= 0f)
        {
            Debug.LogWarning("Tried to apply hitstop with non-positive duration.");
            onFinished?.Invoke();
            return;
        }

        // Extend hitstop if already active
        if (hitstopActive)
        {
            // overwrite callback if knockback knockback was hard OR if there was no previous callback
            if (hit.effect == HitData.HitEffect.SendFlying) onHitstopEnd = onFinished;
            else if (onHitstopEnd == null) onHitstopEnd = onFinished ?? onHitstopEnd;

            hitstopTimer = Mathf.Max(hitstopTimer, hit.hitstop);
            return;
        }

        onHitstopEnd = onFinished ?? onHitstopEnd; // We don't want to set it to null if onFinished is null, because we might have a previous callback stored
        hitstopActive = true;
        hitstopTimer = hit.hitstop;

        // cache motion state
        hitstopStoredCurrentVel = rb.linearVelocity;
        hitstopStoredVerticalVel = verticalVelocity;
        hitstopStoredMoveDir = moveDirection;
        hitstopStoredNewHorizontalVel = NewHorizontalVel;
        hitstopStoredTargetVel = targetVel;

        // stop animation
        p_anim.animator.speed = 0f;

        ResetVelocityFactors();
    }
    public bool HitstopEnded()
    {
        hitstopTimer -= Time.deltaTime;
        if (hitstopTimer > 0f) return false;

        // Debug.Log("Player Hitstop ended after timer. Timer: " + hitstopTimer);
        hitstopActive = false;
        hitstopTimer = 0f;

        rb.linearVelocity = hitstopStoredCurrentVel;
        verticalVelocity = hitstopStoredVerticalVel;
        moveDirection = hitstopStoredMoveDir;
        NewHorizontalVel = hitstopStoredNewHorizontalVel;
        targetVel = hitstopStoredTargetVel;

        p_anim.animator.speed = 1f;

        onHitstopEnd?.Invoke();
        onHitstopEnd = null; // Clear callback after invoking to avoid repeated calls
        return true;
    }
    
    // Dash
    [HideInInspector] public Transform dashTarget; // Gets set by dodge state when we perfect dodge
    public void StartCounterConfirmFromPerfectDodge()
    {
        Player.Instance.CounterAttacking = true;
        GoToState("counterConfirm");
    }
    public void StartDashFromPerfectDodge()
    {
        GetState<PlayerLocomotionDashState>("dash").Configure(dashTarget);
        GoToState("dash");
    }


    // Knockback
    public void StartKnockback(Vector3 targetPos, float duration, HitData attackData, Transform attacker)
    {
        GetState<PlayerLocomotionKnockbackState>("knockback").Configure(transform.position, targetPos, duration, attackData, attacker);
        GoToState("knockback");
    }

    public void StartBlockedKnockback(Vector3 targetPos, float duration, HitData attackData, Transform attacker)
    {
        GetState<PlayerLocomotionBlockedKnockbackState>("blockedKnockback").Configure(transform.position, targetPos, duration, attackData, attacker);
        GoToState("blockedKnockback");
    }
    public void SetReleasedBlockDuringKnockback(bool released) => GetState<PlayerLocomotionBlockState>("block").blockReleasedDuringKnockback = released;

    // Recovery
    public void StartRecoveryFromKnockback(bool heavyRecovery)
    {
        GetState<PlayerLocomotionRecoveryState>("recovery").Configure(heavyRecovery);
        GoToState("recovery");
        // Debug.Log("next state: " + next_State);
    }

    public void StartRecoveryFromBlockedKnockback()
    {
        GoToState("blockedRecovery");
    }

    // Save & Load
    public IEnumerator TeleportPlayer(Vector3 position)
    {
        GoToState("idle");
        yield return null;

        stickToGround = false; 
        ResetVelocityFactors();
        rb.position = position; // need to use rb.position so physics step doesn't reposition player after teleporting

        yield return null;
        GoToState("regular");
    }

}


public class PlayerLocomotionRegularState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    public PlayerLocomotionRegularState(PlayerLocomotionFSM self) : base(self) { }
    public override void Enter() {}
    public override void Update() {}
    public void FixedUpdate() => m_self.MovePlayer();
    public override void Exit() {}
}

public class PlayerLocomotionDodgeState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    // Dodge lasts around .47 seconds
    public PlayerLocomotionDodgeState(PlayerLocomotionFSM self) : base(self) { }

    public enum DodgeState { roll, afterRoll, rest }
    DodgeState dodgeState = DodgeState.roll;
    float originalGroundRay;
    Vector2 dodgeInputDir;
    Vector3 dodgeDirection;
    Vector3 endPosition;
    float elapsedTime = 0f;
    float speed = 0f;
    int _dodgeToken;

    public override void Enter()
    {
        // Debug.Log("Entered Dodge");

        dodgeState = DodgeState.roll;
        originalGroundRay = m_self.groundRayDist;
        m_self.groundRayDist = 5f; // So we can stick to ground better during dodge

        dodgeInputDir = m_self.inputDir;
        m_self.OverrideInputDirection(true, dodgeInputDir);
        m_self.moveDirection = m_self.GetMoveDirection();

        dodgeDirection = m_self.GetSlopeForward(true) * m_self.dodgeDistance;
        endPosition = m_self.transform.position + (dodgeDirection.normalized * m_self.dodgeDistance);

        _dodgeToken = m_self.p_anim.TriggerTokenedAnim(
            key: "Dodge", 
            triggerName: "Dodge", 
            () => m_self.GoToState("regular")
        );

        if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.LockOn)
            m_self.p_anim.RotateModelTowardsDirectionInstant(endPosition);

        elapsedTime = 0f;
        AnimationEvents.Instance.DisableNonEssentialActions();

        if (Player.Instance.PerfectDodgeUnlocked) PerferctDodgeRoutine(); // Checks for perfect dodge and sets up if we do perfect dodge
    
        AudioManager.Instance.PlayDodge(m_self.transform);
    }

    void PerferctDodgeRoutine()
    {
        Player.Instance.PerfectDodgeActive = false;

        // Check for perfect dodge
        IWeaponHolder perfectDodgedWeaponHolder = CheckForPerfectDodge();
        // Debug.Log("Pefect dodged? --- " + perfectDodgedWeaponHolder);
        if (perfectDodgedWeaponHolder != null)
        {
            Player.Instance.PerfectDodgeActive = true;
            m_self.perfectDodgeRingFX.Play();
            AudioManager.Instance.PlayPerfectDodge(m_self.transform);
            AudioManager.Instance.DuckMusicForPerfectDodge();

            m_self.dashTarget = (perfectDodgedWeaponHolder as MonoBehaviour).transform;
            AnimationEvents.Instance.EnableAction("Attack"); // TODO: It's probably better to only enable attack after a certain point in the animation so that it looks good, or I guess maybe the input is fine, but only transition into the attack after a certain point.

            GameManager.Instance.SetTimeScale(0.15f, 0.1f); 
            GameManager.Instance.SetPerfectDodgeVolumeVignetteColor(Color.black);
            GameManager.Instance.SetPerfectDodgeVolumeWeight(1f, 0.1f);

            var anim = m_self.p_anim.animator;   // however you access Animator
            anim.ResetTrigger("Dodge"); // reset the original trigger so that we don't replay the same animation
            anim.CrossFade("Base Layer.Dodge.Dodges", .1f, 0, .25f);
            anim.Update(0f); // plays the dodge immediately
        }
    }
    IWeaponHolder CheckForPerfectDodge()
    {
        Collider[] hitColliders = Physics.OverlapSphere(m_self.transform.position, 2f);

        float closestDistance = float.MaxValue;
        IWeaponHolder closestWeapon = null;

        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject.TryGetComponent(out IWeaponHolder weaponHolder))
            {
                Weapon weapon = weaponHolder.GetWeapon();
                if (!weapon.canBePerfectDodged)
                    continue;

                float distance = Vector3.Distance(m_self.transform.position, collider.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWeapon =   weaponHolder;
                }
            }
        }

        if (closestWeapon != null) return closestWeapon;
        else                       return null;
    }

    public override void Update() {}

    public void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;

        switch(dodgeState)
        {
            case DodgeState.roll: Roll(); break;
            
            case DodgeState.afterRoll: AfterRoll(); break;

            case DodgeState.rest: break;
        }
    }

    public void Roll()
    {
        // Calculate Velocity
        speed = m_self.dodgeDistance / m_self.dodgeDuration;
        Vector3 surfaceAppiedDir = m_self.GetSlopeForward(true);
        Vector3 targetV = surfaceAppiedDir.normalized * speed;

        // Adjustments
        if (m_self.casthit) m_self.SurfaceStick();
        targetV = m_self.WallSlideCheckAndAdjustment(surfaceAppiedDir, targetV);
        
        m_self.rb.linearVelocity = targetV;

        if (elapsedTime >= m_self.dodgeDuration)
        {
            elapsedTime = 0f;
            m_self.OverrideInputDirection(false);
            AnimationEvents.Instance.EnableAction("Move");
            dodgeState = DodgeState.afterRoll;
        } 
    }

   public void AfterRoll()
    {
        // Calculate Velocity
        speed = Mathf.Lerp(speed, 0f, elapsedTime / m_self.dodgeAfterRollDuration);

        // GetSlopeForward needs moveDirection
        if (m_self.inputDir != Vector2.zero) // If player doesn't input, keep last direction
        {
            m_self.moveDirection = m_self.GetMoveDirection();
            dodgeInputDir = m_self.inputDir;
        }
        else
        {
            m_self.moveDirection = (m_self.orientation.forward * dodgeInputDir.y + m_self.orientation.right * dodgeInputDir.x).normalized;
        }
        Vector3 surfaceAppiedDir = m_self.GetSlopeForward(true);
        Vector3 targetV = surfaceAppiedDir.normalized * speed;


        // Adjustments
        if (m_self.casthit) m_self.SurfaceStick();
        targetV = m_self.WallSlideCheckAndAdjustment(surfaceAppiedDir, targetV);
        
        m_self.rb.linearVelocity = targetV;


        if (elapsedTime >= m_self.dodgeAfterRollDuration)
            dodgeState = DodgeState.rest;
    }

    public override void Exit() 
    {
        // Debug.Log("Exiting Dodge");
        m_self.groundRayDist = originalGroundRay;
        m_self.rb.linearVelocity = Vector3.zero;        
        m_self.OverrideInputDirection(false);
        AnimationEvents.Instance.EnableActionMap();
        m_self.p_anim.Cancel("Dodge", _dodgeToken);
        
        if (Player.Instance.PerfectDodgeActive)
        {   
            if (!Player.Instance.CounterAttacking)
            {
                m_self.dashTarget = null;
                GameManager.Instance.SetTimeScale(1f, .15f); // Ensure time is back to normal in case we left it slowed down from perfect dodge
                m_self.perfectDodgeRingFX.StopNow();
                GameManager.Instance.SetPerfectDodgeVolumeWeight(0f, 0.1f); // Smoothly decrease perfect dodge volume weight
                AudioManager.Instance.FadeOutPerfectDodge();
                AudioManager.Instance.RestoreMusicAfterPerfectDodge();
                
            }
        }

        Player.Instance.PerfectDodgeActive = false;
    }

}

public class PlayerLocomotionCounterConfirmState : State<PlayerLocomotionFSM>
{
    public PlayerLocomotionCounterConfirmState(PlayerLocomotionFSM self) : base(self) { }

    private float timer;

    public override void Enter()
    {
        timer = 0f;
        // m_self.perfectDodgeRingFX.StopNow();
        m_self.perfectDodgeRingFX.PlayCounterShine();
        AudioManager.Instance.PlayCounterConfirm(m_self.transform);

        // Push the perfect dodge presentation into the counter confirm feel
        // GameManager.Instance.SetPerfectDodgeVolumeVignetteColor(Color.white, 0.05f);

        GameManager.Instance.SetTimeScale(m_self.counterConfirmTimeScale, m_self.counterConfirmTimeBlend);
        GameManager.Instance.SetPerfectDodgeVolumeVignetteIntensity(.37f, 0.2f); // Smoothly decrease perfect dodge volume weight

        AnimationEvents.Instance.DisableNonEssentialActions();
    }

    public override void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= m_self.counterConfirmDuration)
        {
            AudioManager.Instance.FadeOutPerfectDodge();
            m_self.StartDashFromPerfectDodge();
        }
    }

    public override void Exit()
    {
        // Safety restore
        GameManager.Instance.SetTimeScale(1f, 0.02f);
        GameManager.Instance.SetPerfectDodgeVolumeVignetteIntensity(.33f, 0.15f);    // default = .33
        GameManager.Instance.SetPerfectDodgeVolumeRadialBlursmoothness(0.4f, 0.15f); // default = 0.4f
        // m_self.perfectDodgeRingFX.StopCounterShineNow();
    }
}

public class PlayerLocomotionDashState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    public PlayerLocomotionDashState(PlayerLocomotionFSM self) : base(self) { }

    Vector3 endPosition;
    float elapsedTime = 0f;
    float prevAccel;
    float prevTurnSpeed;
    Transform dashTargetTransform = null;
    float offset;
    bool timeUp = false, reachedEnd = false;
    bool counterAttacking => Player.Instance.CounterAttacking;
    WeaponData weaponTemplate => m_self.p_combat.GetWeapon()?.weaponTemplate;
    public void Configure(Transform dashTarget) => dashTargetTransform = dashTarget;


    public override void Enter()
    {
        if (!counterAttacking)
        {
            dashTargetTransform = ((MonoBehaviour)m_self.p_input.dashTarget).transform;
        }
        else
        {
            GameManager.Instance.SetPerfectDodgeVolumeVignetteColor(Color.white, 0.15f); // Change vignette color to red to indicate counter attack dash
            AudioManager.Instance.RestoreMusicAfterPerfectDodge();
        }

        m_self.p_anim.TriggerDash();
        if (counterAttacking) m_self.p_anim.TriggerCounterAttack();
        m_self.p_anim.SetLockOnForward(dashTargetTransform);
        m_self.p_anim.RotateModelInstant();

        // calculate offset
        float dist = Vector3.Distance(m_self.rb.position, dashTargetTransform.position);
        float weaponOffset = counterAttacking ? weaponTemplate.CounterAttackDashStopOffset : weaponTemplate.DashStopOffset;
        offset = Mathf.Min(weaponOffset, Mathf.Max(0f, dist - 0.01f));
        
        // set dash parameters
        prevAccel = m_self.acceleration;
        prevTurnSpeed = m_self.turnSpeed;
        m_self.acceleration = Mathf.Infinity; // instant acceleration & turn speed
        m_self.turnSpeed = Mathf.Infinity;
        m_self.MaxSpeed = counterAttacking ? weaponTemplate.counterAttackDashSpeed : weaponTemplate.dashSpeed;
        elapsedTime = 0f;
        timeUp = false;
    }

    public override void Update()
    {
        if (reachedEnd) return;

        elapsedTime += Time.deltaTime;
        timeUp = elapsedTime >= m_self.dashDuration;
        
        if (timeUp) 
        {
            // Debug.Log("time's up!");
            m_self.GoToState("regular");
        }
    }
    public void FixedUpdate()
    {
        if (timeUp) return;
        if (dashTargetTransform == null && !counterAttacking)
        {
            m_self.ResetVelocityFactors();
            m_self.GoToState("regular");
            return;
        }

        // Adjustment for counterattack animation. We want to move it a bit to the left because the sword is a bit off
        Vector3 dashDir = (dashTargetTransform.position - m_self.rb.position).normalized;
        Vector3 left = Vector3.Cross(Vector3.up, dashDir).normalized;
        float sideOffset = counterAttacking ? weaponTemplate.CounterAttackSideOffset : 0f;
        
        endPosition = dashTargetTransform.position - dashDir * offset + left * sideOffset;

        float dist = Vector3.Distance(m_self.rb.position, endPosition);
        float maxStep = m_self.MaxSpeed * Time.fixedDeltaTime;     // Make sure that we won't overshoot in case of high speeds
        reachedEnd = dist <= Mathf.Max(offset, maxStep + 0.05f);    // 1f is stop offset. Also, this is to prevent overshooting

        if (reachedEnd)
        {
            endPosition.y = m_self.rb.position.y; // prevent y popping
            m_self.rb.MovePosition(endPosition);  // ensure we reach the exact position in case we over/undershot a bit
            m_self.ResetVelocityFactors(); // Makes sure we don't keep moving before reaching exit. This fixes the frame-timing issue.
            m_self.GoToState("regular");
            return;
        }

        Vector3 dir = (endPosition - m_self.rb.position).normalized;
        m_self.OverrideMovementDirection(true, dir);

        m_self.MovePlayer();
    }

    public override void Exit()
    {
        dashTargetTransform = null;
        m_self.MaxSpeed = m_self.RunningMaxSpeed;
        m_self.acceleration = prevAccel;
        m_self.turnSpeed = prevTurnSpeed;
        m_self.OverrideMovementDirection(false);
        m_self.ResetVelocityFactors();

        if (counterAttacking)
        {
            if (GameManager.Instance.GetTimeScale() != 1f) GameManager.Instance.SetTimeScale(1f);

            GameManager.Instance.SetPerfectDodgeVolumeWeight(0f, 0.1f); 
            Player.Instance.CounterAttacking = false;
            m_self.perfectDodgeRingFX.StopNow();
            m_self.perfectDodgeRingFX.StopCounterShineNow();

        }
        else if (!counterAttacking) m_self.p_anim.TriggerDashExit();
    }
}

public class PlayerLocomotionBlockState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    public PlayerLocomotionBlockState(PlayerLocomotionFSM self) : base(self) { }

    public bool blockReleasedDuringKnockback = false;
    Coroutine C_upperBodyLayerSet;
    public override void Enter()
    {
        AnimationEvents.Instance.EnableActionMap();

        if (blockReleasedDuringKnockback || !m_self.p_input.blockInputHeld) 
        {
            blockReleasedDuringKnockback = false;
            m_self.GoToState("regular");
            return;
        }

        m_self.MaxSpeed = m_self.blockingMaxSpeed;
        m_self.turnSpeed = m_self.blockingTurnSpeed; 

        m_self.p_anim.MaxSpeed = 3.2f;

        StopUpperBodyLayerSetCoroutine();
        C_upperBodyLayerSet = m_self.p_anim.StartCoroutine(m_self.p_anim.SetLayerWeightSmoothly(1f, 0.1f, 1));    
    }

    public override void Update() { }
    public void FixedUpdate() => m_self.MovePlayer();

    public override void Exit()
    {
        m_self.MaxSpeed = m_self.RunningMaxSpeed;
        m_self.turnSpeed = m_self.RunningTurnSpeed;

        m_self.p_anim.MaxSpeed = m_self.RunningMaxSpeed;

        float targetWeight = m_self.next_State.Contains("Block") ? 1f : 0f;
        
        StopUpperBodyLayerSetCoroutine();
        m_self.p_anim.StartCoroutine(m_self.p_anim.SetLayerWeightSmoothly(targetWeight, 0.1f, 1));

    }

    public void StopUpperBodyLayerSetCoroutine()
    {
        if (C_upperBodyLayerSet != null)
        {
            m_self.p_anim.StopCoroutine(C_upperBodyLayerSet);
            C_upperBodyLayerSet = null;
        }
    }
}

public class PlayerLocomotionBlockedKnockbackState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    public PlayerLocomotionBlockedKnockbackState(PlayerLocomotionFSM self) : base(self) { }


    public HitData attack;
    private Vector3 start;
    private Vector3 target;
    private float timer;
    private float duration;
    private Vector3 modelFaceDirection;



    // Configure knockback before we switch into it.
    public void Configure(Vector3 startPos, Vector3 endPos, float duration, HitData attackData, Transform attacker)
    {
        this.start = startPos;
        this.target = endPos;
        this.duration = Mathf.Max(duration, 0.001f); // avoid div-by-zero
        this.attack = attackData;
        this.modelFaceDirection = GetModelFaceDirection(startPos, endPos);

        // Debug.Log("Player Configured Blocked Knockback");
    }

    public override void Enter()
    {
        // Got hit disables non essential actions already
        RotateModelForKnockback();
        m_self.ResetVelocityFactors();
        timer = 0f;
    }

    public override void Update(){}
    public void FixedUpdate()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        Vector3 desiredPos = Vector3.Lerp(start, target, Mathf.Clamp01(t));

        Vector3 baseKnockbackDelta = desiredPos - m_self.transform.position;
        m_self.MovePlayerByDelta(baseKnockbackDelta, attack.effect == HitData.HitEffect.SendFlying);

        if (t >= 1f) m_self.StartRecoveryFromBlockedKnockback();
    }

    public override void Exit()
    {
        // Debug.Log("Exiting blocked knockback");
        float dur = attack.effect == HitData.HitEffect.SendFlying ? m_self.heavyRecoveryDuration : m_self.recoveryDuration;
        m_self.recoveryTimer.duration = dur;

        float targetWeight = m_self.next_State.Contains("Block") ? 1f : 0f;
        m_self.p_anim.animator.SetLayerWeight(1, targetWeight);
    }

    private Vector3 GetModelFaceDirection(Vector3 startPos, Vector3 endPos)
    {
        Vector3 knockbackDirection = endPos - startPos;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return -knockbackDirection.normalized;
    }

    private void RotateModelForKnockback()
    {
        if (modelFaceDirection.sqrMagnitude < 0.0001f)
            return;

        m_self.p_anim.RotateModelTowardsDirectionInstant(m_self.transform.position + modelFaceDirection);
    }
}

public class PlayerLocomotionBlockedRecoveryState : State<PlayerLocomotionFSM>
{
    public PlayerLocomotionBlockedRecoveryState(PlayerLocomotionFSM self) : base(self) { }

    private int _recoveryToken;
    public bool heavyRecovery = false;


    public override void Enter()
    {
        m_self.recoveryTimer.Setup(); 
    }

    public override void Update()
    {
        if (!m_self.recoveryTimer.isSetup) return;

        m_self.recoveryTimer.Tick();
        if (m_self.stickToGround) m_self.SurfaceStick();
        if (m_self.recoveryTimer.Finished()) m_self.GoToState("block");
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Recovery");
        // AnimationEvents.Instance.EnableActionMap();
        m_self.p_anim.Cancel("Recovery", _recoveryToken);

        float targetWeight = m_self.next_State.Contains("Block") ? 1f : 0f;
        m_self.p_anim.animator.SetLayerWeight(1, targetWeight);
    }
}

public class PlayerLocomotionKnockbackState : State<PlayerLocomotionFSM>, IFixedUpdate
{
    public PlayerLocomotionKnockbackState(PlayerLocomotionFSM self) : base(self) { }


    public HitData attack;
    private Vector3 start;
    private Vector3 target;
    private float timer;
    private float duration;
    private Vector3 modelFaceDirection;
    Vector3 postArcVelocity; // used for send flying knockback to store the velocity after the arc ends so that we can continue moving the player if they haven't hit the ground yet



    // Configure knockback before we switch into it.
    public void Configure(Vector3 startPos, Vector3 endPos, float duration, HitData attackData, Transform attacker)
    {
        this.start = startPos;
        this.target = endPos;
        this.duration = Mathf.Max(duration, 0.001f); // avoid div-by-zero
        this.attack = attackData;
        this.modelFaceDirection = GetModelFaceDirection(startPos, endPos);

        // Debug.Log("Player Configured Knockback");
    }

    public override void Enter()
    {
        // Debug.Log("Entered Knockback. Disabling action map.");
        //  if (!attacker.TryGetComponent<INotModelForwardTarget>(out INotModelForwardTarget n))
        // {
        //     // Debug.Log("attacker is a valid Model Forward target");
        //     m_self.p_anim.SetLockOnForward(attacker);
        //     m_self.p_anim.RotateModelInstant();
        // }
        // else
        // {
        //     // Debug.Log("attacker is NOT a valid Model Forward target");
        // }
        RotateModelForKnockback();
        
        m_self.ResetVelocityFactors();
        AnimationEvents.Instance.DisableNonEssentialActions();
        // m_self.p_anim.TriggerCorrectKnockbackAnimation(attack);
        timer = 0f;
    }

    public override void Update(){}
    public void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        float t = timer / duration;

        Vector3 desiredPos = m_self.transform.position;

        switch (attack.effect)  // Knockback type specific movement
        {
            case HitData.HitEffect.Regular: // Linear push
            {
                desiredPos = Vector3.Lerp(start, target, Mathf.Clamp01(t));
                break;
            }

            case HitData.HitEffect.SendFlying: // Parabolic arc

                if (t <= 1f)
                {
                    desiredPos = Vector3.Lerp(start, target, t);
                    desiredPos.y += attack.SendFlyingHeight * 4f * t * (1 - t);

                    postArcVelocity = desiredPos - m_self.transform.position;

                    if (postArcVelocity.y >= 0f)
                        postArcVelocity.y = -m_self.gravity * Time.deltaTime;
                }
                else
                {
                    desiredPos = m_self.transform.position + postArcVelocity;
                }
                
                // I only have the full animation of send flying knockback, so I need to adjust the pose manually. Luckily, the animation matches t pretty well.
                float animT = Mathf.Clamp(t, 0f, 0.8f); // the animation "lands" right after .8f
                m_self.p_anim.animator.Play("SentFlyingKnockback", 0, animT); // POTENTIAL ISSUE: if knockback ends but we're not on the ground, then animation looks off
                break;
            
            case HitData.HitEffect.Stun:
                // No movement
                break;
        }


        Vector3 baseKnockbackDelta = desiredPos - m_self.transform.position;
        m_self.MovePlayerByDelta(baseKnockbackDelta, attack.effect == HitData.HitEffect.SendFlying);

        if (KnockbackFinished(t)) 
        {
            if (attack.effect == HitData.HitEffect.SendFlying)
            {
                GameManager.Instance.PlayHitGroundKnockbackVFX(m_self.LandingVFXAnchor.position, m_self.p_anim.transform);
            }

            if (Player.Instance.healthHasDepleted)
                Player.Instance.DeathRoutine();
            else
                m_self.StartRecoveryFromKnockback(heavyRecovery: attack.effect == HitData.HitEffect.SendFlying);
        }
    }

    public bool KnockbackFinished(float t)
    {
        switch (attack.effect)  // type specific exit conditions
        {
            case HitData.HitEffect.Regular:
                if (t >= 1f) return true;
                break;

            case HitData.HitEffect.SendFlying:
                if (t > .2f && m_self.stickToGround || (t > 1f && m_self.rb.linearVelocity.magnitude < .01f)) 
                {
                    // Debug.Log("Knockback finished by ground contact");
                    m_self.p_anim.animator.Play("SentFlyingKnockback", 0, .8f);
                    return true;
                }
                break;

            case HitData.HitEffect.Stun:
                break;
        }

        return false;
    }

    public override void Exit()
    {
        // Debug.Log("Exiting knockback");
        // Debug.Log("Next state: " + m_self.next_State);
        if (Player.Instance.healthHasDepleted && !Player.Instance.startedDeathRoutine)
            Player.Instance.DeathRoutine();

        float dur = attack.effect == HitData.HitEffect.SendFlying ? m_self.heavyRecoveryDuration : m_self.recoveryDuration;
        m_self.recoveryTimer.duration = dur;
    }

    private Vector3 GetModelFaceDirection(Vector3 startPos, Vector3 endPos)
    {
        Vector3 knockbackDirection = endPos - startPos;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return -knockbackDirection.normalized;
    }

    private void RotateModelForKnockback()
    {
        if (modelFaceDirection.sqrMagnitude < 0.0001f)
            return;

        m_self.p_anim.RotateModelTowardsDirectionInstant(m_self.transform.position + modelFaceDirection);
    }
}

public class PlayerLocomotionRecoveryState : State<PlayerLocomotionFSM>
{
    public PlayerLocomotionRecoveryState(PlayerLocomotionFSM self) : base(self) { }

    private int _recoveryToken;
    public bool heavyRecovery = false;

    public void Configure(bool heavyRecovery) => this.heavyRecovery = heavyRecovery;

    public override void Enter()
    {
        // Debug.Log("Entered Recovery");
        AnimationEvents.Instance.DisableNonEssentialActions();

        if (heavyRecovery)
        {
            // Debug.Log("Heavy recovery, playing recovery animation");
            m_self.recoveryTimer.isSetup = false;

            _recoveryToken = m_self.p_anim.TriggerTokenedAnim(
                key: "Recovery",
                triggerName: "SentFlyingRecovery", 
                () => m_self.recoveryTimer.Setup()
            );
        }
        else
        {
            m_self.recoveryTimer.Setup(); 
        }

        m_self.ResetVelocityFactors();

    }

    public override void Update()
    {
        m_self.MovePlayer();

        if (!m_self.recoveryTimer.isSetup) return;

        m_self.recoveryTimer.Tick();
        if (m_self.stickToGround) m_self.SurfaceStick();
        if (m_self.recoveryTimer.Finished()) m_self.GoToState("regular");
    }

    public override void Exit()
    {
        // Debug.Log("Exiting Recovery");
        // AnimationEvents.Instance.EnableActionMap();
        m_self.p_anim.Cancel("Recovery", _recoveryToken);
    }
}

public class PlayerLocomotionSwimState : State<PlayerLocomotionFSM>, IFixedUpdate
{

    public PlayerLocomotionSwimState(PlayerLocomotionFSM self) : base(self) { }

    private static readonly HashSet<string> Actions = new() { "Move", "Look" };
    Coroutine C_modelOffsetCoroutine;
    public float prevSpeed;
    public override void Enter()
    {
        m_self.p_anim.animator.Play("Moving"); // Interrup any animation and go to the movement blend tree immediately to prevent weird transition issues when entering water while doing other animations. We might want to change this later.
        
        prevSpeed = m_self.MaxSpeed;
        m_self.MaxSpeed = m_self.swimmingMaxSpeed;   

        AnimationEvents.Instance.DisableActionsInPlayerMapExcept(Actions);

        m_self.desiredSurfaceStick = PlayerLocomotionFSM.SurfaceToStick.Water;
        // ModelOffset coroutine interpolates the value of the Player state

        StopModelOffsetCoroutine();
        C_modelOffsetCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetModelYOffsetSmoothly(m_self.swimmingModelOffset, 0.1f, true));

    }
    public override void Update()
    {
        m_self.inWater = Physics.Raycast(m_self.waterRayCastOrigin.position, Vector3.down, out m_self.waterRayHit, m_self.waterRayDist, m_self.waterLayer, QueryTriggerInteraction.Collide);
        if (m_self.inWater)
        {
            float waterLevel = m_self.waterRayHit.point.y;
            float swimStartHeight = m_self.transform.position.y + m_self.waterSwimThreshold;

            if (waterLevel < swimStartHeight) 
                m_self.GoToState("regular");
            else
                m_self.stickToGround = true;
        }
        else m_self.GoToState("regular");
    }
    public void FixedUpdate() => m_self.MovePlayer();
    public override void Exit()
    {
        m_self.MaxSpeed = prevSpeed;

        AnimationEvents.Instance.EnableActionMap();

        Player.Instance.playerState = Player.PlayerStates.normal;
        m_self.desiredSurfaceStick = PlayerLocomotionFSM.SurfaceToStick.Ground;

        StopModelOffsetCoroutine();
        C_modelOffsetCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetModelYOffsetSmoothly(m_self.p_anim.originalOffset.y, 0.1f, false));
    }

    public void StopModelOffsetCoroutine()
    {
        if (C_modelOffsetCoroutine != null)
        {
            m_self.p_anim.StopCoroutine(C_modelOffsetCoroutine);
            C_modelOffsetCoroutine = null;
        }
    }
}

public class PlayerLocomotionAimState : State<PlayerLocomotionFSM>, IFixedUpdate
{

    public PlayerLocomotionAimState(PlayerLocomotionFSM self) : base(self) { }

    private static readonly HashSet<string> Actions = new() { "Move", "Look", "Aim" };
    Coroutine C_LayerSetCoroutine;
    Coroutine C_rigWeightCoroutine;
    Coroutine C_aimFadeCoroutine;


    public override void Enter()
    {
        AnimationEvents.Instance.DisableActionsInMapExcept(Actions);

        // If using a weapon, disable weapon object and enable bow object
        Player.Instance._playerEquipment.DeleteWeaponVisuals();
        m_self.p_anim.bowAnimator.gameObject.SetActive(true);
        Player.Instance._playerEquipment.SetPreviewBowActive(true);
        m_self.p_anim.TriggerBowCharge();

        StopCoroutines();
        C_LayerSetCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetLayerWeightSmoothly(1, 0.2f, 2));
        C_rigWeightCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetRigWeightSmoothly(0, 1f, 0.2f));
        C_aimFadeCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetAimCanvasGroupFadeSmoothly(1f, 0.2f));
    }


    public override void Update() {}
    public void FixedUpdate() => m_self.MovePlayer(); // PlayerAnimation sets the forward of our model to the aim direction


    public override void Exit()
    {
        AnimationEvents.Instance.EnableActionMap();

        StopCoroutines();
        C_LayerSetCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetLayerWeightSmoothly(0, 0.2f, 2));
        C_rigWeightCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetRigWeightSmoothly(0, 0f, 0.2f));
        C_aimFadeCoroutine = m_self.p_anim.StartCoroutine(m_self.p_anim.SetAimCanvasGroupFadeSmoothly(0f, 0.2f));
    }

    public void StopCoroutines()
    {
        if (C_LayerSetCoroutine != null)
        {
            m_self.p_anim.StopCoroutine(C_LayerSetCoroutine);
            C_LayerSetCoroutine = null;
        }
        if (C_rigWeightCoroutine != null)
        {
            m_self.p_anim.StopCoroutine(C_rigWeightCoroutine);
            C_rigWeightCoroutine = null;
        }
        if (C_aimFadeCoroutine != null)
        {
            m_self.p_anim.StopCoroutine(C_aimFadeCoroutine);
            C_aimFadeCoroutine = null;
        }
    }
}

public class PlayerLocomotionIdleState : State<PlayerLocomotionFSM>
{
    public PlayerLocomotionIdleState(PlayerLocomotionFSM self) : base(self) { }
    public override void Enter() {}
    public override void Update() {}
    public override void Exit() {}
}
