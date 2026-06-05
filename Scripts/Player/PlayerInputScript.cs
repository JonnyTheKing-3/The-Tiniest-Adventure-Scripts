using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerInputScript : MonoBehaviour
{
    PlayerAnimation p_anim => Player.Instance._playerAnimation;
    PlayerLocomotionFSM p_loco => Player.Instance._playerLocomotion;
    PlayerCombat p_combat => Player.Instance._playerCombat;
    float bestTargetAlphaUIApproachable => GameManager.Instance.UIApproachbleFadeBestTarget;
    float normalTargetAlphaUIApproachable => GameManager.Instance.UIApproachbleFadeNormalTarget;

    [HideInInspector] public PlayerInput _playerInput;
    [HideInInspector] public bool blockInputHeld = false;

    [Header("Settings")]
    public float interactableDistance;
    public float lockonDistance = 9f;
    [SerializeField] private float chestApproachableDistance = 1f;
    public IInteractable bestInteractable;
    [Space]
    [SerializeField] private LayerMask approachableScanLayerMask = ~0;
    [SerializeField] private int approachableScanBufferSize = 32;
    [SerializeField] private float approachableScanInterval = 0.05f;
    
    [HideInInspector] public float lastAttackBufferAttemptTime = 0f;
    private Collider[] approachableScanResults;
    private readonly HashSet<IApproachable> approachablesNearPlayer = new();
    private readonly HashSet<IApproachable> approachablesFoundThisScan = new();
    private readonly List<IApproachable> approachablesToUnregister = new();
    private float nextApproachableScanTime;


    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        dashTarget = null;
        approachableScanResults = new Collider[approachableScanBufferSize];

        // Register to input action events for Player map
        _playerInput.actions["Move"].started += OnMoveStarted;
        _playerInput.actions["Move"].performed += OnMovePerformed;
        _playerInput.actions["Move"].canceled += OnMoveCanceled;

        _playerInput.actions["Jump"].started += OnJumpStarted;
        _playerInput.actions["Jump"].performed += OnJumpPerformed;
        _playerInput.actions["Jump"].canceled += OnJumpCanceled;

        _playerInput.actions["Attack"].started += OnAttackStarted;
        _playerInput.actions["Attack"].performed += OnAttackPerformed;
        _playerInput.actions["Attack"].canceled += OnAttackCanceled;

        _playerInput.actions["Look"].started += OnLookStarted;
        _playerInput.actions["Look"].performed += OnLookPerformed;
        _playerInput.actions["Look"].canceled += OnLookCanceled;

        _playerInput.actions["LockOn"].started += OnLockOnStarted;
        _playerInput.actions["LockOn"].performed += OnLockOnPerformed;
        _playerInput.actions["LockOn"].canceled += OnLockOnCanceled;

        _playerInput.actions["Aim"].started += OnAimStarted;
        _playerInput.actions["Aim"].performed += OnAimPerformed;
        _playerInput.actions["Aim"].canceled += OnAimCanceled;

        _playerInput.actions["Interact"].started += OnInteractStarted;
        _playerInput.actions["Interact"].performed += OnInteractPerformed;
        _playerInput.actions["Interact"].canceled += OnInteractCanceled;

        _playerInput.actions["Dodge"].started += OnDodgeStarted;
        _playerInput.actions["Dodge"].performed += OnDodgePerformed;
        _playerInput.actions["Dodge"].canceled += OnDodgeCanceled;

        _playerInput.actions["Block"].started += OnBlockStarted;
        _playerInput.actions["Block"].performed += OnBlockPerformed;
        _playerInput.actions["Block"].canceled += OnBlockCanceled;

        _playerInput.actions["Load"].started += OnLoadStarted;
        _playerInput.actions["Load"].performed += OnLoadPerformed;
        _playerInput.actions["Load"].canceled += OnLoadCanceled;

        _playerInput.actions["TestingInput"].started += OnTestingInputStarted;
        _playerInput.actions["TestingInput"].performed += OnTestingInputPerformed;
        _playerInput.actions["TestingInput"].canceled += OnTestingInputCanceled;

        _playerInput.actions["Pause"].started += OnPauseStarted;
        _playerInput.actions["Pause"].performed += OnPausePerformed;
        _playerInput.actions["Pause"].canceled += OnPauseCanceled;

    }

    private void OnDestroy()
    {
        ClearApproachablesNearPlayer();

        // Unregister from input action events
        _playerInput.actions["Move"].started -= OnMoveStarted;
        _playerInput.actions["Move"].performed -= OnMovePerformed;
        _playerInput.actions["Move"].canceled -= OnMoveCanceled;

        _playerInput.actions["Jump"].started -= OnJumpStarted;
        _playerInput.actions["Jump"].performed -= OnJumpPerformed;
        _playerInput.actions["Jump"].canceled -= OnJumpCanceled;

        _playerInput.actions["Attack"].started -= OnAttackStarted;
        _playerInput.actions["Attack"].performed -= OnAttackPerformed;
        _playerInput.actions["Attack"].canceled -= OnAttackCanceled;

        _playerInput.actions["Look"].started -= OnLookStarted;
        _playerInput.actions["Look"].performed -= OnLookPerformed;
        _playerInput.actions["Look"].canceled -= OnLookCanceled;

        _playerInput.actions["LockOn"].started -= OnLockOnStarted;
        _playerInput.actions["LockOn"].performed -= OnLockOnPerformed;
        _playerInput.actions["LockOn"].canceled -= OnLockOnCanceled;

        _playerInput.actions["Aim"].started -= OnAimStarted;
        _playerInput.actions["Aim"].performed -= OnAimPerformed;
        _playerInput.actions["Aim"].canceled -= OnAimCanceled;

        _playerInput.actions["Interact"].started -= OnInteractStarted;
        _playerInput.actions["Interact"].performed -= OnInteractPerformed;
        _playerInput.actions["Interact"].canceled -= OnInteractCanceled;

        _playerInput.actions["Dodge"].started -= OnDodgeStarted;
        _playerInput.actions["Dodge"].performed -= OnDodgePerformed;
        _playerInput.actions["Dodge"].canceled -= OnDodgeCanceled;

        _playerInput.actions["Block"].started -= OnBlockStarted;
        _playerInput.actions["Block"].performed -= OnBlockPerformed;
        _playerInput.actions["Block"].canceled -= OnBlockCanceled;

        _playerInput.actions["Load"].started -= OnLoadStarted;
        _playerInput.actions["Load"].performed -= OnLoadPerformed;
        _playerInput.actions["Load"].canceled -= OnLoadCanceled;

        _playerInput.actions["TestingInput"].started -= OnTestingInputStarted;
        _playerInput.actions["TestingInput"].performed -= OnTestingInputPerformed;
        _playerInput.actions["TestingInput"].canceled -= OnTestingInputCanceled;

        _playerInput.actions["Pause"].started -= OnPauseStarted;
        _playerInput.actions["Pause"].performed -= OnPausePerformed;
        _playerInput.actions["Pause"].canceled -= OnPauseCanceled;
    }

    private void OnDisable() => ClearApproachablesNearPlayer();


    // Used to fade the approach icon for all approachables near player
    void Update()
    {
        ScanForApproachables();

        if (approachablesNearPlayer.Count == 0)
        {
            bestInteractable = null;
            return;
        }

        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Dialogue)
        {
            FadeApproachablesNearPlayer(0f);
            return;
        }

        bestInteractable = GetBestApproachableForInteraction();

        foreach(IApproachable approachable in approachablesNearPlayer)
        {
            float targetAlpha = approachable == bestInteractable ? bestTargetAlphaUIApproachable : normalTargetAlphaUIApproachable;
            approachable.FadeApproachUI(targetAlpha);
        }
    }

    private void OnMoveStarted(InputAction.CallbackContext context)
    {
        // Debug.Log("Move started");
        Player.Instance._playerLocomotion.inputDir = context.ReadValue<Vector2>();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Player.Instance._playerLocomotion.inputDir = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        Player.Instance._playerLocomotion.inputDir = context.ReadValue<Vector2>();
    }



    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        Debug.Log($"JUMP START");
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        Debug.Log($"JUMP PERFORMED");
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("CANCELED JUMP");
    }


    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        if (p_combat.GetWeapon().weaponTemplate == null) return;
        
        if (p_loco.CurrentStateName.Contains("Block")) return;                  // We don't allow attack here instead of disabling because the MovementFSM enables attack even when blocking because blocking is a layered animation

        if (Player.Instance.PerfectDodgeActive)                                 // Attack becomes active during perfect dodge window
        {
            p_loco.StartCounterConfirmFromPerfectDodge();
            return;
        }

        if (!Player.Instance.InAttack || Player.Instance.CanFollowUpAttack)     // Defualt behavior
            Attack();
        else
            lastAttackBufferAttemptTime = Time.time;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log("ATTACK PERFORMED");
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
    }



    private void OnLookStarted(InputAction.CallbackContext context)
    {
        CamerasManager.Instance.c_LockOn.look = context.ReadValue<Vector2>();
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        CamerasManager.Instance.c_LockOn.look = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        CamerasManager.Instance.c_LockOn.look = context.ReadValue<Vector2>();
    }



    public void OnLockOnStarted(InputAction.CallbackContext context)
    {
        // Debug.Log($"LockOn STARTED");
        if (CamerasManager.Instance.CameraState == CamerasManager.CameraStates.LockOn) { return; }

        IInteractable I = GetTargetForInteraction(lockonDistance);

        if (I is MonoBehaviour target && I is IHittable)
        {
            CamerasManager.Instance.SwitchToLockOnCombat(target.transform, 65f, 15f);
            Player.Instance._playerAnimation.SetLockOnForward(target.transform);
            StartCoroutine(Player.Instance._playerAnimation.RotateModelFully());
            Player.Instance.playerState = Player.PlayerStates.battle;
            LockOnUIManager.Instance.SetUILockOnTarget(target.GetComponentInChildren<LockOnAnchor>().transform);
        }
    }

    public void OnLockOnPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log($"LockOn PERFORMED");
    }

    public void OnLockOnCanceled(InputAction.CallbackContext context)
    {
        // Debug.Log($"LockOn CANCELED");
        if (GameManager.Instance._currPlayMode == GameManager.PlayMode.Menu) { return; }
        if (CamerasManager.Instance.CameraState != CamerasManager.CameraStates.LockOn) { return; }

        // Debug.Log($"Exiting");
        // Debug.Log("====================");
        Player.Instance.playerState = Player.PlayerStates.normal;
        CamerasManager.Instance.SwitchToThirdPerson();
        LockOnUIManager.Instance.SetUILockOnTarget(null);

    }


    [HideInInspector] public bool lockAimInputHeld = false;
    private void OnAimStarted(InputAction.CallbackContext context)
    {
        if (!Player.Instance.OwnsBow) return;

        lockAimInputHeld = true;

        if (!p_loco.IsInState<PlayerLocomotionRegularState>()) return;

        p_loco.GoToState("aim");
        CamerasManager.Instance.SwitchToAim();
        Player.Instance.playerState = Player.PlayerStates.battle;
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log($"Aim PERFORMED");
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        lockAimInputHeld = false;
        // BowShotSMB exits the aim state and goes to regular state. We want to return to charging if the player represses the aim input before the shot ends.
    }


    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        bestInteractable = GetTargetForInteraction(GameManager.Instance.ApproachableActiveDistance);

        switch (bestInteractable)
        {
            case Talker talker:

                /// This talker portion, which is for the bow brother and sister, is hard coded ONLY because we're approaching the end. If I revisit, I'll make it cleaner. I'll probably make a database of all the talkers in relation to their convo and the player to keep track of what conversation should play based on ID
                if (talker.talkerID.Contains("bow_brother"))
                {
                    if (!Player.Instance.CanObtainBow)
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[0], talker.transform, talker.animator, talker: talker);
                    else
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[1], talker.transform, talker.animator, talker: talker);
                }
                else if (talker.talkerID.Contains("bow_sister"))
                {
                    if (!Player.Instance.CanObtainBow)
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[0], talker.transform, talker.animator, talker: talker);
                    else if (!Player.Instance.OwnsBow)
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[1], talker.transform, talker.animator, talker: talker);
                    else
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[2], talker.transform, talker.animator, talker: talker);
                }
                else if (talker.talkerID.Contains("singlesword_giver"))
                {
                    if (talker.HasTalked)
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[1], talker.transform, talker.animator, talker: talker);
                    else
                        DialogueManager.Instance.SetupConvo(talker.conversationOptions[0], talker.transform, talker.animator, talker: talker);
                }
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                else
                    DialogueManager.Instance.SetupConvo(talker.conversationOptions[0], talker.transform, talker.animator, talker: talker);
                return;

            case Chest chest:
                chest.OpenChest();
                return;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log($"Interact PERFORMED");
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        // Debug.Log("CANCELED Interact");
    }


    private void OnDodgeStarted(InputAction.CallbackContext context)
    {
        if (p_loco.inputDir.magnitude > 0.1 && !p_loco.IsInState<PlayerLocomotionDodgeState>() && p_loco.stickToGround) 
            p_loco.GoToState("dodge");
    }

    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
    }

    private void OnDodgeCanceled(InputAction.CallbackContext context)
    {
    }


    private void OnBlockStarted(InputAction.CallbackContext context)
    {
        blockInputHeld = true;
        BlockInputHandler(true);
    }

    private void OnBlockPerformed(InputAction.CallbackContext context)
    {
    }

    private void OnBlockCanceled(InputAction.CallbackContext context)
    {
        blockInputHeld = false;
        BlockInputHandler(false);
    }


    private void OnLoadStarted(InputAction.CallbackContext context)
    {
        // Debug.Log($"LOAD START");
        SaveGameManager.Instance.LoadGame();
    }

    private void OnLoadPerformed(InputAction.CallbackContext context)
    {
        // Debug.Log($"LOAD PERFORMED");
    }

    private void OnLoadCanceled(InputAction.CallbackContext context)
    {
        // Debug.Log("CANCELED LOAD");
    }


    public InventoryItemData testItem;
    public bool addItem = true;
    private void OnTestingInputStarted(InputAction.CallbackContext context)
    {
        if (addItem) Player.Instance._playerInventory.AddItem(testItem);
        else Player.Instance._playerInventory.RemoveItem(testItem);
    }

    private void OnTestingInputPerformed(InputAction.CallbackContext context)
    {
    }

    private void OnTestingInputCanceled(InputAction.CallbackContext context)
    {
    }


    private void OnPauseStarted(InputAction.CallbackContext context)
    {
        // Debug.Log($"PAUSE START");
        GameManager.Instance.PauseRoutine();        
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
    }

    private void OnPauseCanceled(InputAction.CallbackContext context)
    {
    }


