using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour, IHasHealth
{
    public static Player Instance { get; private set; }
    [HideInInspector] public PlayerInputScript _playerInputScript;
    [HideInInspector] public PlayerUIInputManager _playerUIInputManager;
    [HideInInspector] public PlayerLocomotionFSM _playerLocomotion;
    [HideInInspector] public PlayerAnimation _playerAnimation;
    [HideInInspector] public PlayerCombat _playerCombat;
    [HideInInspector] public PlayerInventory _playerInventory;
    [HideInInspector] public PlayerEquipment _playerEquipment;
    public HealthBar _playerHealtBar;

    public enum PlayerStates { normal = 0, battle = 1, dialogue = 2, swimming = 3}
    public PlayerStates playerState;

    [Header("Combat")]
    public Transform R_Hand;
    public Transform L_Hand;
    public Transform[] weaponHoldPoints => new Transform[] { R_Hand, L_Hand };
    [Range(0f, 1f)] public float perfectDodgeExtraWindow = 0.2f; // I'll probably replace this with having each attack specify its own perfect dodge window, but for now this is a simple way to allow perfect dodges for a short window before the hitbox is active
    public bool PerfectDodgeActive = false;
    public bool CounterAttacking = false;

    [Header("Aim")]
    public LayerMask aimLayerMask;
    public Transform AimTarget;
    public GameObject arrowPrefab;
    public AttackData arrowAttackData;
    public float aimRadius = 0.5f;
    public float shotDistance = 20;
    public float shotSpeed = 7;

    [Header("Status")]
    public bool InAttack = false;
    public bool CanFollowUpAttack = false;
    public bool DashAttackUnlocked = false;
    public bool PerfectDodgeUnlocked = false;
    public bool CanObtainBow;
    public bool OwnsBow;
    public bool healthHasDepleted = false;
    public bool startedDeathRoutine = false;
    public int SkillTreePoints = 0;
    public Health health;
    public Health Health => health;
    public CombatStats baseCombatStats; // Get's updated in the SkillTreeMenuManager
    public CombatStats weaponCombatStats; // Get's updated by PlayerEquipment
    public CombatStats combatStats => baseCombatStats + weaponCombatStats;
    
    [Header("Settings")]
    public float AttackBuffer;
    public float deathWait;

    void Awake()
    {
        Instance = this;
        CanFollowUpAttack = false;
        PerfectDodgeActive = false;
        CounterAttacking = false;
        healthHasDepleted = false;
        startedDeathRoutine = false;
        playerState = PlayerStates.normal;

        _playerInputScript = GetComponent<PlayerInputScript>();
        _playerUIInputManager = GetComponent<PlayerUIInputManager>();
        _playerLocomotion = GetComponent<PlayerLocomotionFSM>();
        _playerAnimation = GetComponentInChildren<PlayerAnimation>();
        _playerCombat = GetComponentInChildren<PlayerCombat>();
        _playerInventory = GetComponent<PlayerInventory>();
        _playerEquipment = GetComponent<PlayerEquipment>();

        Health.OnDeath += () => healthHasDepleted = true;
    }

    void FixedUpdate()   // Make sure the player is stuck on the ground on death
    {
        if (startedDeathRoutine)
        {
            _playerLocomotion.UpdateGroundingStatus();
            _playerLocomotion.MovePlayer();
        }
    }

    public void DeathRoutine()
    {
        startedDeathRoutine = true;
        _playerLocomotion.GoToState("idle");
        _playerLocomotion.ResetVelocityFactors();
        AnimationEvents.Instance.DisablePlayerActionMap();

        StartCoroutine(PlayDeath());
    }

    IEnumerator PlayDeath()
    {
        yield return new WaitForSeconds(deathWait);

        GameManager.Instance.GameOver(true);
    }
}
