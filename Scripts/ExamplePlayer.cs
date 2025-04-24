using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.DefaultInputActions;
using UnityEditor.VersionControl;

namespace KinematicCharacterController.Examples
{
    public class ExamplePlayer : MonoBehaviour
    {
        public KinematicCharacterMotor motor;
        PlayerInputs playerInput;
        public PlayerStateMachine Character;
        public ExampleCharacterCamera CharacterCamera;
        AnimatorManager AnimatorManager;
        public Animator animator;

        private float moveAmount;
        private bool sprinting = true;
        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";

        public bool isInteracting;
        public float sensitivity = 0.1f;
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }
        public void OnEnable()
        {
            if (playerInput == null)
            {
                playerInput = new PlayerInputs();
            }
            playerInput.Enable();
        }
        public void OnDisable()
        {
            playerInput.Disable();
        }
        public void Awake()
        {
            AnimatorManager=GetComponent<AnimatorManager>();
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            HandleFalling();
            isInteracting = animator.GetBool("IsInteracting");
            HandleCharacterInput();
            
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            HandleCameraInput();
            
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = playerInput.PlayerMovement.Camera.ReadValue<Vector2>().y*sensitivity;
            float mouseLookAxisRight = playerInput.PlayerMovement.Camera.ReadValue<Vector2>().x*sensitivity;
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -playerInput.PlayerMovement.CameraScroll.ReadValue<float>();
#if UNITY_WEBGL
        scrollInput = 0f;
#endif

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // Handle toggling zoom level
            if (Input.GetMouseButtonDown(1))
            {
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            }
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = playerInput.PlayerMovement.Movement.ReadValue<Vector2>().y;
            characterInputs.MoveAxisRight = playerInput.PlayerMovement.Movement.ReadValue<Vector2>().x;
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = playerInput.PlayerActions.Jump.triggered;
            characterInputs.Sprinting = playerInput.PlayerActions.Sprint.phase == InputActionPhase.Started || playerInput.PlayerActions.Sprint.phase == InputActionPhase.Performed;
            characterInputs.DodgeDown= playerInput.PlayerActions.Dodge.triggered;
            
            //handle movement blend tree
            moveAmount =Mathf.Clamp01(Mathf.Abs(characterInputs.MoveAxisForward)+Mathf.Abs(characterInputs.MoveAxisRight));

            //Handle sprinting animation
            if (!playerInput.PlayerActions.Sprint.triggered) {characterInputs.NotSprinting = true;}
            if (characterInputs.Sprinting&&moveAmount!=0) { sprinting = true; }
            else { sprinting = false; }

            //AnimatorManager.UpdateAnimatorValues(0, moveAmount,sprinting);
            
            //characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
            //characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);
            
            // Apply inputs to character
            //Character.SetInputs(ref characterInputs);
        }
        //handle falling
        private void HandleFalling()
        {
            if(!motor.GroundingStatus.IsStableOnGround)
            {
                if (!isInteracting)
                {
                    AnimatorManager.PlayTargetAnimation("fallingBaked", true);
                }
            }
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
            {
                AnimatorManager.PlayTargetAnimation("LandingBaked", false);
            }
        }
    }
}