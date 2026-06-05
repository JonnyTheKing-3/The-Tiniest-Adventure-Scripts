using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyCloseCombatBase : EnemyCombatBase, IWeaponHolder
{
    [Space]
    public Weapon weapon;
    public float stopRange = 1.6f;
    public float chaseDirectionSlerpSpeed = 8f;

    protected override void Awake()
    {
        base.Awake();

        if (weapon == null) weapon = new Weapon();

        AddState("chase", new EnemyChaseState(this));
    }
    protected override void Start()
    {
        base.Start();

        weapon.RegisterWeaponColliders(enemy.R_hand, enemy.L_hand);
        enemy.combatStats += weapon.weaponTemplate.combatStats;
    }

    public override void AfterAlertedIntention() => GoToState("chase");
    public abstract void AfterChaseIntention(); // For subclasses to implement

    // Hit functions
    public Weapon GetWeapon() => weapon;
    private HashSet<IHittable> objectsHit = new HashSet<IHittable>();
    void OnTriggerEnter(Collider other) // Weapons have the colliders
    {
        if (other.gameObject == gameObject) return;

        if (other.TryGetComponent<IHittable>(out IHittable hittable))
        {
            if (objectsHit.Add(hittable))
            {
                if (Player.Instance.PerfectDodgeActive) return;
                // Debug.Log("Hit player. Player's state: " + Player.Instance._playerLocomotion.CurrentStateName);
                
                enemy.enemyLoco.ApplyHitstop(weapon.CurrentHitData()); // Apply hitstop on self

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                hittable.GotHit(gameObject, weapon.CurrentHitData(), enemy.combatStats.Attack, hitPoint, (transform.position - hitPoint).normalized);  // Hit the other. Other has hitstop if applicable
            }
        }
    }
    public void ResetHitList() => objectsHit.Clear();
}


public class EnemyChaseState : State<EnemyCloseCombatBase>
{
    private Vector3 smoothedMoveDir;

    public EnemyChaseState(EnemyCloseCombatBase self) : base(self) { }
    public override void Enter()
    {
        // Debug.Log("COMBAT: Entered Chase");
        smoothedMoveDir = GetInitialMoveDirection();
        m_self.enemy.enemyBrain.agent.SetDestination(m_self.Target.position);
    }

    public override void Update()
    {
        m_self.enemy.enemyBrain.Repath(m_self.Target.position);
        Vector3 desiredDir = m_self.enemy.enemyBrain.GetPathDirection();

        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            if (smoothedMoveDir.sqrMagnitude < 0.0001f)
                smoothedMoveDir = desiredDir;
            else
                smoothedMoveDir = Vector3.Slerp(
                    smoothedMoveDir,
                    desiredDir,
                    Mathf.Clamp01(m_self.chaseDirectionSlerpSpeed * Time.deltaTime)
                ).normalized;

            m_self.enemy.enemyLoco.SetMoveDirection(smoothedMoveDir);
        }
        else
        {
            m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
        }
        
        if (m_self.distanceToTarget() < m_self.stopRange)
            m_self.AfterChaseIntention();
    }

    public override void Exit()
    {
        // Debug.Log("Enemy Exited Chase State");
        smoothedMoveDir = Vector3.zero;
        m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
    }

    private Vector3 GetInitialMoveDirection()
    {
        Vector3 dir = m_self.enemy.enemyLoco.NewHorizontalVel;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = m_self.enemy.enemyAnim.transform.forward;

        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }
}
