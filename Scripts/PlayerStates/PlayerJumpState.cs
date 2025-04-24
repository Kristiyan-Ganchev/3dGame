using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    public PlayerJumpState(PlayerStateMachine context)
    : base(context) { }

    private float currentDuration ;
    private float jumpDuration;
    public override void EnterState()
    {
        jumpDuration = _ctx._jumpDuration;
        currentDuration = 0f;
        if (!_ctx.SprintRequested) {
            _ctx.animMan.PlayTargetAnimation("JumpBaked", true);
        }
        else
        {
            _ctx.animMan.PlayTargetAnimation("sprintJumpBaked", true);
            jumpDuration *= 2.5f;
        }
    }

    public override void ExitState()
    {
        currentDuration = 0f;
    }

    public override void UpdateState(ref Vector3 currentVelocity, float deltaTime)
    {
        _ctx.HandleJumpVelocity(ref currentVelocity, deltaTime);
        _ctx.HandleAirVelocity(ref currentVelocity, deltaTime);
        currentDuration+= Time.deltaTime;
        if(jumpDuration <= currentDuration)
        {
            _ctx.SwitchState(_ctx._fallState);
        }
    }
}
