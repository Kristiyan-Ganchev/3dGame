using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class PlayerFallState : PlayerBaseState
{
    private PlayerStateMachine context;
    public PlayerFallState(PlayerStateMachine ctx) : base(ctx) { }

    public override void EnterState()
    {
        _ctx.animMan.PlayTargetAnimation("fallingBaked", true);
    }

    public override void ExitState()
    {
        _ctx.animMan.PlayTargetAnimation("LandingBaked", false);
        _ctx.isInNonBufferState = true;
    }

    public override void UpdateState(ref Vector3 currentVelocity, float deltaTime)
    {
        _ctx.HandleAirVelocity(ref currentVelocity, deltaTime);
        _ctx.HandleGravity(ref currentVelocity, deltaTime);

        if(_ctx.Motor.GroundingStatus.IsStableOnGround)
        {
            _ctx.SwitchState(_ctx._idleState);
        }
    }
}
