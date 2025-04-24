using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class PlayerDodgeState : PlayerBaseState
{
    private PlayerStateMachine context;
    public PlayerDodgeState(PlayerStateMachine ctx) : base(ctx) { }

    private float currentDuration;
    Vector3 inputRight;
    Vector3 reorientedInput;
    public override void EnterState()
    {
        Vector3 effectiveGroundNormal = _ctx.Motor.GroundingStatus.GroundNormal;
        inputRight = Vector3.Cross(_ctx.MoveInputVector, _ctx.Motor.CharacterUp);
        reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _ctx.MoveInputVector.magnitude;
        _ctx.DodgeRequested = false;
        currentDuration = 0;
        _ctx.animMan.PlayTargetAnimation("dodgeBaked", true);
    }

    public override void ExitState()
    {
        currentDuration = 0;
        _ctx.isInNonBufferState=true ;
    }

    public override void UpdateState(ref Vector3 currentVelocity, float deltaTime)
    {
        float currentVelocityMagnitude = currentVelocity.magnitude;

        Vector3 effectiveGroundNormal = _ctx.Motor.GroundingStatus.GroundNormal;

        // Reorient velocity on slope
        currentVelocity = _ctx.Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

        // Calculate target velocity
        if (reorientedInput == Vector3.zero) inputRight = Vector3.Cross(-_ctx.transform.forward, _ctx.Motor.CharacterUp);
        Vector3 targetMovementVelocity = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _ctx.dodgeSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-_ctx.StableMovementSharpness * deltaTime / _ctx.dodgeDuration));
        currentDuration += Time.deltaTime;
        _ctx.HandleGravity(ref currentVelocity,deltaTime);
        if (currentDuration >= _ctx.dodgeDuration)
        {
            _ctx.SwitchState(_ctx._idleState);
        }
    }
}
