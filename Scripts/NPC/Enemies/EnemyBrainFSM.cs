using UnityEngine;
using UnityEngine.AI;

public class EnemyBrainFSM : FiniteStateMachine
{
    [HideInInspector] public Enemy enemy;
    [HideInInspector] public NavMeshAgent agent;
    public string CurrentStateName;

    [Header("PATROL")]
    public Transform patrolCenter;
    public float patrolRadius = 5f;

    [Header("DETECTION")]
    public float detectionRadius = 7f;

    [Header("DECISION")]
    public Timer decisionCooldown;

    [Header("NavMesh")]
    public float reachedDistance = 1f;
    public float samplePositionRadiusCheck = 2f;
    public float minDistanceFromSamplePoint = 3f;
    public Timer repathRateTimer;

    [Header("Avoidance")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationWeight = 0.75f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int maxSeparationHits = 8;
    private Collider[] separationHits;
    public bool ShouldUnregisterFromCombat { get; private set; }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = true;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Random priority helps prevent all enemies from making the same avoidance decision.
        agent.avoidancePriority = Random.Range(30, 70);

        agent.nextPosition = transform.position;

        separationHits = new Collider[maxSeparationHits];

        repathRateTimer.Setup();

        AddState("decide", new EnemyBrainDecideState(this));
        AddState("patrol", new EnemyBrainPatrolState(this));
        AddState("idle", new EnemyBrainIdleState(this));
        AddState("combat", new EnemyBrainCombatState(this));
        AddState("returnToBase", new EnemyBrainReturnToBaseState(this));

        GoToState("decide");
    }

    protected override void Update()
    {
        base.Update();

        SyncAgentToCharacterController();

        CurrentStateName = CurrentState?.GetType().Name.Replace("EnemyBrain", "").Replace("State", "") ?? "null";

        float dist = Vector3.Distance(transform.position, Player.Instance.transform.position);
        if (dist < detectionRadius && BrainCanTransitionToCombat() && LocomotionAllowsCombat() && SaveGameManager.Instance.loadRoutine == null)
        {
            GoToState("combat");
        }

        if (IsInState<EnemyBrainCombatState>() && GameManager.Instance._currPlayMode == GameManager.PlayMode.GameOver)
        {
            MarkCombatDisengaged();
            GoToState("decide");
            return;
        }

        if (EnemyIsTrackedInCombat()
            && !IsInState<EnemyBrainReturnToBaseState>()
            && LocomotionAllowsCombat()
            && ShouldReturnToBase())
        {
            MarkCombatDisengaged();
            GoToState("returnToBase");
        }
    }

    void SyncAgentToCharacterController() => agent.nextPosition = transform.position;
    public bool LocomotionAllowsCombat() => enemy.enemyLoco.IsInState<EnemyLocomotionRegularState>();

    public void MarkCombatDisengaged() => ShouldUnregisterFromCombat = true;
    public void MarkCombatInterrupted() => ShouldUnregisterFromCombat = false;
    public bool EnemyIsTrackedInCombat() => GameManager.Instance.enemiesInCombat.Contains(enemy);

    public bool ShouldReturnToBase()
    {
        float distanceFromBase = Vector3.Distance(transform.position, patrolCenter.position);
        float returnThreshold = enemy.enemyCombat.returnToBaseDistanceThreshold;

        return distanceFromBase > returnThreshold || SaveGameManager.Instance.loadRoutine != null;
    }

    public bool BrainCanTransitionToCombat()
    {
        // Decide and patrol can transition to combat. We can only go to combat if we are in the locomotion regular state. Note that the combat state is when the enemy is attacking, not when they are being attacked
        return !IsInState<EnemyBrainCombatState>()
            && !IsInState<EnemyBrainReturnToBaseState>()
            && !IsInState<EnemyBrainIdleState>()
            && GameManager.Instance._currPlayMode != GameManager.PlayMode.GameOver;
    }

    public void Repath(Vector3 destination)  // Need this to not get stuck. Just in case
    {
        repathRateTimer.Tick();
        if (!repathRateTimer.Finished()) return;

        repathRateTimer.Setup();
        agent.SetDestination(destination);
    }

