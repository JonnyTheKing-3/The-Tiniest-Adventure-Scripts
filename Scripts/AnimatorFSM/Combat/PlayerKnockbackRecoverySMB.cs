using UnityEngine;

public class PlayerKnockbackRecoverySMB : StateMachineBehaviour
{
    [Tooltip("Must match the key used in PlayerLocomotionRecoveryState.Enter")]
    public string key = "Recovery";

    private int _tokenAtEnter;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _tokenAtEnter = Player.Instance._playerAnimation.CurrentToken(key);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Player.Instance._playerAnimation.NotifyFinished(key, _tokenAtEnter);
    }
}
