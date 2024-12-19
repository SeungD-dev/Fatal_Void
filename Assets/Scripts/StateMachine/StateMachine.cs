using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    StateNode current;
    Dictionary<Type, StateNode> nodes = new();
    HashSet<ITransition> anyTransitions = new();

    public IState CurrentState => current?.State;

    public void Update()
    {
        var transition = GetTransition();
        if (transition != null) ChangeState(transition.To);
        current.State?.Update();
    }

    public void FixedUpdate()
    {
        current.State?.FixedUpdate();
    }

    public void SetState(IState state)
    {
        var node = GetOrAddNode(state);
        if (current != node)
        {
            //Debug.Log($"State changed from {current?.State?.GetType().Name ?? "null"} to {state.GetType().Name}");
            current?.State?.OnExit();
            current = node;
            current.State?.OnEnter();
        }
    }

    void ChangeState(IState state)
    {
        if (state == current.State) return;
        //Debug.Log($"State changing from {current.State.GetType().Name} to {state.GetType().Name}");
        var previousState = current.State;
        var nextState = GetOrAddNode(state).State;
        previousState?.OnExit();
        current = GetOrAddNode(state);
        nextState?.OnEnter();
    }

    ITransition GetTransition()
    {
        foreach (var transition in anyTransitions)
        {
            if (transition.Condition.Evaluate())
                return transition;
        }
        foreach (var transition in current.Transitions)
        {
            if (transition.Condition.Evaluate())
                return transition;
        }
        return null;
    }

    public void AddTransition(IState from, IState to, IPredicate condition)
    {
        GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
    }

    public void AddAnyTransition(IState to, IPredicate condition)
    {
        anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    public bool IsInState<T>() where T : IState
    {
        return current.State is T;
    }

    public bool IsInState(Type stateType)
    {
        return current.State.GetType() == stateType;
    }

    StateNode GetOrAddNode(IState state)
    {
        var node = nodes.GetValueOrDefault(state.GetType());
        if (node == null)
        {
            node = new StateNode(state);
            nodes.Add(state.GetType(), node);
        }
        return node;
    }

    class StateNode
    {
        public IState State { get; }
        public HashSet<ITransition> Transitions { get; }
        public StateNode(IState state)
        {
            State = state;
            Transitions = new HashSet<ITransition>();
        }
        public void AddTransition(IState to, IPredicate condition)
        {
            Transitions.Add(new Transition(to, condition));
        }
    }
}
