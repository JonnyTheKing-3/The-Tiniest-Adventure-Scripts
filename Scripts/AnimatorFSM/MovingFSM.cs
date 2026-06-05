using UnityEngine;

public class MovingFSM : StateMachineBehaviour
{
    PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask grassLayer;

    [Header("SFX Timing")]
    [Range(0f, 1f)] [SerializeField] private float playFootstepSFXTime1 = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float playFootstepSFXTime2 = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float playSwimSFXTime = 0.5f;

    private int footstep1PlayedLoop = -1;
    private int footstep2PlayedLoop = -1;
    private int swimSFXPlayedLoop = -1;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        footstep1PlayedLoop = -1;
        footstep2PlayedLoop = -1;
        swimSFXPlayedLoop = -1;

        // Do not keep going if we restarted this state because of load/death.
        if (SaveGameManager.Instance.loadRoutine != null || Player.Instance.startedDeathRoutine)
            return;

        Player.Instance.InAttack = false;

        if (Player.Instance._playerLocomotion.IsInState<PlayerLocomotionSwimState>())
            return;

        Player.Instance.playerState = Player.PlayerStates.normal;
        AnimationEvents.Instance.EnableActionMap();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (p_loco.IsInState<PlayerLocomotionSwimState>())
        {
            if (StartMenuManager.Instance.StartMenuIntro) return;
             
            HandleSwimSFX(animator, stateInfo);
        }
        else if (p_loco.IsInState<PlayerLocomotionRegularState>() && animator.GetFloat("InputMagnitude") > .1 && Player.Instance._playerLocomotion.stickToGround)
        {
            HandleFootstepSFX(animator, stateInfo);
        }
    }

    private void HandleSwimSFX(Animator animator, AnimatorStateInfo stateInfo)
    {
        int currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
        float currentLoopTime = stateInfo.normalizedTime - currentLoop;

        if (currentLoopTime < playSwimSFXTime || swimSFXPlayedLoop == currentLoop)
            return;

        AudioManager.Instance.PlaySwim(animator.transform);
        swimSFXPlayedLoop = currentLoop;
    }

    private void HandleFootstepSFX(Animator animator, AnimatorStateInfo stateInfo)
    {
        int currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
        float currentLoopTime = stateInfo.normalizedTime - currentLoop;

        if (currentLoopTime >= playFootstepSFXTime1 && footstep1PlayedLoop != currentLoop)
        {
            PlayFootstep(animator.transform);
            footstep1PlayedLoop = currentLoop;
        }
        else if (currentLoopTime >= playFootstepSFXTime2 && footstep2PlayedLoop != currentLoop)
        {
            PlayFootstep(animator.transform);
            footstep2PlayedLoop = currentLoop;
        }
    }

    private void PlayFootstep(Transform playFrom)
    {
        Collider surfaceCollider = Player.Instance._playerLocomotion.Surface.collider;
        int surfaceLayer = surfaceCollider.gameObject.layer;

        if (IsInLayerMask(surfaceLayer, groundLayer))       AudioManager.Instance.PlayHardFootstep(playFrom);
        else if (IsInLayerMask(surfaceLayer, grassLayer))   AudioManager.Instance.PlayGrassyFootstep(playFrom);
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask) => (layerMask.value & (1 << layer)) != 0;
}