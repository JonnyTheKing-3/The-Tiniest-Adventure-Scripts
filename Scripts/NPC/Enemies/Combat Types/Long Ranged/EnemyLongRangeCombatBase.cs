using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyLongRangeCombatBase : EnemyCombatBase
{
    [Space]
    public float minDistanceThreshold = 10f;
    public float maxDistanceThreshold = 20f;
    [Tooltip("Timer for keepDistance state. Ensures state doesn't take too long thinking about the next spot. If enemy hasn't found a good spot, it will exit the state.")]public Timer keepDistanceDecisionTimer;
    public Timer keepDistanceTimer;

    protected override void Awake()
    {
        base.Awake();

        AddState("keepDistance", new EnemyLongRangeKeepDistanceState(this));
    }

    public override void AfterAlertedIntention() => GoToState("keepDistance");
    public abstract void AfterKeepDistanceIntention(); // For subclasses to implement
}

public class EnemyLongRangeKeepDistanceState : State<EnemyLongRangeCombatBase>
{
    public EnemyLongRangeKeepDistanceState(EnemyLongRangeCombatBase self) : base(self) { }


    Vector3? possibleFleePos;
    public override void Enter()
    {
        // Debug.Log("COMBAT: Entered Keep Distance");
        m_self.enemy.enemyBrain.agent.SetDestination(m_self.Target.position);   // player is the default movement optiion default
        possibleFleePos = null;
        m_self.keepDistanceDecisionTimer.Setup();
        m_self.keepDistanceTimer.Setup();
    }

    public override void Update()
    {
        m_self.keepDistanceTimer.Tick();
        
        if ((m_self.distanceToTarget() > m_self.minDistanceThreshold && m_self.distanceToTarget() < m_self.maxDistanceThreshold)
             || m_self.keepDistanceDecisionTimer.Finished()
             || m_self.keepDistanceTimer.Finished())
        {
            m_self.AfterKeepDistanceIntention();
            return;
        }
        

         // Get closer
        if (m_self.distanceToTarget() > m_self.maxDistanceThreshold)
        {
            SetPath(m_self.Target.position);
        }

        // Retreat
        else
        {
            if (possibleFleePos == null)
            {
                m_self.keepDistanceDecisionTimer.Tick();
                Vector3 awayFromTarget = (m_self.transform.position - m_self.Target.position).normalized * m_self.maxDistanceThreshold;

                if (m_self.enemy.enemyBrain.TryGetRandomPatrolPoint(
                                                m_self.transform.position + awayFromTarget, 
                                                m_self.enemy.enemyBrain.samplePositionRadiusCheck, 
                                                out Vector3 fleeTarget, 
                                                m_self.maxDistanceThreshold/2))
                {
                    // Debug.Log("Fleeing...");
                    possibleFleePos = fleeTarget;
                    m_self.enemy.enemyBrain.agent.ResetPath();
                    m_self.enemy.enemyBrain.agent.SetDestination(possibleFleePos.Value); 
                }
            }
            else
            {
                SetPath(possibleFleePos.Value);
            }
        }
        
    }

    void SetPath(Vector3 targetPosition)
    {
        m_self.enemy.enemyBrain.Repath(targetPosition);
        Vector3 dir = m_self.enemy.enemyBrain.GetPathDirection();
        m_self.enemy.enemyLoco.SetMoveDirection(dir);
    }

    public override void Exit()
    {
        // Debug.Log("Enemy Exited Chase State");
        m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
        m_self.enemy.enemyBrain.agent.ResetPath();
    }



    private void OnDrawGizmos()
    {
        if (possibleFleePos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(possibleFleePos.Value, 0.5f);
        }

        Debug.DrawRay(m_self.transform.position, (m_self.transform.position - m_self.Target.position).normalized * m_self.maxDistanceThreshold, Color.blue);
    }
}
