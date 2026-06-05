using UnityEngine;

public class EnemyAttackFinishedSMB : StateMachineBehaviour
{
    [Tooltip("Must match the key used in EnemyMeleeAttackState.Enter")]
    public string key = "Attack";
    public bool exitByTime = false;

    private int _tokenAtEnter;
    private bool endedeWithTime;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var enemyAnim = animator.GetComponent<EnemyAnimation>();
        _tokenAtEnter = enemyAnim.CurrentToken(key);

        endedeWithTime = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!exitByTime || stateInfo.normalizedTime < 1f || endedeWithTime) return;

        var enemyAnim = animator.GetComponent<EnemyAnimation>();
        enemyAnim.NotifyFinished(key, _tokenAtEnter);
        endedeWithTime = true; 
        // Debug.Log("Finished Anim by time");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (exitByTime) return;

        // Debug.Log("Finished Anim by exit");
        var enemyAnim = animator.GetComponent<EnemyAnimation>();
        enemyAnim.NotifyFinished(key, _tokenAtEnter);
    }
}
