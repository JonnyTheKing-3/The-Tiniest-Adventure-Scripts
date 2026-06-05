using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimation : AnimationHelper
{
    protected Enemy enemy;

    public float modelRotationSpeed = 10f;
    public EnemyDarknessDeathEffect darknessDeathEffect;

    [HideInInspector] public Transform modelForward;


    [HideInInspector] public float DirectionMagnitude = 0f;
    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponentInParent<Enemy>();
        darknessDeathEffect = GetComponent<EnemyDarknessDeathEffect>();
    }

    void Update()
    {
        if (enemy.startedDeathRoutine) 
        {
            animator.Play("SentFlyingKnockback", 0, 1);
            return;
        }
        else if (SaveGameManager.Instance.loadRoutine != null)
        {
            animator.Play("Moving", 0, 0f);
        }
        
        float x;
        float y;
        if (enemy.enemyCombat.IsInState<EnemyMeleeStrafeState>())
        {
            // Debug.Log("Strafing");
            Vector3 localMoveDir = enemy.transform.InverseTransformDirection(enemy.enemyLoco.moveDirection.normalized);
            x = localMoveDir.x;
            y = localMoveDir.z;
        }
        else
        {
            // Make enemy moveFWD in battle when in battle. x/yDir are only used for battle
            x = 0f;
            y = enemy.enemyLoco.NewHorizontalVel.magnitude / enemy.enemyLoco.MaxSpeed;
        }

        if (!enemy.enemyCombat.IsInState<EnemyCombatBaseIdleState>() && enemy.enemyCombat is not EnemyBowArrowFSM)
        {
            animator.SetFloat("Strafing", 1f);
        }
        animator.SetFloat("EnemyState", (float)enemy.enemyState);
        animator.SetFloat("xDir", x);
        animator.SetFloat("yDir", y);
        animator.SetFloat("DirectionMagnitude", enemy.enemyLoco.DirectionMagnitude);
        animator.SetFloat("Speed", enemy.enemyLoco.NewHorizontalVel.magnitude / enemy.enemyLoco.MaxSpeed); // counteract's the downward pull
        animator.SetBool("Grounded", enemy.enemyLoco.grounded);
        animator.SetBool("InAttack", false); // TODO
        animator.SetBool("CanFollowUpAttack", false); // TODO
    }

    void LateUpdate()
    {
        if (enemy.startedDeathRoutine) return;

        Vector3 faceDir = enemy.enemyLoco.NewHorizontalVel;
        faceDir.y = 0f;

        if (!animator.GetBool("InAttack") && 
            faceDir.sqrMagnitude > 0.0001f && 
            Vector3.Angle(faceDir.normalized, transform.forward.normalized) > 1f &&
            !enemy.enemyLoco.IsInState<EnemyLocomotionKnockbackState>() && !enemy.enemyCombat.IsInState<EnemyMeleeStrafeState>())
            
            { RotateModel(); }
    }

    public void SetLockOnForward(Transform target) => modelForward = target;
    public void RotateModel()
    {
        Vector3 dir = enemy.enemyLoco.NewHorizontalVel;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, modelRotationSpeed * Time.deltaTime);
    }

    // Used for other scripts
    public void RotateModelInstant()
    {
        if (modelForward == null) return;
        Vector3 dir = modelForward.position - enemy.enemyLoco.transform.position;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    public void RotateModelTowardsDirectionInstant(Vector3 lookPos)
    {
        Vector3 dir = lookPos - enemy.enemyLoco.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(dir);
    }

    // Used for other scripts
    public Coroutine RotateModelFully(System.Action onFinished, float duration = 0.1f) {return StartCoroutine(RotateModelFullyRoutine(onFinished, duration));}
    private IEnumerator RotateModelFullyRoutine(System.Action onFinished, float duration)
    {
        float elapsedTime = 0f;
    
        Vector3 dir = modelForward.position - enemy.transform.position;
        dir.y = 0f;
    
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(dir);
    
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
    
        transform.rotation = targetRot;
        onFinished?.Invoke();
    }


    public void ResetTrigger(string triggerName) => animator.ResetTrigger(triggerName);
    public void TriggerCorrectKnockbackAnimation(HitData attack)
    {
        switch (attack.effect)
        {
            case HitData.HitEffect.Regular:
                TriggerKnockback();
                break;
            case HitData.HitEffect.SendFlying:
                animator.Play("SentFlyingKnockback", 0, 0f);
                break;
            case HitData.HitEffect.Stun:
                TriggerKnockback();
                break;
        }
    }
    public void TriggerKnockback() => animator.SetTrigger("KnockedBack"); // Used by TriggerCorrectKnockbackAnimation()
    public void TriggerSentFlying() => animator.SetTrigger("SentFlying"); // Used by TriggerCorrectKnockbackAnimation() and EnemyLocomotionKnockbackState Enter()
}
