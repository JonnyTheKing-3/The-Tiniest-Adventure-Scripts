using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHelper : MonoBehaviour
{
    [HideInInspector] public Animator animator;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // The below is used to manage logic in order to run functions AFTER an animations finishes finishes
    /* Mental model
        Think of it like placing a “claim ticket” on the animation. We'll use attack as an example:
            1) You start an attack → Call TriggerTokenAnim. Say you get ticket #41 for attack
            2) The animator (EnemyAttackFinishedSMB) enters the state and keeps a copy of ticket #41 for attack via CurrentToken
            3) When that state ends, the animator hands ticket #41 back to you via NotifyFinished
            4) Your system checks: “do I still care about ticket #41?”
                - if yes → run callback
                - if no → ignore
            5) It asks if it cares because if the attack was cancelled/interrupted/etc., we don't care about ticket #41 anymore, 
               which is why we Cancel it at the end of the EnemyCombatMeleeFSM attack state.
    */
    private int _tokenCounter = 0;
    private readonly Dictionary<string, int> _currentTokenByKey = new(); // Which request is active right now per key
    private readonly Dictionary<(string key, int token), Action> _onFinishedByKeyToken = new(); // When request (key, token) finishes, what code should run
    public int TriggerTokenedAnim(string key, string triggerName, Action onFinished = null) // Make sure trigger somehow ends with a StateMachineBehaviour that calls NotifyFinished
    {
        int token = ++_tokenCounter;

        _currentTokenByKey[key] = token;

        if (onFinished != null) _onFinishedByKeyToken[(key, token)] = onFinished;

        animator.SetTrigger(triggerName);
        return token;
    }
    public int CurrentToken(string key) => _currentTokenByKey.TryGetValue(key, out var token) ? token : 0;
    public void NotifyFinished(string key, int token)   // Make sure called by StateMachineBehaviour at end of animation
    {
        if (_onFinishedByKeyToken.TryGetValue((key, token), out var cb))
        {
            _onFinishedByKeyToken.Remove((key, token));
            cb?.Invoke();
        }
        else
        {
            // Debug.LogWarning($"NotifyFinished: No callback found for key '{key}' token {token}.");
        }
    }
    public void Cancel(string key, int token) => _onFinishedByKeyToken.Remove((key, token));
}
