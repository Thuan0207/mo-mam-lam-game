using System.Collections.Generic;
using Godot;

// Virtual base class for all states.
public partial class State : Node
{
    // 	 Reference to the state machine, to call its `transition_to()` method directly.
    //  That's one unorthodox detail of our state implementation, as it adds a dependency between the
    //  state and the state machine objects, but we found it to be most efficient for our needs.

    //  The state machine node will set it.
    public StateMachine StateMachine;

    // Virtual function. Receives events from the `_ready()` callback.
    public virtual void Prepared() { }

    // Virtual function. Receives events from the `_unhandled_input()` callback.
    public virtual void HandleInput(InputEvent @event) { }

    // Virtual function. Corresponds to the `_process()` callback.
    public virtual void Update(double _delta) { }

    // Virtual function. Corresponds to the `_physics_process()` callback
    public virtual void PhysicsUpdate(double _delta) { }

    // Virtual function. Called by the state machine upon changing the active state. The `msg` parameter
    // is a dictionary with arbitrary data the state can use to initialize itself.
    public virtual void Enter(Dictionary<string, dynamic> _msg = null)
    {
        _msg ??= new Dictionary<string, dynamic>();
    }

    // Virtual function. Called by the state machine before changing the active state. Use this function
    // to clean up the state.
    public virtual void Exit() { }
}