    public Vector3 GetPathDirection()
    {
        if (agent.pathPending) return Vector3.zero;
        if (!agent.hasPath) return Vector3.zero;

        Vector3 dir = agent.desiredVelocity; // desiredVelocity is better than steeringTarget for this because it uses the built in obstacle avoidance, unlike
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    public bool ReachedDestination(float stopDistance = -1f)
    {
        if (!HasUsablePath()) return false;

        float threshold = stopDistance > 0f ? stopDistance : reachedDistance;
        return agent.remainingDistance <= threshold;
    }

    // pathPending is false if we tried but failed to calculatte a path. Just a little note
    public bool HasUsablePath() => !agent.pathPending && agent.hasPath;

    public bool TryGetRandomPatrolPoint(Vector3 samplePoint, float radius, out Vector3 result, float samplePosRadiusCheck)
    {
        result = transform.position;

        float minRadiusSqr = minDistanceFromSamplePoint * minDistanceFromSamplePoint;

        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistanceFromSamplePoint, radius);

            Vector3 candidate = samplePoint + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, samplePosRadiusCheck, NavMesh.AllAreas))
            {
                Vector3 flatOffset = hit.position - samplePoint;
                Vector3 fromEnemy = hit.position - transform.position;

                flatOffset.y = 0f;

                if (flatOffset.sqrMagnitude >= minRadiusSqr && fromEnemy.sqrMagnitude >= minRadiusSqr)
                {
                    // Debug.Log($"Found patrol point");
                    result = hit.position;
                    return true;
                }
            }
        }

        // Debug.Log("Failed to find a valid patrol point");
        return false;
    }
    
    public Vector3 GetAvoidanceAdjustedPathDirection()
    {
        Vector3 pathDir = GetPathDirection();
        Vector3 separationDir = GetSeparationDirection();

        Vector3 finalDir = pathDir + separationDir * separationWeight;
        finalDir.y = 0f;

        if (finalDir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return finalDir.normalized;
    }

    private Vector3 GetSeparationDirection()
    {
        if (separationHits == null || separationHits.Length != maxSeparationHits)
            separationHits = new Collider[maxSeparationHits];

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            separationHits,
            enemyLayer,
            QueryTriggerInteraction.Ignore
        );

        Vector3 separation = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = separationHits[i];

            if (hit == null) continue;
            if (hit.transform == transform) continue;

            Vector3 away = transform.position - hit.transform.position;
            away.y = 0f;

            float sqrDistance = away.sqrMagnitude;
            if (sqrDistance < 0.0001f) continue;

            // Closer enemies push harder.
            separation += away.normalized / sqrDistance;
        }

        return separation;
    }

    void OnDrawGizmosSelected()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null) return;
        if (!agent.hasPath) return;
    
        Vector3[] corners = agent.path.corners;
        if (corners == null || corners.Length < 2) return;
    
        Gizmos.color = Color.green;
    
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
            Gizmos.DrawSphere(corners[i], 0.15f);
        }
    
        Gizmos.DrawSphere(corners[corners.Length - 1], 0.15f);
    }
}

public class EnemyBrainDecideState : State<EnemyBrainFSM>
{
    public EnemyBrainDecideState(EnemyBrainFSM self) : base(self) { }

    public override void Enter()
    {
        m_self.agent.ResetPath();
        
        m_self.enemy.enemyLoco.moveDirection = Vector3.zero;
        m_self.decisionCooldown.Setup();
    }

    public override void Update()
    {
        m_self.decisionCooldown.Tick();

        if (m_self.decisionCooldown.Finished())
            m_self.GoToState("patrol");
    }

    public override void Exit()
    {
    }
}

public class EnemyBrainIdleState : State<EnemyBrainFSM>
{
    public EnemyBrainIdleState(EnemyBrainFSM self) : base(self) { }

    public override void Enter()
    {
        m_self.agent.ResetPath();
        m_self.enemy.enemyLoco.moveDirection = Vector3.zero;
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}

public class EnemyBrainPatrolState : State<EnemyBrainFSM>
{
    public EnemyBrainPatrolState(EnemyBrainFSM self) : base(self) { }

    public Vector3 patrolTarget;
    float timer = 0f;

    public override void Enter()
    {
        timer = 0f;

        if (m_self.TryGetRandomPatrolPoint(m_self.patrolCenter.position, m_self.patrolRadius, out patrolTarget, m_self.samplePositionRadiusCheck))
        {
            m_self.agent.SetDestination(patrolTarget);
        }
        else
        {
            patrolTarget = m_self.transform.position;
            m_self.GoToState("decide");
        }
    }

