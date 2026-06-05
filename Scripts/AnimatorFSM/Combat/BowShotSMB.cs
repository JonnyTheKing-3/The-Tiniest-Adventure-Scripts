using UnityEngine;

public class BowShotSMB : StateMachineBehaviour
{
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Player.Instance._playerInputScript.ShootArrow();
        AudioManager.Instance.PlayBowRelease(animator.transform);
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!Player.Instance._playerInputScript.lockAimInputHeld)
        {
            // Only go to regular if the player is aiming. This makes sure that if we somehow transition into another state (like knockback) before we leave bow shot, we won't override that state by going to regular
            if (Player.Instance._playerLocomotion.IsInState<PlayerLocomotionAimState>())
            {
                Player.Instance._playerLocomotion.GoToState("regular");
            }

            // Smoothly rotate player upright (the aim system makes the player model rotate vertically while keeping a close camera to give the illusion we are aiming, so when we end, we need to correct the rotation)
            Player.Instance._playerAnimation.StartCoroutine(Player.Instance._playerAnimation.RotateModelUpright());

            CamerasManager.Instance.SwitchToThirdPerson();
            Player.Instance.playerState = Player.PlayerStates.normal;
        }
        else
            Player.Instance._playerAnimation.TriggerBowCharge();
    }
}
