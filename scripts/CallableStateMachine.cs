using System;
using System.Collections.Generic;
using Godot;

namespace MatchTastic;

public sealed class CallableStateMachine
{
    private sealed class State
    {
        public State(Action normal, Action enter, Action leave)
        {
            Normal = normal;
            Enter = enter;
            Leave = leave;
        }

        public Action Normal { get; }
        public Action Enter { get; }
        public Action Leave { get; }
    }

    private readonly Dictionary<string, State> _states = new();

    public string CurrentState { get; private set; } = string.Empty;

    public void AddState(string stateName, Action normal, Action enter = null, Action leave = null)
    {
        _states[stateName] = new State(normal, enter, leave);
    }

    public void SetInitialState(string stateName)
    {
        if (_states.ContainsKey(stateName))
        {
            SetState(stateName);
        }
        else
        {
            GD.PushWarning($"No state with name {stateName}");
        }
    }

    public void Update()
    {
        if (!string.IsNullOrEmpty(CurrentState))
        {
            _states[CurrentState].Normal();
        }
    }

    public void ChangeState(string stateName)
    {
        if (_states.ContainsKey(stateName))
        {
            Callable.From(() => SetState(stateName)).CallDeferred();
        }
        else
        {
            GD.PushWarning($"No state with name {stateName}");
        }
    }

    private void SetState(string stateName)
    {
        if (!string.IsNullOrEmpty(CurrentState))
        {
            _states[CurrentState].Leave?.Invoke();
        }

        CurrentState = stateName;
        _states[CurrentState].Enter?.Invoke();
    }
}
