using UnityEngine;

public abstract class PlayerBaseState 
{
    protected PlayerStateMachine _ctx;

    public PlayerBaseState(PlayerStateMachine ctx)
    {
        _ctx = ctx;
    }

    public abstract void EnterState();
    public abstract void UpdateState(ref Vector3 currentVelocity, float deltaTime);
    public abstract void ExitState();
    //public abstract void UpdateStateVelocity();
    //public abstract void UpdateStateRotation();
}
