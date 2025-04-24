using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerStateMachine context)
    : base(context) { }


    public override void EnterState()
    {
        //_ctx.animMan.UpdateAnimatorValues(0, 0.5f, false);
        //_ctx.animMan.UpdateAnimatorValues(0, 0.5f, false);
    }

    public override void ExitState()
    {

    }
    public override void UpdateState(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = Vector3.Lerp(currentVelocity, _ctx.HandleVelocity(ref currentVelocity, deltaTime, _ctx.MaxStableMoveSpeed), 1f - Mathf.Exp(-_ctx.StableMovementSharpness * deltaTime));

        if (!_ctx.Motor.GroundingStatus.IsStableOnGround)
        {
            _ctx.SwitchState(_ctx._fallState);
            return;
        }
        if(_ctx.SprintRequested)
        {
            _ctx.SwitchState(_ctx._sprintState);
        }
        if (_ctx.JumpRequested)
        {
            _ctx.SwitchState(_ctx._jumpState);
            return;
        }
        if (_ctx.DodgeRequested)
        {
            _ctx.SwitchState(_ctx._dodgeState);
            return;
        }
        if (_ctx.MoveInputVector.magnitude <=0.1f)
        {
            _ctx.SwitchState(_ctx._idleState);
            return;
        }
    }
}
