using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IHasHealth
{
    public enum EnemyStates {Idle = 0, Patrol = 1, Combat = 2}
    public EnemyStates enemyState = EnemyStates.Idle;

    [Header("SAVE DATA")]
    [SerializeField] private string enemyID;
    public string EnemyID => enemyID;
    public bool HasSaveID => !string.IsNullOrWhiteSpace(enemyID);

    [HideInInspector] public EnemyBrainFSM enemyBrain;
    [HideInInspector] public EnemyLocomotionFSM enemyLoco;
    [HideInInspector] public EnemyAnimation enemyAnim;
    [HideInInspector] public EnemyCombatBase enemyCombat;

    public Transform R_hand;
    public Transform L_hand;
    public Transform deathVFXposition;
    public Health health;
    public CombatStats combatStats;

    public Health Health => health;
    public bool healthDepleted; // Gets set by healths OnDeath. Locomotion knockback uses it to know if it should die after knockback
    [Tooltip("The death routine is called once the last knockback is over. Once the death routine is called, this is how much we wait before we do anything. Kind of like a brief freeze before death")]
    public float DeathTimer;
    [HideInInspector] public bool startedDeathRoutine;


    void Start()
    {
        enemyLoco = GetComponent<EnemyLocomotionFSM>();
        enemyAnim = GetComponentInChildren<EnemyAnimation>();
        enemyBrain = GetComponent<EnemyBrainFSM>();
        enemyCombat = GetComponent<EnemyCombatBase>();
        healthDepleted = false;
        startedDeathRoutine = false;

        Health.OnDeath += () => healthDepleted = true;
    }

    public void DeathRoutine()
    {
        GameManager.Instance.UnregisterEnemyInCombat(this); // Just in case

        startedDeathRoutine = true;
        enemyLoco.GoToState("idle");
        enemyBrain.GoToState("idle");
        enemyCombat.GoToState("idle");
        
        StartCoroutine(PlayDeath());
    }

    void Update()   // Need to keep the enemy grounded during death timer
    {
        if (enemyLoco.grounded && startedDeathRoutine) {enemyLoco.controller.Move(Vector3.down * (enemyLoco.transform.position.y +.1f - enemyLoco.Surface.point.y) * Time.deltaTime);}
    }

    IEnumerator PlayDeath()
    {
        yield return StartCoroutine(enemyAnim.darknessDeathEffect.SpreadDarkness(DeathTimer));

        AudioManager.Instance.PlaySmokePoof(transform);
        GameManager.Instance.PlayDeathVFX(deathVFXposition.position);
        enemyAnim.gameObject.SetActive(false);
        CanvasGroup healthBarCanvas = GetComponentInChildren<CanvasGroup>(true); // The only canvas group in the enemy is the health bar canvas group. If I add more UI objects, I'll get a variable reference instead
        if (healthBarCanvas != null) healthBarCanvas.alpha = 0f;

        gameObject.SetActive(false);
    }


    // Used for LoadGame()
    public void ResetFromSave(Vector3 savedPosition, Quaternion savedRotation)
    {
        StopAllCoroutines();
        GameManager.Instance.UnregisterEnemyInCombat(this);

        gameObject.SetActive(true);
        transform.SetPositionAndRotation(savedPosition, savedRotation);

        healthDepleted = false;
        startedDeathRoutine = false;
        enemyState = EnemyStates.Idle;
        Health.RestoreToFull();

        enemyAnim.gameObject.SetActive(true);
        enemyAnim.darknessDeathEffect.RestoreOriginalMaterials();
        enemyAnim.animator.speed = 1f;
        enemyAnim.animator.CrossFade("Moving", .1f, 0);

        CanvasGroup healthBarCanvas = GetComponentInChildren<CanvasGroup>(true);
        healthBarCanvas.alpha = 0f;

        enemyLoco.hitstopActive = false;
        enemyLoco.ResetVelocityFactors();
        enemyLoco.GoToState("regular");

        enemyBrain.GoToState("decide");
        enemyCombat.GoToState("idle");
    }

}
