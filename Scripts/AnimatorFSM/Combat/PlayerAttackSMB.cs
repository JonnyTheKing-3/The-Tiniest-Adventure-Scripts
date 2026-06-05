using UnityEngine;

public class PlayerAttackSMB : StateMachineBehaviour
{
    /// <summary>
    /// All player attack animations use this FSM to manage combat animation events exclusive to the player 
    /// </summary>
   

    public int attackIndex; [Space]
    public float CanFollowUpAttackTime;
    public float EndAttackAndEnableActionMapTime;
    bool endedAttack = false;

    IWeaponHolder weaponHolder;
    Weapon weaponData;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackIndex < 0) return;

        // Debug.Log("PlayerAttackSMB disabling move ");
        Player.Instance._playerInputScript.EnteringAttack();

        Transform parentTransform = animator.transform.parent.transform; // model + animator are children of the main objects

        // Assign weapon and attack data based on index
        if (parentTransform.TryGetComponent<IWeaponHolder>(out weaponHolder))
        {
            weaponData = weaponHolder.GetWeapon();
            if (weaponData == null) Debug.LogError("WeaponData is null");

            CanFollowUpAttackTime = weaponData.weaponTemplate.attackDatas[attackIndex].hit.CanFollowUpAttackTime;
            EndAttackAndEnableActionMapTime = weaponData.weaponTemplate.attackDatas[attackIndex].hit.EndAttackAndEnableActionMapTime;
        }
        else
        {
            Debug.LogError("No IWeaponHolder found on player weapon holder");
        }

        endedAttack = false;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (attackIndex < 0) return;

        float t = Mathf.Repeat(stateInfo.normalizedTime, 1f);

        if (t >= CanFollowUpAttackTime && CanFollowUpAttackTime > 0.01f && Player.Instance.CanFollowUpAttack == false)
        {
            AnimationEvents.Instance.ActivateCanAttackAndCheckForAttackBuffer();
            CanFollowUpAttackTime = -1f;
        }

        if (t >= EndAttackAndEnableActionMapTime && t > 0.01f && !endedAttack)
        {
            AnimationEvents.Instance.EnableActionMap();
            endedAttack = true;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Player.Instance.CanFollowUpAttack = false;

        // don't reset for attack clips because an input will exit the incoming attack state
        if (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Player.Instance.InAttack = false;
            Player.Instance.playerState = Player.PlayerStates.normal;
        }
        else
        {
            AnimationEvents.Instance.DisableAction("Move");
            animator.SetFloat("InputMagnitude", 0f);
        }
    }
}
