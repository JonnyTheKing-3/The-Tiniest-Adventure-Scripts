using System;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyLocomotionFSM : NPCLocomotion, IKnockbackable
{

    [Header("ENEMY VARIABLES")]
    [HideInInspector] public Enemy enemy;
    public Timer Cooldown;
    public bool hitstopActive;
    public string CurrentStateName = "";

    [Space]
    public LayerMask AirKnockbackCollisionLayers;
    // Smooth toward that offset instead of snapping instantly
    public float smoothTime = 0.08f;   // bigger = softer/slower
    public float AirKnockbackCollisionCorrectionMaxSpeed = 20f;       // bigger = stronger/faster push

    protected override void Awake()
    {
        base.Awake();

        enemy = GetComponent<Enemy>();

        AddState("regular",  new EnemyLocomotionRegularState(this));
        AddState("knockback", new EnemyLocomotionKnockbackState(this));
        AddState("recovery", new EnemyLocomotionRecoveryState(this));
    }
    void Start()
    {
        GoToState("regular");
    }
    protected override void Update()
    {
        CurrentStateName = CurrentState?.GetType().Name.Replace("EnemyLocomotion", "").Replace("State", "") ?? "null";
        UpdateGroundingStatus();

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


    // HELPERS ----------------------------------------------------

    // Hitstop
    float hitstopTimer;
    Vector3 hitstopStoredCurrentVel, hitstopStoredMoveDir, hitstopStoredNewHorizontalVel, hitstopStoredTargetVel; float hitstopStoredVerticalVel;
    Action onHitstopEnd;
    public void ApplyHitstop(HitData attack, Action onFinished = null)
    {
        // Debug.Log("Enemy applying Player Hitstop");

        if (attack.hitstop <= 0f)
        {
            onFinished?.Invoke();
            return;
        }

        // Extend hitstop if already active
        if (hitstopActive)
        {
            if (attack.effect == HitData.HitEffect.SendFlying) onHitstopEnd = onFinished; // overwrite callback if knockback will sendFlying
            else if (onHitstopEnd == null) onHitstopEnd = onFinished ?? onHitstopEnd;
            
            hitstopTimer = Mathf.Max(hitstopTimer, attack.hitstop);
            return;
        }

        onHitstopEnd = onFinished ?? onHitstopEnd; // We don't want to set it to null if onFinished is null, because we might have a previous callback stored
        hitstopActive = true;
        hitstopTimer = attack.hitstop;

        // cache motion state
        hitstopStoredCurrentVel = currentVelocity;
        hitstopStoredVerticalVel = verticalVelocity;
        hitstopStoredMoveDir = moveDirection;
        hitstopStoredNewHorizontalVel = NewHorizontalVel;
        hitstopStoredTargetVel = targetVel;
        
        // stop animation
        enemy.enemyAnim.animator.speed = 0f;

        ResetVelocityFactors();
    }
    public bool HitstopEnded()
    {
        // Debug.Log("Enemy hitstop ticking.");
        hitstopTimer -= Time.deltaTime;
        if (hitstopTimer > 0f) return false;


        hitstopActive = false;
        hitstopTimer = 0f;

        currentVelocity = hitstopStoredCurrentVel;
        verticalVelocity = hitstopStoredVerticalVel;
        moveDirection = hitstopStoredMoveDir;
        NewHorizontalVel = hitstopStoredNewHorizontalVel;
        targetVel = hitstopStoredTargetVel;

        enemy.enemyAnim.animator.speed = 1f;

        onHitstopEnd?.Invoke();
        onHitstopEnd = null; // Clear callback after invoking to avoid repeated calls
        return true;
    }


    // Knockback
    public void StartKnockback(Vector3 targetPos, float duration, HitData attackData, Transform attacker)
    {
        // Debug.Log("Enemy starting knockback");
        GetState<EnemyLocomotionKnockbackState>("knockback").Configure(transform.position, targetPos, duration, attackData, attacker);
        GoToState("knockback");
    }


    // Recovery
    public void StartRecovery(float duration)
    {
        GetState<EnemyLocomotionRecoveryState>("recovery").Configure(duration);
        GoToState("recovery");
    }

}

public class EnemyLocomotionRegularState : State<EnemyLocomotionFSM>
{
    public EnemyLocomotionRegularState(EnemyLocomotionFSM self) : base(self) { }

    public override void Enter() { }

    public override void Update() => m_self.Move();

    public override void Exit() { }
}

public class EnemyLocomotionKnockbackState : State<EnemyLocomotionFSM>
{
    public HitData attack;
    private Vector3 start;
    private Vector3 target;
    private float timer;
    private float duration;
    private Vector3 postArcVelocity;
    private Transform attacker;
    private Vector3 modelFaceDirection;

    // -------- separation tuning --------
    private const float regularSpacing = 1.75f;
    private const float sendFlyingSpacing = 1.25f;
    private const float groupingRadius = 2.5f;
    private const float minForwardDotToGroup = 0.8f;

    private static readonly List<EnemyLocomotionKnockbackState> activeKnockbacks = new();

    // Cached for this whole knockback so the path stays stable
    private float cachedLateralOffset = 0f;
    private Vector3 cachedLateralAxis = Vector3.zero;
    private bool cachedSeparationAssigned = false;

    public EnemyLocomotionKnockbackState(EnemyLocomotionFSM self) : base(self) { }

    public void Configure(Vector3 startPos, Vector3 endPos, float duration, HitData attackData, Transform attacker)
    {
        this.start = startPos;
        this.target = endPos;
        this.duration = Mathf.Max(duration, 0.001f);
        this.timer = 0f;
        this.attack = attackData;
        this.attacker = attacker;
        this.modelFaceDirection = GetModelFaceDirection(startPos, endPos);

        cachedLateralOffset = 0f;
        cachedLateralAxis = Vector3.zero;
        cachedSeparationAssigned = false;

        m_self.enemy.enemyBrain.GoToState("idle");
        m_self.enemy.enemyCombat.GoToState("idle");
    }

    public override void Enter()
    {
        RotateModelForKnockback();
        m_self.ResetVelocityFactors();
        postArcVelocity = Vector3.zero;
        // m_self.controller.excludeLayers = m_self.AirKnockbackCollisionLayers;

        if (!activeKnockbacks.Contains(this))
            activeKnockbacks.Add(this);

        AssignSeparationSlotInstant();
    }

    public override void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        Vector3 desiredPos = m_self.transform.position;

        switch (attack.effect)
        {
            case HitData.HitEffect.Regular:
            {
                desiredPos = Vector3.Lerp(start, target, Mathf.Clamp01(t));
                break;
            }

            case HitData.HitEffect.SendFlying:
            {
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

                float animT = Mathf.Clamp(t, 0f, 0.8f);
                m_self.enemy.enemyAnim.animator.Play("SentFlyingKnockback", 0, animT);
                break;
            }

            case HitData.HitEffect.Stun:
            {
                break;
            }
        }

        // Keep the same lateral lane the entire time
        if (cachedSeparationAssigned)
            desiredPos += cachedLateralAxis * cachedLateralOffset;

        Vector3 baseKnockbackDelta = desiredPos - m_self.transform.position;
        m_self.MoveByDelta(baseKnockbackDelta, attack.effect == HitData.HitEffect.SendFlying);

        if (KnockbackFinished(t))
        {
            if (m_self.enemy.healthDepleted) 
            {
                m_self.enemy.DeathRoutine();
                GameManager.Instance.PlayHitGroundKnockbackVFX(m_self.enemy.deathVFXposition.position, m_self.transform);
            }

            else                                
            {
                m_self.GoToState("recovery"); 
                
                if (attack.effect == HitData.HitEffect.SendFlying)
                    GameManager.Instance.PlayHitGroundKnockbackVFX(m_self.enemy.deathVFXposition.position, m_self.transform);
            }
        }
    }

    private void AssignSeparationSlotInstant()
    {
        if (attacker == null)
            return;

        Vector3 forward = GetKnockbackForwardXZ();
        if (forward.sqrMagnitude < 0.0001f)
            return;

        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        cachedLateralAxis = lateral;

        List<EnemyLocomotionKnockbackState> group = GetRelevantGroup(forward);
        if (group.Count <= 1)
        {
            cachedSeparationAssigned = true;
            cachedLateralOffset = 0f;
            return;
        }

        group.Sort((a, b) =>
        {
            float aProj = Vector3.Dot(Flatten(a.start), lateral);
            float bProj = Vector3.Dot(Flatten(b.start), lateral);

            int cmp = aProj.CompareTo(bProj);
            if (cmp != 0) return cmp;

            return a.m_self.GetInstanceID().CompareTo(b.m_self.GetInstanceID());
        });

        int myIndex = group.IndexOf(this);
        if (myIndex < 0)
        {
            cachedSeparationAssigned = true;
            cachedLateralOffset = 0f;
            return;
        }

        float spacing;
        switch (attack.effect)
        {
            case HitData.HitEffect.Regular:
                spacing = regularSpacing;
                break;

            case HitData.HitEffect.SendFlying:
                spacing = sendFlyingSpacing;
                break;

            default:
                spacing = 0f;
                break;
        }

        float centerIndex = (group.Count - 1) * 0.5f;
        cachedLateralOffset = (myIndex - centerIndex) * spacing;
        cachedSeparationAssigned = true;
    }

    private List<EnemyLocomotionKnockbackState> GetRelevantGroup(Vector3 myForward)
    {
        List<EnemyLocomotionKnockbackState> result = new();
        Vector3 myPosXZ = Flatten(m_self.transform.position);

        for (int i = 0; i < activeKnockbacks.Count; i++)
        {
            EnemyLocomotionKnockbackState other = activeKnockbacks[i];
            if (other == null || other.m_self == null)
                continue;

            if (other == this)
            {
                result.Add(other);
                continue;
            }

            if (other.attacker != attacker)
                continue;

            Vector3 otherForward = other.GetKnockbackForwardXZ();
            if (otherForward.sqrMagnitude < 0.0001f)
                continue;

            if (Vector3.Dot(myForward, otherForward) < minForwardDotToGroup)
                continue;

            Vector3 otherPosXZ = Flatten(other.m_self.transform.position);
            float sqrDist = (myPosXZ - otherPosXZ).sqrMagnitude;
            if (sqrDist > groupingRadius * groupingRadius)
                continue;

            result.Add(other);
        }

        return result;
    }

    private Vector3 GetKnockbackForwardXZ()
    {
        Vector3 forward = Flatten(target - start);

        if (forward.sqrMagnitude < 0.0001f && attacker != null)
            forward = Flatten(m_self.transform.position - attacker.position);

        return forward.normalized;
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
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

        m_self.enemy.enemyAnim.RotateModelTowardsDirectionInstant(m_self.transform.position + modelFaceDirection);
    }

    public bool KnockbackFinished(float t)
    {
        switch (attack.effect)
        {
            case HitData.HitEffect.Regular:
                if (t >= 1f) return true;
                break;

            case HitData.HitEffect.SendFlying:
                if ((t > .2f && IsActuallyGrounded()) || (t > 1f && m_self.controller.velocity.magnitude < .01f))
                {
                    m_self.enemy.enemyAnim.animator.Play("SentFlyingKnockback", 0, .8f);
                    return true;
                }
                break;

            case HitData.HitEffect.Stun:
                break;
        }

        return false;
    }

    public bool IsActuallyGrounded()
    {
        RaycastHit hit;
        Vector3 origin = m_self.transform.position + Vector3.up;
        bool casthit = Physics.SphereCast(origin, m_self.sphereRadius, Vector3.down, out hit, m_self.groundRayDist, m_self.groundLayer, QueryTriggerInteraction.Ignore);

        hit = casthit ? hit : default;
        bool hitGround = casthit && (m_self.transform.position.y + .1f - hit.point.y) <= m_self.MaxGroundStickDistance;

        return hitGround;
    }

    public override void Exit()
    {
        activeKnockbacks.Remove(this);
        // m_self.controller.excludeLayers = 0;
    }
}

public class EnemyLocomotionRecoveryState : State<EnemyLocomotionFSM>
{
    public EnemyLocomotionRecoveryState(EnemyLocomotionFSM self) : base(self) { }

    public void Configure(float cooldownDuration) => m_self.Cooldown.duration = cooldownDuration;

    public override void Enter()
    {
        // Debug.Log("Enemy Entered recovery. Brain off");
        m_self.SetMoveDirection(Vector3.zero);
        m_self.enemy.enemyBrain.GoToState("idle"); // Set brain off while enemy gets hit. Might want to move this to configure later
        m_self.Cooldown.Setup();
    }

    public override void Update()
    {
        m_self.Cooldown.Tick();
        if (m_self.grounded) {m_self.controller.Move(Vector3.down * (m_self.transform.position.y +.1f - m_self.Surface.point.y) * Time.deltaTime);}
        // add else clause that applies gravity if not grounded

        if (m_self.Cooldown.Finished()) 
            m_self.GoToState("regular");
    }

    public override void Exit()
    {
        m_self.enemy.enemyBrain.GoToState("decide"); // Resume AI behavior. Combatcycle can start at this point
        // Debug.Log("LOCOMOTION: Exited recovery. Brain to decide");
    }
}
