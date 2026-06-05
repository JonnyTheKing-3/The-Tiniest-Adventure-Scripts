using System.Collections;
using UnityEngine;

public class VillagerAnimation : MonoBehaviour
{
    [HideInInspector] public Villager villager;
    [HideInInspector] public Animator animator;
    public float modelRotationSpeed = 10f;
    [HideInInspector] public Transform modelForward;


    [HideInInspector] public float DirectionMagnitude = 0f;
    void Awake()
    {
        villager = GetComponentInParent<Villager>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        animator.SetFloat("VillagerState", (float)villager.villagerState);
        animator.SetFloat("xDir", villager.villagerLoco.inputDir.x);
        animator.SetFloat("yDir", villager.villagerLoco.inputDir.y);
        animator.SetFloat("DirectionMagnitude", villager.villagerLoco.DirectionMagnitude);
        animator.SetFloat("Speed", villager.villagerLoco.NewHorizontalVel.magnitude / villager.villagerLoco.MaxSpeed); // counteract's the downward pull
        animator.SetBool("Grounded", villager.villagerLoco.grounded);
        animator.SetBool("InAttack", false);
        animator.SetBool("CanFollowUpAttack", false);
    }

    void LateUpdate()
    {
        if (!animator.GetBool("InAttack") && 
            villager.villagerLoco.DirectionMagnitude > .01 && 
            Vector3.Angle(villager.villagerLoco.moveDirection.normalized, transform.forward.normalized) > 1f) 
            
            { RotateModel(); }
    }

    public void RotateModel()
    {
        Vector3 dir = villager.villagerLoco.moveDirection.normalized;
        dir.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, modelRotationSpeed * Time.deltaTime);
    }

    // // Used for other scripts
    public void RotateModelInstant()
    {
        if (modelForward == null) return;
        Vector3 dir = modelForward.position - villager.villagerLoco.transform.position;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    // // Used for other scripts
    public Coroutine RotateModelFully(System.Action onFinished, float duration = 0.1f) {return StartCoroutine(RotateModelFullyRoutine(onFinished, duration));}
    private IEnumerator RotateModelFullyRoutine(System.Action onFinished, float duration)
    {
        float elapsedTime = 0f;

        Vector3 dir = modelForward.position - villager.villagerLoco.transform.position;
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



    public void SetLockOnForward(Transform target) => modelForward = target;
    // public void TriggerKnockback() { if (enemy.enemyLoco.grounded) animator.SetTrigger("KnockedBack"); } // Used by EnemyLocomotionKnockbackState Enter()
    // public void TriggerSentFlying() => animator.SetTrigger("SentFlying"); // Used by EnemyLocomotionKnockbackState Enter()
    // public void TriggerAttack(System.Action onFinished) {animator.SetTrigger("Attack"); onFinished?.Invoke();} // Used by EnemyCombat type scripts
}
