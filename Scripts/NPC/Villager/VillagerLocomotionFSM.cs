using UnityEngine;

public class VillagerLocomotionFSM : NPCLocomotion
{
    protected override void Awake()
    {
        base.Awake();

        AddState("regular", new VillagerLocomotionRegularState(this));
        GoToState("regular");
    }

    protected override void Update()
    {
        UpdateGroundingStatus();
        base.Update();
    }
}


public class VillagerLocomotionRegularState : State<VillagerLocomotionFSM>
{
    public VillagerLocomotionRegularState(VillagerLocomotionFSM self) : base(self) { }

    public override void Enter() { }

    public override void Update() => m_self.Move();

    public override void Exit() { }
}