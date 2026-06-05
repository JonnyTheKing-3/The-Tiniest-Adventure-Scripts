using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AnimationEvents : MonoBehaviour
{
    public static AnimationEvents Instance { get; private set; }
    InputActionMap PlayerMap => Player.Instance._playerInputScript._playerInput.actions.FindActionMap("Player");
    InputActionMap map => Player.Instance._playerInputScript._playerInput.currentActionMap;
    PlayerAnimation p_anim => Player.Instance._playerAnimation;
    PlayerInputScript p_input => Player.Instance._playerInputScript;
    PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;
    PlayerCombat p_combat => Player.Instance._playerCombat;
    Player player_ => Player.Instance;
    void Awake() => Instance = this;



    public void DisableActionMap() => map.Disable();
    public void DisablePlayerActionMap() => PlayerMap.Disable();

    public void DisableActionsInMapExcept(HashSet<string> exception) { foreach (var action in map.actions) { if (exception.Contains(action.name)) action.Enable(); else action.Disable(); } }
    public void DisableActionsInPlayerMapExcept(HashSet<string> exception) { foreach (var action in PlayerMap.actions) { if (exception.Contains(action.name)) action.Enable(); else action.Disable(); } }
    
    public void DisableAction(string action) => map.FindAction(action)?.Disable();
    public void DisableNonEssentialActions()
    {
        // Technically, an array or something like that would be better, but this is fine for me to be honest. It's easy to just add another line. I also like how readable it is.
        DisableAction("Move");
        DisableAction("Attack");
        DisableAction("Dodge");
        DisableAction("Jump");
        DisableAction("Aim");
        DisableAction("Interact");
        DisableAction("Block");
    }
    public void DisableNonEssentialActionsExceptBlock()
    {
        DisableAction("Move");
        DisableAction("Attack");
        DisableAction("Dodge");
        DisableAction("Jump");
        DisableAction("Aim");
        DisableAction("Interact");
    }


    public void EnableActionMap() => map.Enable();
    public void EnablePlayerActionMap() => PlayerMap.Enable();
    public void EnableAction(string action) => map.FindAction(action)?.Enable();

    public void EndAttackAndEnableActionMap() { player_.CanFollowUpAttack = false; map.Enable(); }
    public void ActivateCanAttackAndCheckForAttackBuffer()
    {
        float t = Time.time - p_input.lastAttackBufferAttemptTime;

        if (t < player_.AttackBuffer)
        {
            // Debug.Log("Executing buffered attack");
            p_input.Attack();
        }
        else
            player_.CanFollowUpAttack = true;
    }

    public void PushForward(float force) => p_loco.rb.AddForce(p_loco.GetSlopeForward(false) * force, ForceMode.Impulse);
    
}
