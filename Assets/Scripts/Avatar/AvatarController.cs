using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public enum AvatarState {
    Default,
}

public class AvatarController : BaseCharacterController {
    [SerializeField] private new Rigidbody rigidbody;

    public AvatarState CurrentAvatarState { get; private set; }

    [Header("Input Values")]
    [SerializeField] private float MoveInputDeadZone = 0.01f;

    [Header("Ground Movement")]
    [SerializeField] private float GroundMoveSpeed = 5f;
    [SerializeField] private float MoveCurveSharpness = 5f;

    [Header("Air Movement")]
    [SerializeField] private float AirMoveSpeed = 3f;
    [SerializeField] private float AirAcceleration = 3f;
    [SerializeField] private float AirDrag = 0.1f;
    [SerializeField] private Vector3 Gravity = new Vector3(0f, -30f, 0f);

    [Header("Turning Values")]
    [SerializeField] private float TurnSpeed = 5f;

    private Vector3 pureInput = Vector3.zero;
    private Vector3 moveInput = Vector3.zero;
    private Vector3 movementDirection = Vector3.zero;
    private Vector3 lastMovementDirection = Vector3.zero;

    private Vector3 turnDirection = Vector3.zero;

    private float currentMoveSpeed = 0f;
    private float currentMoveSmoothVelocity = 0f;

    private float timeSinceMoveInputBegan = 0f;

    #region State Handling

    private void ChangeState(AvatarState newState) {
        OnStateExit(CurrentAvatarState);
        CurrentAvatarState = newState;
        OnStateEnter(CurrentAvatarState);
    }

    private void OnStateEnter(AvatarState state) {

    }

    private void OnStateExit(AvatarState state) {

    }

    #endregion

    #region Input Handling

    private void Update() {
        // Input handling.
        HandleInput();
    }

    private void HandleInput() {
        pureInput.x = Managers.Input.MoveInput.x;
        pureInput.y = 0f;
        pureInput.z = Managers.Input.MoveInput.y;

        if (pureInput.x >= MoveInputDeadZone || pureInput.x <= -MoveInputDeadZone) {
            timeSinceMoveInputBegan += Time.deltaTime;
        }
        else {
            timeSinceMoveInputBegan = 0f;
        }

        moveInput = Managers.Camera.GetCurrentCamera().transform.TransformDirection(pureInput);
        moveInput.y = 0f;
    }

    #endregion

    #region Helpers

    public bool MovementShouldAdjustCamera() {
        return timeSinceMoveInputBegan > 1f && (Motor.Velocity.x > MoveInputDeadZone || Motor.Velocity.x < -MoveInputDeadZone);
    }

    #endregion

    #region KinematicCharacterController Implementation

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        switch(CurrentAvatarState) {
            case AvatarState.Default:
                if (pureInput.sqrMagnitude > MoveInputDeadZone * MoveInputDeadZone) {
                    turnDirection = Vector3.Slerp(Motor.CharacterForward, moveInput, 1 - Mathf.Exp(-TurnSpeed * deltaTime)).normalized;
                    currentRotation = Quaternion.LookRotation(turnDirection, Motor.CharacterUp);
                }
                break;
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        switch(CurrentAvatarState) {
            case AvatarState.Default:
                Vector3 targetMoveVelocity = Vector3.zero;

                if (Motor.GroundingStatus.IsStableOnGround) {
                    Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                    if(currentVelocity.sqrMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
                    {
                        // Take the normal from where we're coming from
                        Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                        if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                        {
                            effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                        }
                        else
                        {
                            effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                        }
                    }

                    // Reorient velocity on slope
                    currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocity.magnitude;
                    Vector3 inputRight = Vector3.Cross(moveInput, Motor.CharacterUp);
                    Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * moveInput.magnitude;
                    targetMoveVelocity = reorientedInput * GroundMoveSpeed;
                    currentVelocity = Vector3.Lerp(currentVelocity, reorientedInput * GroundMoveSpeed, 1 - Mathf.Exp(-MoveCurveSharpness * deltaTime));
                }
                else { // Movement within the air.
                    // Add move input
                    if (pureInput.sqrMagnitude > 0f)
                    {
                        targetMoveVelocity = moveInput * AirMoveSpeed;

                        // Prevent climbing on un-stable slopes with air movement
                        if (Motor.GroundingStatus.FoundAnyGround)
                        {
                            Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                            targetMoveVelocity = Vector3.ProjectOnPlane(targetMoveVelocity, perpenticularObstructionNormal);
                        }

                        Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMoveVelocity - currentVelocity, Gravity);
                        currentVelocity += velocityDiff * AirAcceleration * deltaTime;
                    }

                    // Gravity
                    currentVelocity += Gravity * deltaTime;

                    // Drag
                    currentVelocity *= (1f / (1f + (AirDrag * deltaTime)));
                }
                break;
        }

        
    }

    public override void BeforeCharacterUpdate(float deltaTime) {}

    public override void PostGroundingUpdate(float deltaTime) {}

    public override void AfterCharacterUpdate(float deltaTime) {}

    public override bool IsColliderValidForCollisions(Collider coll) {
        /*if (IgnoredColliders.Contains(coll)) {
            return false;
        }*/
        return true;
    }

    public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}

    public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}

    public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}

    #endregion
}
