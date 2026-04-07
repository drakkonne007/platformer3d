using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using UnityEngine.VFX;

namespace KinematicCharacterController.Examples
{
    public enum CharacterState
    {
        Default,
        Swimm,
    }

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
        public bool CrouchDown;
        public bool CrouchUp;
        public bool AttackDown;
        public bool ChangeColorDown;
        public bool DashDown;
        public bool BlockDown;
        public bool InteractDown;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public class ExampleCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;
        public Animator Animator;
        public PlayerGameLogic PlayerLogic;
        public List<Collider> WeaponCollider;
        public Collider flyBridge;
        private HashSet<Collider> nearFlyBridges = new();
        public DashEffect DashEffect;
        public GameObject DashParticles;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 15f;
        public float MaxMovementSpeed = 20f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;
        public int isFly = 0;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;
        public ParticleSystem jumpEffect; 

        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
        public float BonusOrientationSharpness = 10f;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        public Transform CameraFollowPoint;
        public float CrouchedCapsuleHeight = 1f;
        public GameObject currentWeapon_;

        [Header("Dash")]
        public float DashSpeed = 30f;
        public float DashDuration = 0.2f;
        public float DashCooldown = 1f;
        [Tooltip("What fraction of DashSpeed is kept when arriving at a bridge (0 = dead stop, 1 = full speed)")]
        public float BridgeArrivalSpeedFactor = 0.5f;

        public CharacterState CurrentCharacterState { get; private set; }

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

        private float _dashTimer = 0f;
        private float _dashCooldownTimer = 0f;
        private bool _isDashing = false;
        private Vector3 _dashDirection = Vector3.forward;
        private Vector3 _dashTargetPosition;
        private bool _isTargetedDash;
        private Collider _currentTargetBridge;
        private Collider _lastTargetBridge;
        private float _lastBridgeCooldownTimer;

        // Combo & Attack
        private int _comboIndex = 0;
        private bool _isAttacking;
        private bool _isBlocking;
        HashSet<Collider> wasHited = new();
        private int _lastAttackAnimationHash = 0;
        private TrailRenderer swordTrail_;

        // Animator Hashes
        private readonly int _animIDSpeed = Animator.StringToHash("Speed");
        private readonly int _animIDIsGrounded = Animator.StringToHash("IsGrounded");
        private readonly int _animIDHorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private readonly int _animIDJump = Animator.StringToHash("Jump");
        private readonly int _animIDGrounded = Animator.StringToHash("Grounded");
        private readonly int _animIDFly = Animator.StringToHash("InAir");
        private readonly int _animIDAttack = Animator.StringToHash("Attack");
        private readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex");
        private readonly int _animRun = Animator.StringToHash("Run");
        private readonly int _animBlock = Animator.StringToHash("Block");
        bool _alreadyAir = false;

        private void Awake()
        {
            // Handle initial state
            if(currentWeapon_ != null)
            {
                swordTrail_ = currentWeapon_.GetComponentInChildren<TrailRenderer>();
            }
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
            if (PlayerLogic == null)
            {
                PlayerLogic = GetComponentInChildren<PlayerGameLogic>();
            }
            foreach(var coll in WeaponCollider)
            {
                coll.GetComponent<ColliderStarter>().OnEnter += attack;
                coll.GetComponent<ColliderStarter>().OnStay += attack;
            }
            flyBridge.GetComponent<ColliderStarter>().OnEnter += AddFlyBridge;
            flyBridge.GetComponent<ColliderStarter>().OnExit += RemoveFlyBridge;
        }

        void AddFlyBridge(Collider other)
        {
            nearFlyBridges.Add(other);
        }
        void RemoveFlyBridge(Collider other)
        {
            nearFlyBridges.Remove(other);
            other.transform.root.GetComponent<ActiveSwitcher>()?.Disable();
        }

        void OnDestroy()
        {
            flyBridge.GetComponent<ColliderStarter>().OnEnter -= AddFlyBridge;
            flyBridge.GetComponent<ColliderStarter>().OnExit -= RemoveFlyBridge;
            foreach (var coll in WeaponCollider)
            {
                coll.GetComponent<ColliderStarter>().OnEnter -= attack;
                coll.GetComponent<ColliderStarter>().OnStay -= attack;
            }
        }

        void attack(Collider other)
        {
            if (wasHited.Contains(other))
            {
                return;
            }
            wasHited.Add(other);
            GiantAI.GiantAI enemy = other.transform.root.GetComponent<GiantAI.GiantAI>();
            if (enemy != null)
            {
                enemy.doHurt(40, other.ClosestPointOnBounds(transform.position), inArmor: false, isPlayer: true, attacker: transform);
            }

        }
        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
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

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Move and look inputs
                        _moveInputVector = cameraPlanarRotation * moveInputVector;