    public override void Update()
    {
        m_self.Repath(patrolTarget);

        Vector3 dir = m_self.GetAvoidanceAdjustedPathDirection();
        m_self.enemy.enemyLoco.SetMoveDirection(dir);

        timer += Time.deltaTime;

        bool noPathAfterTooLong = !m_self.agent.pathPending && !m_self.agent.hasPath && timer > 0.25f; // if we do have a path but it's taking too long to generate, then leave. 
        bool stuck = m_self.enemy.enemyLoco.currentVelocity.magnitude < 0.15f && timer > 1.5f;

        if (m_self.ReachedDestination(1f) || noPathAfterTooLong || stuck)
        {
            // if (noPathAfterTooLong) Debug.Log("No path after too long, leaving patrol state");
            // if (stuck) Debug.Log("Stuck, leaving patrol state");
            // if (m_self.ReachedDestination(1f)) Debug.Log("Reached destination according to agent");

            m_self.GoToState("decide");
        }
    }

    public override void Exit()
    {
        m_self.agent.ResetPath();
        m_self.enemy.enemyLoco.moveDirection = Vector3.zero;
        // Debug.Log("Exiting patrol, going to decide");
    }
}

public class EnemyBrainCombatState : State<EnemyBrainFSM>
{
    public EnemyBrainCombatState(EnemyBrainFSM self) : base(self) { }

    public override void Enter()
    {
        m_self.MarkCombatInterrupted();
        m_self.agent.ResetPath();
        m_self.enemy.enemyCombat.SetTarget(Player.Instance.transform);
        m_self.enemy.enemyCombat.StartCombatCycle();
        m_self.enemy.enemyState = Enemy.EnemyStates.Combat;
        GameManager.Instance.RegisterEnemyInCombat(m_self.enemy);
    }

    public override void Update()
    {
        if (m_self.ShouldReturnToBase())
        {
            m_self.MarkCombatDisengaged();
            m_self.GoToState("returnToBase");
            return;
        }

        if (!m_self.LocomotionAllowsCombat())
        {
            m_self.MarkCombatInterrupted();
            m_self.GoToState("idle");
        }
    }

    public override void Exit()
    {
        m_self.enemy.enemyCombat.GoToState("idle");
        m_self.enemy.enemyState = Enemy.EnemyStates.Idle;

        if (m_self.ShouldUnregisterFromCombat)
            GameManager.Instance.UnregisterEnemyInCombat(m_self.enemy);
    }
}

public class EnemyBrainReturnToBaseState : State<EnemyBrainFSM>
{
    public EnemyBrainReturnToBaseState(EnemyBrainFSM self) : base(self) { }

    float timer = 0f;


    public override void Enter()
    {
        timer = 0f;

        if (m_self.ShouldUnregisterFromCombat)
            GameManager.Instance.UnregisterEnemyInCombat(m_self.enemy);

        m_self.agent.SetDestination(m_self.patrolCenter.position);
    }

    public override void Update()
    {
        m_self.Repath(m_self.patrolCenter.position);

        Vector3 dir = m_self.GetAvoidanceAdjustedPathDirection();
        m_self.enemy.enemyLoco.SetMoveDirection(dir);

        timer += Time.deltaTime;

        bool noPathAfterTooLong = !m_self.agent.pathPending && !m_self.agent.hasPath && timer > 0.25f;
        bool stuck = m_self.enemy.enemyLoco.currentVelocity.magnitude < 0.15f && timer > 1.5f;

        if (m_self.ReachedDestination(3f) || noPathAfterTooLong || stuck)
        {
            // if (noPathAfterTooLong) Debug.Log("No path after too long, leaving returnToBase state");
            // if (stuck) Debug.Log("Stuck, leaving returnToBase state");
            // if (m_self.ReachedDestination(1f)) Debug.Log("Reached destination according to agent in returnToBase state");

            m_self.GoToState("decide");
        }
    }

    public override void Exit()
    {
        m_self.agent.ResetPath();
        m_self.enemy.enemyLoco.moveDirection = Vector3.zero;
        // Debug.Log("Exiting return to base, going to decide");
    }
}
