using UnityEngine;

public class GameTimelineManager : MonoBehaviour
{
    PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;

    public Talker IntroConvoGameObjectDummy;

    public void ResetPlayerMovementOverride() => p_loco.OverrideMovementDirection(false);
    public void SetPlayerMovementOverrideToForward() => p_loco.OverrideMovementDirection(true, Vector3.forward);


    public void ResetPlayerInputOverride() => p_loco.OverrideInputDirection(false);
    public void SetPlayerInputOverrideYToPositive() => p_loco.OverrideInputDirection(true, new Vector2(0f, 1f));


    public void TurnOnCinemachineBrain() => CamerasManager.Instance.cinemachineBrain.enabled = true;
    public void TurnOffCinemachineBrain() => CamerasManager.Instance.cinemachineBrain.enabled = false;

    public void PlayIntroConvo()
    {
        StartMenuManager.Instance.StartMenuIntro = false;
        DialogueManager.Instance.SetupConvo(IntroConvoGameObjectDummy.conversationOptions[0], IntroConvoGameObjectDummy.transform, IntroConvoGameObjectDummy.animator, false);
    }

}
