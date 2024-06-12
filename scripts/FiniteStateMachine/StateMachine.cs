using System.Collections.Generic;
using Godot;

public partial class StateMachine : Node
{
    [Signal]
    public delegate void TransitionedEventHandler(string state_name); // Emitted when transitioning to a new state.

    [Export]
    public NodePath InitialStatePath; // Path to the initial active state. We export it to be able to pick the initial state in the inspector.

    Dictionary<string, State> states;
    State state; // The current active state. At the start of the game, we get the `initial_state`.

    public override void _Ready()
    {
        states = new Dictionary<string, State>();
        foreach (Node _node in GetChildren())
        {
            if (_node is State _stateChild)
            {
                states[_node.Name] = _stateChild;
                _stateChild.StateMachine = this;
                _stateChild.Prepared();
                _stateChild.Exit(); // reset
            }
        }
        state = GetNode<State>(InitialStatePath);
        state.Enter();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        state.HandleInput(@event);
        @event.Dispose();
    }

    public override void _Process(double delta)
    {
        state.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        state.PhysicsUpdate(delta);
    }

    public void TransitionTo(string _key, Dictionary<string, dynamic> _msg = null)
    {
        if (!states.ContainsKey(_key) || state == states[_key])
            return;
        state.Exit();
        state = states[_key];
        state.Enter(_msg);
        EmitSignal("Transitioned", state.Name);
    }
}
