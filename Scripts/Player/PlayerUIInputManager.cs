using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIInputManager : MonoBehaviour
{    
    PlayerInput playerInput; // Makes it easier to have here and use initialize in awake then have to reference the PlayerInputScript every time I want to use it.

    // These actions can only be used in one instance of UI and there won't be many actions for UI. 
    // So if it's on dialogue, it can't be used in pause menu or anything like that and vice versa.
    // Because of this. I find it a lot simpler to just have them here and reference them from the UI scripts instead of having them be a complex like with the PlayerInputScript
    
    [HideInInspector] public InputAction NavigateNonInputModuleAction { get; private set; }
    [HideInInspector] public InputAction RightStickNavigateAction { get; private set; }
    [HideInInspector] public InputAction SubmitAction { get; private set; }
    [HideInInspector] public InputAction CancelAction { get; private set; }
    [HideInInspector] public InputAction ScrollLeftAction { get; private set; }
    [HideInInspector] public InputAction ScrollRightAction { get; private set; }
    [HideInInspector] public InputAction TabLeftAction { get; private set; }
    [HideInInspector] public InputAction TabRightAction { get; private set; }
    [HideInInspector] public InputAction ExitAction { get; private set; }

    private void Awake() // Not awake so that PlayerInputScript can initialize first and set playerInput
    {
        playerInput = GetComponent<PlayerInput>();

        NavigateNonInputModuleAction = playerInput.actions["NavigateNonInputModule"];
        RightStickNavigateAction = playerInput.actions["RightStickNavigate"];
        SubmitAction = playerInput.actions["Submit"];
        CancelAction = playerInput.actions["Cancel"];
        ScrollLeftAction = playerInput.actions["ScrollLeft"];
        ScrollRightAction = playerInput.actions["ScrollRight"];
        TabLeftAction = playerInput.actions["TabLeft"];
        TabRightAction = playerInput.actions["TabRight"];
        ExitAction = playerInput.actions["Exit"];
        playerInput.actions.FindActionMap("UI").Disable();


    }

    public void SwitchToUI()
    {
        playerInput.SwitchCurrentActionMap("UI");
    }

    public void SwitchToPlayer()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }

    private static readonly HashSet<string> Actions = new() { "Submit", "Navigate" };
    public void SwitchToUIDialogue()
    {
        playerInput.SwitchCurrentActionMap("UI");
        AnimationEvents.Instance.DisableActionsInMapExcept(Actions);
    }
}