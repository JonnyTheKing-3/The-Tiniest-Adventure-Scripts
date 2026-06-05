using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimation : AnimationHelper
{
    PlayerLocomotionFSM p_movement => Player.Instance._playerLocomotion;
    
    public float modelRotationSpeed = 10f;
    public Animator bowAnimator; // Used for bow aiming animations
    public RigBuilder rigBuilder; // Used for enabling/disabling the rig when entering/exiting bow aiming
    public CanvasGroup aimIconCanvasGroup;
    



    [HideInInspector] public RectTransform aimIconRectTransform;
    [HideInInspector] public Vector3 originalOffset;
    [HideInInspector] public AnimationEvents anim_events;
    [HideInInspector] public Transform modelForward;
    [HideInInspector] public float MaxSpeed;
    private Vector3 BowIdleRotation = new Vector3(0f, -55f, 0f);
    private Vector3 BowAimRotation = new Vector3(0f, -90f, 0f);
    private float playerState;
    private bool blendingPlayerState;

    protected override void Awake()
    {
        base.Awake();
        anim_events = GetComponent<AnimationEvents>();

        originalOffset = transform.localPosition;
        rigBuilder = GetComponent<RigBuilder>();
        aimIconRectTransform = aimIconCanvasGroup.transform.GetChild(0).GetComponent<RectTransform>();
    }

    void Start()
    {
        MaxSpeed = p_movement.RunningMaxSpeed;
    }

    float prevXLocalMove, prevZLocalMove; // Used for debugging
    void Update()
    {
        if (Player.Instance.startedDeathRoutine)
        {
            animator.Play("SentFlyingKnockback", 0, 1);
            return;
        }
        
        // Preparing variables for animator
        playerState = blendingPlayerState ? playerState : (float)Player.Instance.playerState;
        float xLocalMove = transform.InverseTransformDirection(p_movement.SurfaceAppliedDir).x;
        float zLocalMove = transform.InverseTransformDirection(p_movement.SurfaceAppliedDir).z;
        if (CamerasManager.Instance.CameraState == CamerasManager.CameraStates.LockOn)
        {
            if (!Player.Instance._playerLocomotion.IsInState<PlayerLocomotionDodgeState>() && !Player.Instance._playerLocomotion.IsInState<PlayerLocomotionCounterConfirmState>())
            {
                // Determine dominant direction. These Dodge animations don't look good when blended
                prevXLocalMove = Mathf.Abs(zLocalMove) > Mathf.Abs(xLocalMove) ? 0 : Mathf.Sign(xLocalMove);
                prevZLocalMove = Mathf.Abs(zLocalMove) > Mathf.Abs(xLocalMove) ? Mathf.Sign(zLocalMove) : 0;
            }
            else
            {
                xLocalMove = prevXLocalMove;
                zLocalMove = prevZLocalMove;
            }
        }


        // Applying variables to animator
        animator.SetFloat("PlayerState", playerState);
        animator.SetFloat("CameraState", (float)CamerasManager.Instance.CameraState);
        animator.SetFloat("Speed", p_movement.rb.linearVelocity.magnitude / MaxSpeed);
        animator.SetFloat("InputMagnitude", p_movement.inputDir.magnitude);
        animator.SetFloat("xInput", p_movement.inputDir.x);
        animator.SetFloat("yInput", p_movement.inputDir.y);
        animator.SetFloat("xLocalMovement", xLocalMove);
        animator.SetFloat("zLocalMovement", zLocalMove);
        animator.SetBool("Grounded", p_movement.stickToGround);
        animator.SetBool("InAttack", Player.Instance.InAttack);
        animator.SetBool("CanFollowUpAttack", Player.Instance.CanFollowUpAttack);
        animator.SetFloat("AroundEnemies", GameManager.Instance.enemiesInCombat.Count > 0 ? 1f : 0f);
    }

    void LateUpdate()
    {
        if (Player.Instance.startedDeathRoutine) return;

        if (CamerasManager.Instance.CameraState == CamerasManager.CameraStates.Aim)
        {
            transform.forward = p_movement.orientation.forward; // CameraOrientation sets orientation towards what we are aiming at
            return;
        }

        float angle = Vector3.Angle(p_movement.moveDirection.normalized, transform.forward.normalized);
        if (!Player.Instance.InAttack && ((p_movement.inputDir.sqrMagnitude > .01 && angle > 1f) || CamerasManager.Instance.CameraState == CamerasManager.CameraStates.LockOn)) 
            RotateModel();
    }


    public void RotateModel()
    {
        Vector3 dir = Vector3.zero;
        float rot = modelRotationSpeed;

        switch (CamerasManager.Instance.CameraState)
        {
            case CamerasManager.CameraStates.ThirdPerson:
                dir = p_movement.moveDirection.normalized;

                if (p_movement.IsInState<PlayerLocomotionBlockState>()) 
                {
                    dir = p_movement.NewHorizontalVel.normalized;
                    rot /= 2f;
                }
                break;

            case CamerasManager.CameraStates.LockOn:
                dir = modelForward.position - p_movement.transform.position;
                break;

            default:
                dir = p_movement.moveDirection.normalized;
                break;
        }
        
        dir.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rot * Time.deltaTime);
    }

    // Used for other scripts
    public void RotateModelInstant()
    {
        if (modelForward == null) return;
        Vector3 dir = modelForward.position - p_movement.transform.position;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    // Used for other scripts
    public IEnumerator RotateModelFully(float duration = 0.1f)
    {
        float elapsedTime = 0f;

        Vector3 dir = modelForward.position - p_movement.transform.position;
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
    }

    public void RotateModelTowardsDirectionInstant(Vector3 lookPos)
    {
        Vector3 dir = lookPos - p_movement.transform.position;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    public IEnumerator RotateModelUpright(float duration = 0.15f)
    {
        float elapsedTime = 0f;
    
        Quaternion startRot = transform.rotation;
    
        Vector3 euler = transform.rotation.eulerAngles;
        Quaternion targetRot = Quaternion.Euler(0f, euler.y, 0f);
    
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
    
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
    
            yield return null;
        }
    
        transform.rotation = targetRot;
    }

    public void TriggerCorrectKnockbackAnimation(HitData attack, bool blocked = false)
    {
        // blocked knockback
        if (blocked)
        {   
            animator.SetLayerWeight(1, 0f); // Reset upper body layer weight so when blocked knockback plays it's not affected by upper body layer
            TriggerBlockedKnockback();
            return;
        }

        // Regular knockback
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

    public void SetLockOnForward(Transform target) => modelForward = target; // Used by playerinputscript
    public void TriggerAttack() { if (p_movement.stickToGround) animator.SetTrigger("Attack"); } // Used by playerinputscript
    public void TriggerDash() { if (p_movement.stickToGround) animator.SetTrigger("Dash"); } // Used by playerinputscript
    public void TriggerDashExit() => animator.SetTrigger("DashExit"); // Used by PlayerLocomotionFSM Dash State
    public void TriggerKnockback() => animator.SetTrigger("KnockedBack"); // Used by PlayerLocomotion FSM Knockback State
    public void TriggerBlockedKnockback() => animator.SetTrigger("BlockedKnockback"); // Used by PlayerLocomotion FSM Knockback State
    public void TriggerDodge() => animator.SetTrigger("Dodge"); // Used by PlayerLocomotion FSM Dodge State
    public void TriggerCounterAttack() => animator.SetTrigger("CounterAttack"); // Used by PlayerLocomotion FSM CounterAttack State
    public void TriggerBowCharge() { animator.SetTrigger("BowCharge"); bowAnimator.SetTrigger("BowCharge"); bowAnimator.transform.localRotation = Quaternion.Euler(BowAimRotation);} // Used by PlayerLocomotion FSM Bow Charge State
    public void TriggerBowShot() {animator.SetTrigger("BowShot"); bowAnimator.SetTrigger("BowShot"); bowAnimator.transform.localRotation = Quaternion.Euler(BowIdleRotation);} // Used by PlayerInputScript

    public void SetAnimatorSpeed(float speed)
    {
        animator.speed = speed;
        bowAnimator.speed = speed;
    }
    
    public IEnumerator SetLayerWeightSmoothly(float targetWeight, float duration, int layerIndex) // Used by PlayerLocomotionBlockState
    {
        float elapsedTime = 0f;
        float startWeight = animator.GetLayerWeight(layerIndex); // Upper Body Layer is layer 1

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float newWeight = Mathf.Lerp(startWeight, targetWeight, t);
            animator.SetLayerWeight(layerIndex, newWeight);
            
            if (animator.GetLayerWeight(layerIndex) <= 0.01f) break; // Used so when triggering blockedknockback animation, the coroutine stops and doesn't interfere with the animation 

            yield return null;
        }
        
        animator.SetLayerWeight(layerIndex, targetWeight);
    }

    public IEnumerator SetModelYOffsetSmoothly(float targetOffsetY, float duration, bool enteringSwimming)
    {
        float elapsedTime = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = new Vector3(startPos.x, targetOffsetY, startPos.z);
        blendingPlayerState = true;

        float startPlayerState = playerState;
        float targetPlayerState = enteringSwimming ? (float)Player.PlayerStates.swimming : (float)Player.PlayerStates.normal;


        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            playerState = Mathf.Lerp(startPlayerState, targetPlayerState, t);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.localPosition = targetPos;

        if (enteringSwimming) 
            playerState = (float)Player.PlayerStates.swimming;
        else 
            playerState = (float)Player.PlayerStates.normal;
    }

    public IEnumerator SetRigWeightSmoothly(int rigLayerIndex,float targetWeight, float duration)
    {
        Rig rig = rigBuilder.layers[rigLayerIndex].rig;
        float initialWeight = rig.weight;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newWeight = Mathf.Lerp(initialWeight, targetWeight, elapsedTime / duration);
            rig.weight = newWeight;
            yield return null;
        }

        rig.weight = targetWeight; // Ensure it ends at the exact target weight
    }

    public IEnumerator SetAimCanvasGroupFadeSmoothly(float targetFade, float duration)
    {
        float initialFade = aimIconCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newFade = Mathf.Lerp(initialFade, targetFade, elapsedTime / duration);
            aimIconCanvasGroup.alpha = newFade;
            yield return null;
        }

        aimIconCanvasGroup.alpha = targetFade; // Ensure it ends at the exact target fade
    }
}
