using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    public class ExamplePlayer : MonoBehaviour
    {
        public ExampleCharacterController Character;
        public ExampleCharacterCamera CharacterCamera;

        private InputSystem_Actions _inputActions;
        PlayerGameLogic playerGameLogic;
        private void Awake()
        {
            playerGameLogic = GetComponent<PlayerGameLogic>();
            _inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        private void Update()
        {
            if (_inputActions.Player.Attack.WasPressedThisFrame())
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

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
            Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();
            // Apply sensitivity to match legacy behavior (approx 0.1)
            lookInput *= 0.1f;
            Vector3 lookInputVector = new Vector3(lookInput.x, lookInput.y, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = 0f;
            
            // Use the new Zoom action
            // Scroll values are typically 120 per click, legacy GetAxis is around 0.1 per click (depending heavily on setup).
            // Input System passes raw values. 
            // Let's try to normalize it. 
            float zoomValue = _inputActions.Player.Zoom.ReadValue<float>();
            // Scroll values are typically 120 per click.
            // Legacy GetAxis was smoothed over multiple frames.
            // Input System value is raw (1 frame). We need to increase impact.
            // 120 * 0.01 = 1.2. 
            scrollInput = -zoomValue * 0.01f;

#if UNITY_WEBGL
        scrollInput = 0f;
#endif

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            Vector2 moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
            characterInputs.MoveAxisForward = moveInput.y;
            characterInputs.MoveAxisRight = moveInput.x;
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = _inputActions.Player.Jump.WasPressedThisFrame();
            characterInputs.CrouchDown = _inputActions.Player.Crouch.WasPressedThisFrame();
            characterInputs.CrouchUp = _inputActions.Player.Crouch.WasReleasedThisFrame();
            characterInputs.AttackDown = _inputActions.Player.Attack.WasPressedThisFrame();
            characterInputs.ChangeColorDown = _inputActions.Player.ChangeColor.WasPressedThisFrame();
            characterInputs.DashDown = _inputActions.Player.Sprint.WasPressedThisFrame();
            characterInputs.BlockDown = _inputActions.Player.Block.IsPressed();

            var interactAction = _inputActions.FindAction("Interact");
            characterInputs.InteractDown = interactAction != null && interactAction.WasPressedThisFrame();

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}