// Helpers
    [HideInInspector] public IHittable dashTarget;
    [HideInInspector] public Arrow currentArrow;
    public void Attack()
    {
        Player.Instance._playerLocomotion.rb.linearVelocity = Vector3.zero;

     
        // If using a bow, disable bow object and enable our weapon object visual
        if (p_anim.bowAnimator.gameObject.activeSelf)
        {
            p_anim.bowAnimator.gameObject.SetActive(false);
            Player.Instance._playerEquipment.SetPreviewBowActive(false);
            Player.Instance._playerEquipment.RefreshAllEquipment();
        }

        // Dash if the enemy is in range and it's the first attack. Otherwise, just do a normal attack
        if(Player.Instance.DashAttackUnlocked && (Player.Instance.playerState != Player.PlayerStates.battle || (CamerasManager.Instance.CameraState == CamerasManager.CameraStates.LockOn && !Player.Instance.InAttack)))
        {
            dashTarget = GetTargetForAttackDash(p_loco.attackDashDistance);
            
            if (dashTarget is MonoBehaviour mb)
            {
                // Debug.Log($"DASH");
                p_loco.GoToState("dash");
                return;
            }
        }
        
        // Debug.Log($"NORMAL ATTACK");
        p_anim.TriggerAttack();
    }
    public void EnteringAttack() // Called by the PlayerAttackFSM
    {
        Player.Instance.InAttack = true;
        Player.Instance.CanFollowUpAttack = false;
        Player.Instance.playerState = Player.PlayerStates.battle;
        AnimationEvents.Instance.DisableAction("Move");
        AnimationEvents.Instance.PushForward(10f);
    }
    private void ScanForApproachables()
    {
        if (Time.time < nextApproachableScanTime) return;
        nextApproachableScanTime = Time.time + approachableScanInterval;

        // Grab all approachables near player and add them to the near player list
        approachablesFoundThisScan.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            GameManager.Instance.ApproachableActiveDistance,
            approachableScanResults,
            approachableScanLayerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = approachableScanResults[i];
            if (col == null) continue;

            col.TryGetComponent(out IApproachable approachable);

            if (approachable == null) continue;
            if (!IsWithinApproachableDistance(approachable)) continue;

            approachablesFoundThisScan.Add(approachable);
            approachablesNearPlayer.Add(approachable);
        }

        // Unregister and remove any approachable that is in the list that was not found
        approachablesToUnregister.Clear();
        foreach (IApproachable approachable in approachablesNearPlayer)
        {
            if (!approachablesFoundThisScan.Contains(approachable))
                approachablesToUnregister.Add(approachable);
        }


        foreach (IApproachable approachable in approachablesToUnregister)
        {
            approachablesNearPlayer.Remove(approachable);
            approachable.FadeApproachUI(0f);
        }
    }
    private void ClearApproachablesNearPlayer()
    {
        FadeApproachablesNearPlayer(0f);

        approachablesNearPlayer.Clear();
        approachablesFoundThisScan.Clear();
        approachablesToUnregister.Clear();
        bestInteractable = null;
    }
    private void FadeApproachablesNearPlayer(float targetAlpha)
    {
        foreach (IApproachable approachable in approachablesNearPlayer)
            approachable.FadeApproachUI(targetAlpha);
    }
    private IInteractable GetBestApproachableForInteraction()
    {
        IInteractable bestTarget = null;
        float bestScore = float.MaxValue;
        Vector2 viewportCenter = new Vector2(0.5f, 0.5f);

        foreach (IApproachable approachable in approachablesNearPlayer)
        {
            if (!(approachable is MonoBehaviour mb) || mb.gameObject == gameObject) continue;

            Vector3 viewportPos = Camera.main.WorldToViewportPoint(mb.transform.position);

            if (viewportPos.z <= 0 || viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1)
                continue;

            Vector2 objectViewportPos = new Vector2(viewportPos.x, viewportPos.y);
            float distanceFromCenter = Vector2.Distance(viewportCenter, objectViewportPos);
            float distFromPlayer = Vector3.Distance(transform.position, mb.transform.position);
            float score = distanceFromCenter * 10f + distFromPlayer;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = approachable;
            }
        }

        return bestTarget;
    }
    private IInteractable GetTargetForInteraction(float dist)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, dist);

        List<IInteractable> interactables = new List<IInteractable>();
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IInteractable interactable))
                interactables.Add(interactable);
        }

        IInteractable bestTarget = null;
        float bestScore = float.MaxValue;
        Vector2 viewportCenter = new Vector2(0.5f, 0.5f);

        foreach (IInteractable interactable in interactables)
        {
            // Get MonoBehaviour to access transform, scene, etc.
            if (interactable is MonoBehaviour mb && mb.gameObject != gameObject)
            {
                if (!IsWithinApproachableDistance(interactable)) continue;

                Transform inter = mb.transform;
                Vector3 viewportPos = Camera.main.WorldToViewportPoint(inter.transform.position);

                if (viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1)
                {
                    Vector2 objectViewportPos = new Vector2(viewportPos.x, viewportPos.y);

                    float distanceFromCenter = Vector2.Distance(viewportCenter, objectViewportPos);
                    float distFromPlayer = Vector3.Distance(transform.position, inter.transform.position);
                    float score = distanceFromCenter * 10f + distFromPlayer;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = interactable;
                    }
                }
            }
        }

        if (bestTarget != null)
            return bestTarget;
        else
            return null;                
    }
    private bool IsWithinApproachableDistance(IInteractable interactable)
    {
        if (interactable is not Chest chest) return true;

        return Vector3.Distance(transform.position, chest.transform.position) <= chestApproachableDistance;
    }
    private IHittable GetTargetForAttackDash(float distance)
    {
        IHittable bestTarget = null;
        float bestAngle = Mathf.Infinity;
        Vector3 origin = transform.position;
        
        Vector3 forward;
        if (CamerasManager.Instance.CameraState == CamerasManager.CameraStates.LockOn)
            forward =  p_anim.transform.forward;
        else
            forward = p_loco.inputDir.magnitude > .1f ? p_loco.GetSlopeForward(true) : p_anim.transform.forward;

        forward.y = 0f;
        forward.Normalize();


        Collider[] colliders = Physics.OverlapSphere(origin, distance);
        foreach (var col in colliders)
        {
            if (!col.TryGetComponent<IHittable>(out IHittable hittable) || col.gameObject == this.gameObject)
                continue;

            Vector3 toTarget = col.transform.position - origin;
            toTarget.y = 0f;
            toTarget.Normalize();

            float angle = Vector3.Angle(forward, toTarget);

            if (angle > 35f) continue;
            else if (angle < bestAngle)
            {
                bestAngle = angle;
                bestTarget = hittable;
            }
        }

        return bestTarget;
    }
    private void BlockInputHandler(bool started) // Make it so that we can't block in the middle of a dodge
    {
        bool releaseBlock = !started;

        // If we are in any blocking phase, just register the release intention based on if we released the block input
        if (p_loco.IsInState<PlayerLocomotionBlockedKnockbackState>() || 
            p_loco.IsInState<PlayerLocomotionBlockedRecoveryState>() || 
            (p_loco.IsInState<PlayerLocomotionBlockState>() && p_loco.hitstopActive))
        {
            p_loco.SetReleasedBlockDuringKnockback(releaseBlock);
            return;
        }

        // otherwise, just go back to state based on input
        if (started && (!p_loco.IsInState<PlayerLocomotionBlockState>()))
            p_loco.GoToState("block");
        else if (!started && p_loco.IsInState<PlayerLocomotionBlockState>())
            p_loco.GoToState("regular");

    }
    public void InstantiateArrow()
    {
        Quaternion rot = Player.Instance.R_Hand.rotation * Quaternion.Euler(-10f, 100f, 0f);// small offset to make the arrow face the right direction
        GameObject arrow = Instantiate(Player.Instance.arrowPrefab, Player.Instance.R_Hand.position, rot, Player.Instance.R_Hand);
        currentArrow = arrow.GetComponent<Arrow>();
    }
    public void ShootArrow()
    {
        if (currentArrow != null)
        {
            currentArrow.ArrowShot(Player.Instance.AimTarget.position, Player.Instance.shotSpeed, Player.Instance.arrowAttackData, Player.Instance.aimLayerMask);
        }
    }

}
