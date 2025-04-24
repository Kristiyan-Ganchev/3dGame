using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprintState : PlayerBaseState
{
    public PlayerSprintState(PlayerStateMachine context)
    : base(context) { }


    public override void EnterState()
    {
        _ctx.animMan.UpdateAnimatorValues(0, 1, true);
        _ctx.weaponSwitchAndParent.StowSheathWeapon();
    }

    public override void ExitState()
    {
        _ctx.weaponSwitchAndParent.HoldSheathWeapon();
    }

    public override void UpdateState(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = Vector3.Lerp(currentVelocity, _ctx.HandleVelocity(ref currentVelocity, deltaTime, _ctx.SprintSpeed), 1f - Mathf.Exp(-_ctx.StableMovementSharpness * deltaTime));
        if (!_ctx.Motor.GroundingStatus.IsStableOnGround)
        {
            _ctx.SwitchState(_ctx._fallState);
            return;
        }
        if (!_ctx.SprintRequested)
        {
            _ctx.SwitchState(_ctx._walkState);
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
        if (_ctx.MoveInputVector.magnitude <= 0.1f)
        {
            _ctx.SwitchState(_ctx._idleState);
            return;
        }
    }
}
