using KinematicCharacterController.Examples;
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public enum OrientationMethod
{
    TowardsCamera,
    TowardsMovement,
}

public struct PlayerCharacterInputs
{
    public float MoveAxisForward;
    public float MoveAxisRight;
    public Quaternion CameraRotation;
    public bool JumpDown;
    public bool Sprinting;
    public bool NotSprinting;
    public bool DodgeDown;
    public bool LockOnDown;
    }
public enum BonusOrientationMethod
{
    None,
    TowardsGravity,
    TowardsGroundSlopeAndGravity,
}
public class PlayerStateMachine : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;
    public WeaponSwitchAndParent weaponSwitchAndParent;
    [Header("Stable Movement")]
    public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15f;
    public float OrientationSharpness = 10f;
    public float SprintSpeed = 70f;
    public float dodgeSpeed = 10000f;
    public float dodgeDuration = 0.5f;
    public bool canDodge = true;
    public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

    [Header("Air Movement")]
    public float MaxAirMoveSpeed = 15f;
    public float AirAccelerationSpeed = 15f;
    public float Drag = 0.1f;
    public float MaxAirVelocity = 30f;

    [Header("Jumping")]
    public bool AllowJumpingWhenSliding = false;
    public float JumpUpSpeed = 10f;
    public float JumpScalableForwardSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;
    public float _jumpDuration = 0.3f;

    [Header("Misc")]
    public List<Collider> IgnoredColliders = new List<Collider>();
    public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
    public float BonusOrientationSharpness = 10f;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public Transform MeshRoot;
    public Transform CameraFollowPoint;
    public float CrouchedCapsuleHeight = 1f;
    public bool isInNonBufferState = true;

    public CharacterState CurrentCharacterState { get; private set; }
    public PlayerBaseState CurrentState { get => _currentState; set => _currentState = value; }
    public bool JumpRequested { get => JumpRequested1; set => JumpRequested1 = value; }
    public bool SprintRequested { get => _sprintRequested; set => _sprintRequested = value; }
    public bool JumpConsumed { get => JumpConsumed1; set => JumpConsumed1 = value; }
    public float TimeSinceLastAbleToJump { get => _timeSinceLastAbleToJump; set => _timeSinceLastAbleToJump = value; }
    public bool JumpedThisFrame { get => _jumpedThisFrame; set => _jumpedThisFrame = value; }
    public bool JumpConsumed1 { get => _jumpConsumed; set => _jumpConsumed = value; }
    public bool JumpRequested1 { get => _jumpRequested; set => _jumpRequested = value; }
    public Vector3 MoveInputVector { get => _moveInputVector; set => _moveInputVector = value; }
    public bool DodgeRequested { get => _dodgeRequested; set => _dodgeRequested = value; }

    private Collider[] _probedColliders = new Collider[8];
    private RaycastHit[] _probedHits = new RaycastHit[8];
    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;
    private bool _jumpRequested = false;
    private bool _jumpConsumed = false;
    private bool _jumpedThisFrame = false;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private Vector3 _internalVelocityAdd = Vector3.zero;
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;
    private bool _sprintRequested = false;
    private bool _dodgeRequested = false;
    public AnimatorManager animMan;

    private Vector3 lastInnerNormal = Vector3.zero;
    private Vector3 lastOuterNormal = Vector3.zero;

    public PlayerBaseState _currentState;
    public PlayerIdleState _idleState;
    public PlayerJumpState _jumpState;
    public PlayerWalkState _walkState;
    public PlayerSprintState _sprintState;
    public PlayerDodgeState _dodgeState;
    public PlayerFallState _fallState;
    public void SwitchState(PlayerBaseState state)
    {
        CurrentState?.ExitState();
        CurrentState = state;
        state.EnterState();
    }
    private void Awake()
    {
        weaponSwitchAndParent.HoldSheathWeapon();
        _idleState = new PlayerIdleState(this);
        _walkState = new PlayerWalkState(this);
        _jumpState = new PlayerJumpState(this);
        _sprintState = new PlayerSprintState(this);
        _dodgeState = new PlayerDodgeState(this);
        _fallState = new PlayerFallState(this);

        // Assign the characterController to the motor
        Motor.CharacterController = this;

        // Setup states

        SwitchState(_idleState);
    }
    /// <summary>
    /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
    /// </summary>
    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        // Clamp input
        Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

        // Move and look inputs
        _moveInputVector = cameraPlanarRotation * moveInputVector;

        switch (OrientationMethod)
        {
            case OrientationMethod.TowardsCamera:
                _lookInputVector = cameraPlanarDirection;
            break;
            case OrientationMethod.TowardsMovement:
                _lookInputVector = _moveInputVector.normalized;
            break;
        }
        // Jumping input
        if (inputs.JumpDown&&isInNonBufferState)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
            isInNonBufferState = false;
        }
        if (inputs.Sprinting)
        {
            _sprintRequested = true;
        }
        else if (inputs.NotSprinting)
        {

            _sprintRequested = false;
        }
        if (inputs.DodgeDown&&isInNonBufferState)
        {
            _dodgeRequested = true;
            isInNonBufferState= false;
        }
    }

    /// <summary>
    /// This is called every frame by the AI script in order to tell the character what its inputs are
    /// </summary>
    public void SetInputs(ref AICharacterInputs inputs)
    {
        _moveInputVector = inputs.MoveVector;
        _lookInputVector = inputs.LookVector;
    }

    private Quaternion _tmpTransientRot;
    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                    {
                        // Smoothly interpolate from current to target look direction
                        Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                        // Set the current rotation (which will be used by the KinematicCharacterMotor)
                        currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                    }

                    Vector3 currentUp = (currentRotation * Vector3.up);
                    if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
                    {
                        // Rotate from current up to invert gravity
                        Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                    }
                    else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
                    {
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                            Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                            // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                            Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                        }
                        else
                        {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                    }
                    else
                    {
                        Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                    }
                    break;
                }
        }
    }
    
    public Vector3 HandleVelocity(ref Vector3 currentVelocity,float deltaTime,float MaxSpeed)
    {
        float currentVelocityMagnitude = currentVelocity.magnitude;

        Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

        // Reorient velocity on slope
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

        // Calculate target velocity
        Vector3 inputRight = Vector3.Cross(MoveInputVector, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * MoveInputVector.magnitude;
        Vector3 targetMovementVelocity = reorientedInput * MaxSpeed;
        return targetMovementVelocity;
    }
    public Vector3 HandleAirVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (MoveInputVector.sqrMagnitude > 0f)
        {

            Vector3 addedVelocity = MoveInputVector * AirAccelerationSpeed * deltaTime;

            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

            // Limit air velocity from inputs
            if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
            {
                // clamp addedVel to make total vel not exceed max vel on inputs plane
                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                addedVelocity = newTotal - currentVelocityOnInputsPlane;
            }
            else
            {
                // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                {
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                }
            }

            // Prevent air-climbing sloped walls
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                }
            }

            // Apply added velocity

            currentVelocity += addedVelocity;
        }
        return currentVelocity;
    }
    public void HandleGravity(ref Vector3 currentVelocity,float deltaTime)
    {
        currentVelocity += Gravity * deltaTime;

        // Drag
        currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        currentVelocity.x = Mathf.Clamp(currentVelocity.x, -MaxAirVelocity, MaxAirVelocity);
        currentVelocity.y = Mathf.Clamp(currentVelocity.y, -MaxAirVelocity, MaxAirVelocity);
        currentVelocity.z = Mathf.Clamp(currentVelocity.z, -MaxAirVelocity, MaxAirVelocity);
    }
    public Vector3 HandleJumpVelocity(ref Vector3 currentVelocity,float deltaTime)
    {
        JumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;

        // See if we actually are allowed to jump
        if (!JumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || TimeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
        {
            // Calculate jump direction before ungrounding
            Vector3 jumpDirection = Motor.CharacterUp;
            if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
            {
                jumpDirection = Motor.GroundingStatus.GroundNormal;
            }
            // Makes the character skip ground probing/snapping on its next update. 
            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
            Motor.ForceUnground();

            // Add to the return velocity and reset jump state
            currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
            currentVelocity += (MoveInputVector * JumpScalableForwardSpeed);
            JumpRequested = false;
            JumpConsumed = true;
            JumpedThisFrame = true;
        }
        return currentVelocity;
    }
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        CurrentState.UpdateState(ref currentVelocity,deltaTime);
        // Take into account additive velocity
        //if (_internalVelocityAdd.sqrMagnitude > 0f)
        //{
        //    currentVelocity += _internalVelocityAdd;
        //    _internalVelocityAdd = Vector3.zero;
        //}
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    // Handle jump-related values
                    {
                        // Handle jumping pre-ground grace period
                        if (JumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                        {
                            JumpRequested = false;
                        }

                        if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                        {
                            // If we're on a ground surface, reset jumping values
                            if (!JumpedThisFrame)
                            {
                                JumpConsumed = false;
                            }
                            TimeSinceLastAbleToJump = 0f;
                        }
                        else
                        {
                            // Keep track of time since we were last able to jump (for grace period)
                            TimeSinceLastAbleToJump += deltaTime;
                        }
                    }

                    // Handle uncrouching
                    //if (_isCrouching && !_shouldBeCrouching)
                    //{
                    //    // Do an overlap test with the character's standing height to see if there are any obstructions
                    //    Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                    //    if (Motor.CharacterOverlap(
                    //        Motor.TransientPosition,
                    //        Motor.TransientRotation,
                    //        _probedColliders,
                    //        Motor.CollidableLayers,
                    //        QueryTriggerInteraction.Ignore) > 0)
                    //    {
                    //        // If obstructions, just stick to crouching dimensions
                    //        Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                    //    }
                    //    else
                    //    {
                    //        // If no obstructions, uncrouch
                    //        MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                    //        _isCrouching = false;
                    //    }
                    //}
                    break;
                }
        }
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Handle landing and leaving ground
        if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLanded();
        }
        else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
        {
            OnLeaveStableGround();
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (IgnoredColliders.Count == 0)
        {
            return true;
        }

        if (IgnoredColliders.Contains(coll))
        {
            return false;
        }

        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void AddVelocity(Vector3 velocity)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
                {
                    _internalVelocityAdd += velocity;
                    break;
                }
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    protected void OnLanded()
    {
    }

    protected void OnLeaveStableGround()
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}
