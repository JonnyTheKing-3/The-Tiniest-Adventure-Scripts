using System.Collections.Generic;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;

public class EnemyCombatMeleeStraferFSM : EnemyCombatMeleeFSM
{
    public Timer strafeTimer;
    public float strafeRange = 2.5f;
    [Tooltip("Scalar for the speed of the strafe movement. Directly multiplied with max speed.")] public float strafeSpeedScalar = 1f;

    [HideInInspector] public float closeCombatStopRange; // For remembering the stopRange of EnemyCloseCombatBase on strafe Enter, and to set it back on strafe -> chase, so that chase gets close enough for a attack 
    
    
    protected override void Awake()
    {
        base.Awake();

        AddState("strafe", new EnemyMeleeStrafeState(this));

        closeCombatStopRange = stopRange;
        stopRange = strafeRange;
    }



    public override void AfterChaseIntention()
    {
        // If we chased with strafe distance in mind, then 
        if (stopRange == strafeRange) // stopRange changes depending on strafe or chase. So whatever state it's working for right now, is the state it was just in.
        {
            GoToState("strafe");
            stopRange = closeCombatStopRange; // set it back for the next chase, so that the enemy gets close enough to attack
        }
        else 
        {
            stopRange = strafeRange; 
            GoToState("attack");
        }
    } 

    public virtual void AfterStrafeIntention()
    {
        // Chose between strafe, or chase. For now just chase
        GoToState("chase");
    }
}


public class EnemyMeleeStrafeState : State<EnemyCombatMeleeStraferFSM>
{
    public EnemyMeleeStrafeState(EnemyCombatMeleeStraferFSM self) : base(self) { }

    private float strafeDirectionScaler = 1f; // 1 = right, -1 = left
    private float originalSpeed;

    // Smoothing
    private float smoothedRadialWeight = 0f;
    private float radialWeightVelocity = 0f;
    private Vector3 smoothedMoveDir = Vector3.zero;

    // Tuning
    private const float deadZone = 0.15f;
    private const float correctionScale = 1.5f;
    private const float maxRadialInfluence = 3f;
    private const float radialSmoothTime = .01f;
    private const float moveDirSlerpSpeed = 10f;
    private float faceTargetRotateSpeed = 720f;

    public override void Enter()
    {
        m_self.strafeTimer.Setup();

        strafeDirectionScaler = Random.value > 0.5f ? 1f : -1f;

        originalSpeed = m_self.enemy.enemyLoco.MaxSpeed;
        m_self.enemy.enemyLoco.SetSpeed(originalSpeed * m_self.strafeSpeedScalar);

        smoothedRadialWeight = 0f;
        radialWeightVelocity = 0f;
        smoothedMoveDir = Vector3.zero;
        faceTargetRotateSpeed = m_self.enemy.enemyAnim.modelRotationSpeed;
    }

    public override void Update()
    {
        m_self.strafeTimer.Tick();

        if (m_self.strafeTimer.Finished() || m_self.distanceToTarget() < m_self.closeCombatStopRange || m_self.distanceToTarget() > m_self.strafeRange * 1.5f)
        {
            m_self.AfterStrafeIntention();
            return;
        }

        RotateTowardTargetSmooth();

        Vector3 toTarget = m_self.Target.position - m_self.enemy.transform.position;
        toTarget.Normalize();
        Vector3 strafeDirection = Vector3.Cross(toTarget, Vector3.up) * strafeDirectionScaler; // Tangent around the target is more or less the strafe direction

        float currentDistance = m_self.distanceToTarget();
        float desiredDistance = m_self.strafeRange;
        float distanceError = currentDistance - desiredDistance;

        if (Mathf.Abs(distanceError) < deadZone) distanceError = 0f; // Ignore small distance errors to prevent jitter

        float targetRadialWeight = Mathf.Clamp( distanceError * correctionScale, -maxRadialInfluence, maxRadialInfluence);
        smoothedRadialWeight = Mathf.SmoothDamp(smoothedRadialWeight, targetRadialWeight, ref radialWeightVelocity, radialSmoothTime);

        Vector3 radialCorrection = toTarget * smoothedRadialWeight;
        Vector3 desiredMoveDirection = (strafeDirection + radialCorrection).normalized;

        if (smoothedMoveDir == Vector3.zero)
            smoothedMoveDir = desiredMoveDirection;
        else
            smoothedMoveDir = Vector3.Slerp(smoothedMoveDir,desiredMoveDirection,moveDirSlerpSpeed * Time.deltaTime).normalized;

        CharacterController cc = m_self.enemy.enemyLoco.controller;
        Vector3 castOrigin = m_self.enemy.transform.position + cc.center;
        Vector3 moveToCheck = smoothedMoveDir;
    
        float checkDistance = Mathf.Max(cc.radius + 0.2f, m_self.enemy.enemyLoco.MaxSpeed * 0.2f);
    
        if (Physics.SphereCast(castOrigin, cc.radius, moveToCheck, out RaycastHit hit, checkDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // Debug.Log("Strafe hit");
            Vector3 slideDir = Vector3.ProjectOnPlane(moveToCheck, hit.normal);
            // Debug.DrawRay(castOrigin,slideDir * 2, Color.red);
            slideDir.y = 0f;
    
            if (slideDir.sqrMagnitude > 0.001f)
                smoothedMoveDir = slideDir.normalized;
        }

        m_self.enemy.enemyLoco.SetMoveDirection(smoothedMoveDir);
    }

    public override void Exit()
    {
        m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
        m_self.enemy.enemyLoco.SetSpeed(originalSpeed);
    }

    // Handling rotation here is better because it's continous and I don't think I should use a coroutine for that. A bit hardcoded, but it works for now
    private void RotateTowardTargetSmooth()
    {
        Vector3 dir = m_self.Target.position - m_self.enemy.transform.position;
        dir.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        m_self.enemy.enemyAnim.transform.rotation = Quaternion.RotateTowards(
            m_self.enemy.enemyAnim.transform.rotation,
            targetRot,
            faceTargetRotateSpeed * Time.deltaTime
        );
    }
}