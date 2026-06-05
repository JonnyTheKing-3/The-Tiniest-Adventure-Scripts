using UnityEngine;

public class EnemyBowArrowFSM : EnemyLongRangeCombatBase
{
    [Space]
    public Animator bowAnimator;

    public GameObject arrowPrefab;
    public AttackData arrowAttackData;
    public float shotSpeed = 7;

    public Timer chargeTimer;
    public Timer Cooldown;

    public LayerMask hittableLayers;

    [HideInInspector] public Arrow currentArrow;

    protected override void Awake()
    {
        base.Awake();

        AddState("chargeArrow", new EnemyBowArrowChargeState(this));
        AddState("shootArrow", new EnemyBowArrowShootState(this));
        AddState("cooldown", new EnemyBowArrowCooldownState(this));
    }

    public override void AfterKeepDistanceIntention() => GoToState("chargeArrow");
}



public class EnemyBowArrowChargeState : State<EnemyBowArrowFSM>
{
    public EnemyBowArrowChargeState(EnemyBowArrowFSM self) : base(self) { }

    private int _chargeToken;

    public override void Enter()
    {
        if (m_self.enemy.enemyLoco.hitstopActive) return;

        // Debug.Log("COMBAT: Entered Charge");

        m_self.chargeTimer.isSetup = false; // In case it was still ticking from last time

        _chargeToken = m_self.enemy.enemyAnim.TriggerTokenedAnim(  // If we restart state before attack animations finish, this doesn't get called for the previous attacks instance. Which is good, because we want to start the cooldown at the end of the full attack (after combo)
            key: "Charge", 
            triggerName: "Charge", 
            () => {m_self.chargeTimer.Setup();}
        );
        m_self.bowAnimator.SetTrigger("BowCharge");
        MakeArrow();
    }


    void MakeArrow()
    {
        Quaternion rot = m_self.enemy.R_hand.rotation * Quaternion.Euler(0, 90f, 0f);// small offset to make the arrow face the right direction
        GameObject arrow = Object.Instantiate(m_self.arrowPrefab, m_self.enemy.R_hand.position, rot, m_self.enemy.R_hand);
        m_self.currentArrow = arrow.GetComponent<Arrow>();
    }

    

    public override void Update()
    {
        m_self.enemy.enemyAnim.RotateModelInstant();
        if (!m_self.chargeTimer.isSetup) return;

        m_self.chargeTimer.Tick();

        if (m_self.chargeTimer.Finished()) m_self.GoToState("shootArrow");
    }

    public override void Exit()
    {
        // Debug.Log("COMBAT: Exited Charge");
        m_self.enemy.enemyAnim.Cancel("Charge", _chargeToken);
    }
}

public class EnemyBowArrowShootState : State<EnemyBowArrowFSM>
{
    public EnemyBowArrowShootState(EnemyBowArrowFSM self) : base(self) { }

    int _shootToken;
    public override void Enter()
    {
        if (m_self.enemy.enemyLoco.hitstopActive) return;

        // Debug.Log("COMBAT: Entered Shoot");

        _shootToken = m_self.enemy.enemyAnim.TriggerTokenedAnim(  
            key: "Shoot", 
            triggerName: "Shoot", 
            () => m_self.GoToState("cooldown")
        );
        m_self.bowAnimator.SetTrigger("BowShot");

        AudioManager.Instance.PlayBowRelease(m_self.transform);
        ShootArrow();
    }

    void ShootArrow()
    {
        // Debug.Log("Shoot arrow here");
        if (m_self.currentArrow != null)
        {
            m_self.currentArrow.ArrowShot(m_self.enemy.enemyAnim.modelForward.position, m_self.shotSpeed, m_self.arrowAttackData, m_self.hittableLayers);
        }
    }

    public override void Update() { }

    public override void Exit()
    {
        // Debug.Log("COMBAT: Exited Shoot");
        m_self.enemy.enemyAnim.Cancel("Shoot", _shootToken);
    }
}

public class EnemyBowArrowCooldownState : State<EnemyBowArrowFSM>
{
    public EnemyBowArrowCooldownState(EnemyBowArrowFSM self) : base(self) { }

    public override void Enter()
    {
        // Debug.Log("COMBAT: Entered Cooldown");
        m_self.enemy.enemyLoco.SetMoveDirection(Vector3.zero);
        m_self.Cooldown.Setup();
    }

    public override void Update()
    {
        m_self.Cooldown.Tick();

        if (m_self.Cooldown.Finished()) 
            m_self.GoToState("alerted");
    }

    public override void Exit()
    {
        // Debug.Log("COMBAT: Exited Cooldown");
    }
}
