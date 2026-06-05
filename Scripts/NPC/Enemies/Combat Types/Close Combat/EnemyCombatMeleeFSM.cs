using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatMeleeFSM : EnemyCloseCombatBase
{
    [Space]
    public Timer attackCooldown;

    protected override void Awake()
    {
        base.Awake();

        AddState("attack", new EnemyMeleeAttackState(this));
    }

    public override void AfterChaseIntention() => GoToState("attack");

    public virtual void AfterAttackIntention() => GoToState("alerted"); // Face target before deciding whether to chase or attack again
}


public class EnemyMeleeAttackState : State<EnemyCombatMeleeFSM>
{
    public EnemyMeleeAttackState(EnemyCombatMeleeFSM self) : base(self) { }

    private int _attackToken;


    public override void Enter()
    {
        if (m_self.enemy.enemyLoco.hitstopActive) return;

        // Debug.Log("COMBAT: Entered Attack");

        m_self.attackCooldown.isSetup = false; // In case it was still ticking from last time

        // Debug.Log("Entered Attack State");
        _attackToken = m_self.enemy.enemyAnim.TriggerTokenedAnim(  // If we restart state before attack animations finish, this doesn't get called for the previous attacks instance. Which is good, because we want to start the cooldown at the end of the full attack (after combo)
            key: "Attack", 
            triggerName: "Attack", 
            () => m_self.attackCooldown.Setup()
        );
    }

    public override void Update()
    {
        if (!m_self.attackCooldown.isSetup) return;

        m_self.attackCooldown.Tick();

        if (m_self.attackCooldown.Finished()) m_self.AfterAttackIntention();
    }

    public override void Exit()
    {
        // Debug.Log("COMBAT: Exited Attack");
        // Debug.Log("================================");
        m_self.enemy.enemyAnim.Cancel("Attack", _attackToken);
    }
}
