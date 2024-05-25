using System.Collections.Generic;
using Godot;

public partial class StateMachine : Node
{
    [Signal]
    public delegate void TransitionedEventHandler(string state_name); // Emitted when transitioning to a new state.

    [Export]
    public NodePath InitialStatePath; // Path to the initial active state. We export it to be able to pick the initial state in the inspector.

    State state; // The current active state. At the start of the game, we get the `initial_state`.

    public override void _Ready()
    {
        base._Ready();
        state = GetNode<State>(InitialStatePath);
        foreach (Node Child in GetChildren())
        {
            if (Child is State StateChild)
            {
                StateChild.StateMachine = this;
            }
        }
        state.Enter();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        state.HandleInput(@event);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        state.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        state.PhysicsUpdate(delta);
    }

    public void TransitionTo(string _targetStateName, Dictionary<string, dynamic> _msg = null)
    {
        if (!HasNode(_targetStateName))
            return;
        state.Exit();
        state = GetNode<State>(_targetStateName);
        state.Enter(_msg);
        EmitSignal("Transitioned", state.Name);
    }
}
