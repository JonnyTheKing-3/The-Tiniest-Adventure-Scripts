using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void Enter(); 
    void Update(); 
    void Exit();
}

public abstract class State<T> : IState
{
    protected readonly T m_self;

    // This is basically saying that the State is a state of T self, and then assigning it
    protected State(T self) => m_self = self;

    public abstract void Enter(); 
    public abstract void Update(); 
    public abstract void Exit();
}

public abstract class FiniteStateMachine : MonoBehaviour
{
    // I know using strings can lead to problems like typos, but I genuinely like how readable they are. I might change this later to use enums or something else
    protected readonly Dictionary<string, IState> _states = new Dictionary<string, IState>();
    private IState _currentState;
    private IState _nextState;

    public IState CurrentState => _currentState;    // Used for debugging
    public IState NextState => _nextState;          // Used for debugging
    public bool IsInState<TState>() where TState : class, IState => _currentState is TState;


    protected void AddState(string stateId, IState state) => _states[stateId] = state;

    // Public because then the State<T> classes need to access it
    public void GoToState(string stateId)
    {
        if (!_states.TryGetValue(stateId, out var state))
        {
            Debug.LogError($"[FSM] State '{stateId}' not found on {name}");
            return;
        }

        _nextState = state;
    }

    protected TState GetState<TState>(string stateId) where TState : class, IState
    {
        if (!_states.TryGetValue(stateId, out var state))
        {
            Debug.LogError($"[FSM] State '{stateId}' not found on {name}");
            return null;
        }

        var typed = state as TState;
        if (typed == null)
        {
            Debug.LogError($"[FSM] State '{stateId}' is not of type {typeof(TState).Name} on {name}");
        }

        return typed;
    }

    // Virtual because the children might want to extend this update. Like having a universal thing happening for every state + the current states update
    protected virtual void Update()
    {
        if (_nextState != null)
        {
            _currentState?.Exit();
            _currentState = _nextState;
            _nextState = null;
            _currentState.Enter();
        }

        _currentState?.Update();
    }

    protected virtual void FixedUpdate()
    {
        if (_currentState is IFixedUpdate fixedUpdateState)
            fixedUpdateState.FixedUpdate();
    }
}
