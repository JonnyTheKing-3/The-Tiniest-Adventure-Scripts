using UnityEngine;

public class DashSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Debug.Log("DashSMB Disabling Move");
        Player.Instance._playerInputScript.EnteringAttack();
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.ResetTrigger("DashExit"); // In case of exiting dash via other way other than exit trigger
        AnimationEvents.Instance.DisableAction("Move");
        animator.SetFloat("InputMagnitude", 0f);
    }
}