                        if (inputs.AttackDown)
                        {
                            ExecuteAttack();
                        }
                        else
                        {
                            Animator.ResetTrigger(_animIDAttack);
                        }

                        _isBlocking = inputs.BlockDown;
                        if (Animator)
                        {
                            Animator.SetBool(_animBlock, _isBlocking);
                        }

                        if (_isAttacking || _isBlocking)
                        {
                            if (MaxStableMoveSpeed > 0f)
                            {
                                _moveInputVector = Vector3.ClampMagnitude(_moveInputVector, 0.5f / MaxStableMoveSpeed);
                            }
                            else
                            {
                                _moveInputVector = Vector3.zero;
                            }
                        }

                        if (inputs.ChangeColorDown && PlayerLogic != null)
                        {
                            PlayerLogic.changeMaterial();
                        }

                        if (inputs.InteractDown && MainHandler.Instance != null)
                        {
                            MainHandler.Instance.StartInteractive();
                        }

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
                        if (inputs.JumpDown && !_isBlocking)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        // Crouching input
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                                MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                            }
                        }
                        else if (inputs.CrouchUp)
                        {
                            _shouldBeCrouching = false;
                        }
                        if (inputs.DashDown)
                        {
                            _isTargetedDash = false;
                            if (nearFlyBridges.Count > 0)
                            {
                                Collider bestBridge = null;
                                float minDist = float.MaxValue;
                                bool hasInput = _moveInputVector.sqrMagnitude > 0f;

                                foreach (var bridge in nearFlyBridges)
                                {
                                    // Skip if it's the bridge we just arrived at and its cooldown is active
                                    if (bridge == _lastTargetBridge && _lastBridgeCooldownTimer > 0f) continue;

                                    Vector3 toBridge = (bridge.transform.position - Motor.TransientPosition);
                                    float dist = toBridge.magnitude;

                                    if (hasInput)
                                    {
                                        Vector3 dirToBridge = toBridge.normalized;
                                        float dot = Vector3.Dot(_moveInputVector.normalized, dirToBridge);
                                        if (dot > 0.5f)
                                        {
                                            if (dist < minDist)
                                            {
                                                minDist = dist;
                                                bestBridge = bridge;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // No input - just closest bridge overall
                                        if (dist < minDist)
                                        {
                                            minDist = dist;
                                            bestBridge = bridge;
                                        }
                                    }
                                }

                                if (bestBridge != null)
                                {
                                    _isTargetedDash = true;
                                    _dashTargetPosition = bestBridge.transform.position;
                                    _currentTargetBridge = bestBridge;
                                    _isDashing = true;
                                    if (DashEffect != null)
                                    {
                                        DashEffect.StartDashEffect();
                                    }

                                    if (DashParticles != null)
                                    {
                                        DashParticles.SetActive(true);
                                        foreach (var ps in DashParticles.GetComponentsInChildren<ParticleSystem>())
                                        {
                                            ps.Play();
                                        }
                                    }
                                }
                            }
                            if (!_isTargetedDash && _dashCooldownTimer <= 0f && !_isDashing && !_isBlocking)
                            {
                                _isDashing = true;
                                _dashCooldownTimer = DashCooldown;
                                _dashTimer = DashDuration;
                                _dashDirection = _moveInputVector.sqrMagnitude > 0f ? _moveInputVector.normalized : Motor.CharacterForward;
                                if (DashEffect != null)
                                {
                                    DashEffect.StartDashEffect();
                                }

                                if (DashParticles != null)
                                {
                                    DashParticles.SetActive(true);
                                    foreach (var ps in DashParticles.GetComponentsInChildren<ParticleSystem>())
                                    {
                                        ps.Play();
                                    }
                                }
                            }
                        }

                        break;
                    }
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
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, isFly == 0 ? -Gravity.normalized : Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
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
                                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, isFly == 0 ? -Gravity.normalized : Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
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

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Ground movement
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude;

                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                            // Reorient velocity on slope
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                            // Calculate target velocity
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                            // Smooth movement Velocity
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
                        }
                        // Air movement
                        else
                        {
                            // Add move input
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

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

                            // Gravity
                            currentVelocity += Gravity * deltaTime;

                            // Drag
                            currentVelocity *= (1f / (1f + (Drag * deltaTime)));

                            // Limit horizontal speed if not dashing (in air)
                            if (!_isDashing)
                            {
                                Vector3 horizontalVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                                if (horizontalVel.magnitude > MaxMovementSpeed)
                                {
                                    horizontalVel = horizontalVel * (MaxMovementSpeed / horizontalVel.magnitude);
                                    currentVelocity = new Vector3(horizontalVel.x, currentVelocity.y, horizontalVel.z);
                                }
                            }
                        }

                        if (_isDashing)
                        {
                            bool stopDash = false;
                            if (_isTargetedDash)
                            {
                                Vector3 dirToTarget = (_dashTargetPosition - Motor.TransientPosition);
                                float distToTarget = dirToTarget.magnitude;

                                // If close enough to arrive this frame (with 0.3m buffer)
                                if (distToTarget < (DashSpeed * deltaTime) + 0.3f)
                                {
                                    Vector3 exitVelocity = dirToTarget.normalized * DashSpeed * BridgeArrivalSpeedFactor;
                                    Motor.SetTransientPosition(_dashTargetPosition);
                                    stopDash = true;

                                    // Preserve some momentum instead of dead stop
                                    currentVelocity = exitVelocity;

                                    // Set per-bridge cooldown
                                    _lastTargetBridge = _currentTargetBridge;
                                    _lastBridgeCooldownTimer = 1.0f;
                                }
                                else
                                {
                                    currentVelocity = dirToTarget.normalized * DashSpeed;
                                }
                            }
                            else
                            {
                                _dashTimer -= deltaTime;
                                if (_dashTimer <= 0f)
                                {
                                    stopDash = true;
                                }
                                else
                                {
                                    currentVelocity = _dashDirection * DashSpeed;
                                }
                            }

                            if (stopDash)
                            {
                                _isDashing = false;

                                if (DashEffect != null)
                                {
                                    DashEffect.StopDashEffect();
                                }

                                if (DashParticles != null)
                                {
                                    foreach (var ps in DashParticles.GetComponentsInChildren<ParticleSystem>())
                                    {
                                        ps.Stop();
                                    }
                                    DashParticles.SetActive(false);
                                }

                                // After dash, limit velocity to max speed to prevent excessive momentum in air
                                if (!Motor.GroundingStatus.IsStableOnGround)
                                {
                                    currentVelocity = Vector3.ClampMagnitude(currentVelocity, MaxAirMoveSpeed);
                                }
                                else
                                {
                                    currentVelocity = Vector3.ClampMagnitude(currentVelocity, MaxStableMoveSpeed);
                                }
                            }
                        }

                        // Handle jumping
                        _jumpedThisFrame = false;
                        _timeSinceJumpRequested += deltaTime;
                        if (_jumpRequested && !_isBlocking)
                        {
                            // See if we actually are allowed to jump
                            if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
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
                                currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                                _jumpRequested = false;
                                _jumpConsumed = true;
                                _jumpedThisFrame = true;
                                _alreadyAir = true;
                                if (Animator)
                                {
                                    Animator.SetTrigger(_animIDJump);
                                    Animator.ResetTrigger(_animIDGrounded);
                                }
                                if (_internalVelocityAdd.sqrMagnitude > 0f)
                                {
                                    currentVelocity += new Vector3(_internalVelocityAdd.x * 0.2f
                                        , _internalVelocityAdd.y * 0.2f
                                        , _internalVelocityAdd.z * 0.2f);
                                    _internalVelocityAdd = Vector3.zero;
                                }
                            }
                        }

                        // Take into account additive velocity
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            if (Animator)
            {
                AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo nextStateInfo = Animator.GetNextAnimatorStateInfo(0);
                
                bool inAttackState = stateInfo.IsName("Attack01") || stateInfo.IsName("Attack02") ||
                                     stateInfo.IsName("Attack03") || stateInfo.IsName("Attack04");
                bool transitioningToAttack = nextStateInfo.IsName("Attack01") || nextStateInfo.IsName("Attack02") ||
                                             nextStateInfo.IsName("Attack03") || nextStateInfo.IsName("Attack04");

                if (!inAttackState && !transitioningToAttack)
                {
                    foreach (var coll in WeaponCollider)
                    {
                        coll.enabled = false;
                    }

                    if (swordTrail_ != null)
                    {
                        swordTrail_.emitting = false;
                    }

                    _comboIndex = 0;
                    _isAttacking = false;
                    Animator.SetInteger(_animIDAttackIndex, _comboIndex);
                    _lastAttackAnimationHash = 0; // Reset hash when not attacking
                }
                else
                {
                    if (inAttackState && stateInfo.fullPathHash != _lastAttackAnimationHash)
                    {
                        wasHited.Clear();
                        _lastAttackAnimationHash = stateInfo.fullPathHash;
                    }

                    if (inAttackState && swordTrail_ != null)
                    {
                        swordTrail_.emitting = stateInfo.normalizedTime > 0.3f && stateInfo.normalizedTime < 0.6f;
                    }
                    
                    foreach (var coll in WeaponCollider)
                    {
                        coll.enabled = true;
                    }
                }
            }

            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= deltaTime;
            }

            if (_lastBridgeCooldownTimer > 0f)
            {
                _lastBridgeCooldownTimer -= deltaTime;
            }

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Handle jump-related values
                        {
                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // If we're on a ground surface, reset jumping values
                                if (!_jumpedThisFrame)
                                {
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Do an overlap test with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }

            // Sync Animation
            if (Animator)
            {
                // Convert world velocity to local character space 
                // to get Forward (Z) and Right (X) components for the BlendTree
                Vector3 localVelocity = transform.InverseTransformDirection(Motor.Velocity);
                float speed = new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude;

                // Sync parameters:
                // Speed (X Axis in BlendTree) -> Local Right/Left
                // HorizontalSpeed (Y Axis in BlendTree) -> Local Forward/Backward
                Animator.SetFloat(_animIDSpeed, localVelocity.z);
                Animator.SetFloat(_animIDHorizontalSpeed, localVelocity.x * -1);

                Animator.SetBool(_animIDIsGrounded, Motor.GroundingStatus.IsStableOnGround);

                // Sync 'Run' bool based on movement
                Animator.SetBool(_animRun, speed > 0.1f);

                // Delayed falling animation
                if (!Motor.GroundingStatus.IsStableOnGround && _timeSinceLastAbleToJump > 0.8f)
                {
                    AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                    AnimatorStateInfo nextStateInfo = Animator.GetNextAnimatorStateInfo(0);
                    bool alreadyInAir = stateInfo.IsName("JumpStart") || stateInfo.IsName("AirFly") || stateInfo.IsName("JumpEnd") ||
                                        nextStateInfo.IsName("JumpStart") || nextStateInfo.IsName("AirFly") || nextStateInfo.IsName("JumpEnd");
                    
                    if (!alreadyInAir)
                    {
                        _alreadyAir = true;
                        Animator.SetBool(_animIDFly, true);
                    }
                }
            }
            UpdateBridgeVisuals();
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

        public void SetVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd = velocity - Motor.Velocity;
                        break;
                    }
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
            if (Animator)
            {
                Animator.SetTrigger(_animIDGrounded);
                Animator.ResetTrigger(_animIDJump);
                Animator.SetBool(_animIDFly, false);
            }
            if(jumpEffect != null && _alreadyAir)
            {
                Instantiate(jumpEffect, transform.position + new Vector3(0,0.2f,0), Quaternion.identity * Quaternion.Euler(-90,0,0));
            }
            _alreadyAir = false;
        }

        protected void OnLeaveStableGround()
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
        private void ExecuteAttack()
        {
            
            if (Animator)
            {
                int var = _comboIndex;
                AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Attack01") && stateInfo.normalizedTime > 0.6f) _comboIndex = 1;
                else if (stateInfo.IsName("Attack02") && stateInfo.normalizedTime > 0.6f) _comboIndex = 2;
                else if (stateInfo.IsName("Attack03") && stateInfo.normalizedTime > 0.6f) _comboIndex = 3;
                else _comboIndex = 0;
                Animator.SetInteger(_animIDAttackIndex, _comboIndex);
                
                if (!_isAttacking)
                {
                    Animator.SetTrigger(_animIDAttack);
                }
            }
            _isAttacking = true;
        }

        private void UpdateBridgeVisuals()
        {
            Collider potentialTarget = null;

            // Prediction logic: find which bridge would be selected if Shift was pressed
            if (nearFlyBridges.Count > 0 && !_isDashing)
            {
                float minDist = float.MaxValue;
                bool hasInput = _moveInputVector.sqrMagnitude > 0f;

                foreach (var bridge in nearFlyBridges)
                {
                    // Respect the per-bridge cooldown
                    if (bridge == _lastTargetBridge && _lastBridgeCooldownTimer > 0f) continue;

                    Vector3 toBridge = (bridge.transform.position - Motor.TransientPosition);
                    float dist = toBridge.magnitude;

                    if (hasInput)
                    {
                        Vector3 dirToBridge = toBridge.normalized;
                        float dot = Vector3.Dot(_moveInputVector.normalized, dirToBridge);
                        if (dot > 0.5f)
                        {
                            if (dist < minDist)
                            {
                                minDist = dist;
                                potentialTarget = bridge;
                            }
                        }
                    }
                    else
                    {
                        // No input - just closest bridge overall
                        if (dist < minDist)
                        {
                            minDist = dist;
                            potentialTarget = bridge;
                        }
                    }
                }
            }

            // Apply Enable/Disable to switchers on all nearby bridges
            foreach (var bridge in nearFlyBridges)
            {
                var switcher = bridge.transform.root.GetComponent<ActiveSwitcher>();
                if (switcher != null)
                {
                    if (bridge == potentialTarget)
                    {
                        switcher.Enable();
                    }
                    else
                    {
                        switcher.Disable();
                    }
                }
            }
        }
    }
}