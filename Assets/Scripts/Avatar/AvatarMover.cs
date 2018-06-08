using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarMover : MonoBehaviour {
    [SerializeField] private new Rigidbody rigidbody;

    [Header("Input Values")]
    [SerializeField] private float MoveInputDeadZone = 0.01f;

    [Header("Movement Values")]
    [SerializeField] private float DesiredMoveSpeed = 5f;
    [SerializeField] private float MoveAccelerationTime = 1f;
    [SerializeField] private float MoveDecelerationTime = 1f;

    [Header("Turning Values")]
    [SerializeField] private float DesiredTurnSpeed = 5f;
    [SerializeField] private float TurnAccelerationTime = 1f;
    [SerializeField] private float TurnDecelerationTime = 1f;

    private Vector3 pureInput = Vector3.zero;
    private Vector3 moveInput = Vector3.zero;
    private Vector3 movementVector = Vector3.zero;
    private Vector3 lastMovementVector = Vector3.zero;

    private Quaternion desiredRotation;

    private float currentMoveSpeed = 0f;
    private float currentMoveSmoothVelocity = 0f;
    private float currentTurnSpeed = 0f;
    private float currentTurnSmoothVelocity = 0f;

    private float timeSinceMoveInputBegan = 0f;

    private void Awake() {
		
	}

    private void Update() {
        // Input handling.
        HandleInput();
        CalculateMovement();
    }

    #region Movement Update Calculations

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

    private void CalculateMovement() {
        if (pureInput.sqrMagnitude > MoveInputDeadZone * MoveInputDeadZone) {
            currentMoveSpeed = Mathf.SmoothDamp(currentMoveSpeed, DesiredMoveSpeed, ref currentMoveSmoothVelocity, MoveAccelerationTime);
            currentTurnSpeed = Mathf.SmoothDamp(currentTurnSpeed, DesiredTurnSpeed, ref currentTurnSmoothVelocity, TurnAccelerationTime);

            movementVector = moveInput * currentMoveSpeed * Time.deltaTime;
            movementVector.y = 0f;

            desiredRotation = Quaternion.LookRotation(moveInput);

            lastMovementVector = movementVector.normalized;
        }
        else if (currentMoveSpeed > MoveInputDeadZone) {
            currentMoveSpeed = Mathf.SmoothDamp(currentMoveSpeed, 0f, ref currentMoveSmoothVelocity, MoveDecelerationTime);
            currentTurnSpeed = Mathf.SmoothDamp(currentTurnSpeed, 0f, ref currentTurnSmoothVelocity, TurnDecelerationTime);

            movementVector = lastMovementVector * currentMoveSpeed * Time.deltaTime;
            movementVector.y = 0f;

            desiredRotation = Quaternion.LookRotation(lastMovementVector);
        }
    }

    #endregion
	
    #region Movement Code

    private void FixedUpdate() {
		HandleMovement();
	}

    private void HandleMovement() {
        rigidbody.MovePosition(transform.position + movementVector);
        rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, desiredRotation, currentTurnSpeed * Time.deltaTime));
    }

    #endregion

    #region Helpers

    public bool MovementShouldAdjustCamera() {
        return timeSinceMoveInputBegan > 1f && currentMoveSpeed > 0.15f;
    }

    #endregion
}
