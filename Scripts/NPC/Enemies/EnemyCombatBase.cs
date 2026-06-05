using UnityEngine;

public abstract class EnemyCombatBase : FiniteStateMachine, IHittable
{
    [HideInInspector] public Enemy enemy;
    [HideInInspector] public Transform Target;
    public string CurrentStateName;
    public float returnToBaseDistanceThreshold = 15f;

    [Tooltip("Whenever a hit is the last hit, we use this attack's data")]
    public AttackData deathAttackData;

    protected virtual void Awake() 
    {
        // Debug.Log("EnemyCombatBase Awake");
        enemy = GetComponent<Enemy>();
        AddState("idle", new EnemyCombatBaseIdleState(this));   
        AddState("alerted", new EnemyAlertedState(this));
    }
    protected virtual void Start()
    {
        GoToState("idle");
    }
    protected override void Update()
    {
        base.Update();
        string stateName = CurrentState != null ? CurrentState.GetType().Name : "null";
        CurrentStateName = stateName.Replace("EnemyMelee", "").Replace("EnemyCombat", "").Replace("State", "");
    }

    public void SetTarget(Transform targetTransform) => Target = targetTransform;
    public float distanceToTarget() => Vector3.Distance(transform.position, Target.position);

    public virtual void StartCombatCycle() => GoToState("alerted");
    public abstract void AfterAlertedIntention(); // For subclasses to implement


    public void GotHit(GameObject attacker, HitData attackData, float AttackerAttackStat, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (SaveGameManager.Instance.loadRoutine != null) return; // If we are loading, don't interact
        if (attacker.TryGetComponent<Enemy>(out Enemy e)) return; // Don't get hit by other enemies
        if (enemy.healthDepleted) return;                         // Don't trigger any attacks after death if body is still in field

        enemy.health.TakeDamage(attackData.damage + AttackerAttackStat - enemy.combatStats.Defense);

        GameObject hitVFX = GameManager.Instance.GetHitImpactVFX();
        hitVFX.transform.position = hitPoint;
        hitVFX.transform.rotation = Quaternion.LookRotation(hitNormal);
        hitVFX.SetActive(true);

        if (transform.TryGetComponent<IKnockbackable>(out IKnockbackable knockbackable)) 
        {
            // Debug.Log("enemy got hit");
            AudioManager.Instance.PlayHitImpact(transform);
            HitData passData = enemy.healthDepleted ? deathAttackData.hit : attackData;

            enemy.enemyAnim.TriggerCorrectKnockbackAnimation(passData);
            
            enemy.enemyLoco.ApplyHitstop( passData,  
                onFinished:() => KnockbackTriggered(attacker, passData, knockbackable));
        }
    }
    public void KnockbackTriggered(GameObject attacker, HitData attackData, IKnockbackable knockbackable) // Used in GotHit after hitstop ends
    {
        // Debug.Log("Enemy hitstop ended. Calculating knockback.");
        float dist = attackData.distance;
        float dur = attackData.duration;
        Vector3 targetPosition = attacker.transform.position;
        targetPosition += attacker.transform.GetChild(0).TryGetComponent(out Animator animator)? animator.transform.forward * dist : attacker.transform.forward * dist; // Use the forward of the attacker's model if it has one
        targetPosition.y = transform.position.y; // Keep the enemy's y position
        
        knockbackable.StartKnockback(targetPosition, dur, attackData, attacker.transform);
    }

}

public class EnemyCombatBaseIdleState : State<EnemyCombatBase>
{
    public EnemyCombatBaseIdleState(EnemyCombatBase self) : base(self) { }
    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}

public class EnemyAlertedState : State<EnemyCombatBase>
{
    public EnemyAlertedState(EnemyCombatBase self) : base(self) { }

    private Coroutine rotateC;
    private bool exited;

    public override void Enter()
    {
        // Debug.Log("COMBAT: Entered Alerted");
        m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
        m_self.enemy.enemyAnim.SetLockOnForward(m_self.Target);
        exited = false;

        rotateC = m_self.enemy.enemyAnim.RotateModelFully(() => 
        {
            // Debug.Log("Rotation finished in Alerted");
            if (exited) return;
            // Debug.Log("Going to Chase from Alerted");
            rotateC = null;
            m_self.AfterAlertedIntention();
        }, 
        duration: .17f);
    }

    public override void Update() { }

    public override void Exit()
    {
        // Debug.Log("Exiting Alerted");
        exited = true;

        if (rotateC != null)
        {
            // Debug.Log("Stopping rotation coroutine in Alerted Exit");
            m_self.enemy.enemyAnim.StopCoroutine(rotateC);
            rotateC = null;
        }

        // Debug.Log("Exited Alerted");
    }
}