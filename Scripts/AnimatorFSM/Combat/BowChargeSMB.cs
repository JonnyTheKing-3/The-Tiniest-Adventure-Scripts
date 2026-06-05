using UnityEngine;

public class BowChargeSMB : StateMachineBehaviour
{
    bool hasTriggeredShot;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Player.Instance._playerInputScript.InstantiateArrow();
        hasTriggeredShot = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasTriggeredShot)
            return;

        if (stateInfo.normalizedTime >= 1f &&
            !Player.Instance._playerInputScript.lockAimInputHeld)
        {
            hasTriggeredShot = true;
            Player.Instance._playerAnimation.TriggerBowShot();
        }
    }
}
