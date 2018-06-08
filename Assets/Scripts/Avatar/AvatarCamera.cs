using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class AvatarCamera : MonoBehaviour {
    #region Inspector-Surfaced Properties

    [Header("References")]
	[SerializeField] private new Camera camera;
    public Camera Camera { get { return camera; } }

    [SerializeField] private Avatar avatar;
    [SerializeField] private Transform avatarFocus;

    [Header("Field of View")]
    [SerializeField] private float FOVDefault = 60f;
    [SerializeField] private float FOVSkyward = 70f;

    [Header("Limits")]
    [SerializeField] private float DistanceMin = 3f;
    [SerializeField] private float DistanceMax = 10f;
    [SerializeField] private float PitchMin = -20f;
    [SerializeField] private float PitchMax = 80f;

    [Header("Input Modifiers")]
    [SerializeField, Range(0f, 1f)] private float LookInputDeadZone = 0.01f;
    [SerializeField, Range(0f, 1f)] private float ScrollInputDeadZone = 0.01f;
    [SerializeField, Range(0.1f, 10f)] private float LateralInputSensitivity = 1f;
    [SerializeField, Range(0.1f, 10f)] private float VerticalInputSensitivity = 1f;
    [SerializeField, Range(0.1f, 10f)] private float ZoomInputSensitivity = 1f;

    [Header("Smoothing")] // Time, in seconds, that it will take to reach the desired values.
    [SerializeField] private float XSmoothTime = 0.5f; 
    [SerializeField] private float YSmoothTime = 1f; 
    [SerializeField] private float DistanceSmoothTime = 3f;
    [SerializeField] private float ForwardCorrectionTime = 2f; // How long it takes for the camera to swing around to the player's direction if there's no input.

    [Header("Occlusion Checking")]
    [SerializeField] private float OcclusionDistanceStep = 0.4f; // How often we will run the Occlusion Raycast Check
    [SerializeField] private int MaxOcclusionChecks = 10;

    [Header("Occlusion Returning")] // when the camera moves back to its previous distance after coming forward from occlusion
    [SerializeField] private float DistanceReturnSmoothTime = 1f;

    #endregion

    #region Internal Properties

    private Vector3 currentPosition = Vector3.zero;
    private Vector3 desiredPosition = Vector3.zero;
    private float xSmoothVelocity = 0f;
    private float ySmoothVelocity = 0f;
    private float zSmoothVelocity = 0f;

    private float startDistance = 0f;
    private float currentDistance = 0f;
    private float desiredDistance = 0f;
    private float distanceSmoothVelocity = 0f;
    private float forwardCorrectionSmoothVelocity = 0f;

    private float distanceSmoothTime = 0f;
    private float preOccludedDistance = 0f;

    private float yaw = 0f; // lateral
    private float pitch = 0f; // vertical

    private float lateralOffset = 0f;
    private float verticalOffset = 0f;

    private int occlusionCheckCount = 0;

    private Transform secondaryFocus; // for determining if we should offset the focus.

    #endregion

    private void Awake() {
        currentDistance = Mathf.Clamp(currentDistance, DistanceMin, DistanceMax);
        startDistance = currentDistance;
        Reset();
    }

    private void Reset() {
        // Set orbit rotation values to orient the camera a little up and behind the Avatar.
        yaw = 0f;
        pitch = 10f;

        // prevent the camera from moving on start, it has already been clamped within the field
        currentDistance = startDistance;
        desiredDistance = currentDistance;
        preOccludedDistance = currentDistance;
    }

    #region Internal Camera Logic

    private void LateUpdate() {
        if (avatarFocus == null) {
            return;
        }
    
        HandleInput();

        // Core camera position determination.
        occlusionCheckCount = 0;
        do {
            CalculateDesiredPosition();
            occlusionCheckCount++;
        } while(CheckIfOccluded(occlusionCheckCount));

        UpdateCameraPosition();
    }

    private void HandleInput() {
        if (Managers.Input.LookInput.sqrMagnitude > LookInputDeadZone * LookInputDeadZone) {
            yaw += Managers.Input.LookInput.x * LateralInputSensitivity;
            pitch += -Managers.Input.LookInput.y * VerticalInputSensitivity; // Opposite input creates the non-inverted behaviour.
        }
        else if (avatar.Movement.MovementShouldAdjustCamera()) {
            yaw = Mathf.SmoothDampAngle(yaw, avatarFocus.eulerAngles.y, ref forwardCorrectionSmoothVelocity, ForwardCorrectionTime);
        }

        // clamp the pitch angle within the nice area.
        // TODO: have the speed toward these min and max values slow down as pitch gets closer, following a curve.
        pitch = CameraHelper.ClampAngle(pitch, PitchMin, PitchMax);

        float zoom = Managers.Input.ZoomInput;
        if (zoom < -ScrollInputDeadZone || zoom > ScrollInputDeadZone) {
            desiredDistance = Mathf.Clamp(currentDistance - (zoom * ZoomInputSensitivity), DistanceMin, DistanceMax);
            preOccludedDistance = desiredDistance;
            distanceSmoothTime = DistanceSmoothTime;
        }
    }

    private void CalculateDesiredPosition() {
        ReturnOccludedDistanceToPreOcclusionValue();

        // Smooth damp (s-curve) the distance to the desired distance
        currentDistance = Mathf.SmoothDamp(currentDistance, desiredDistance, ref distanceSmoothVelocity, distanceSmoothTime);

        // Get the desired position
        desiredPosition = GetDesiredOffsetFromAvatar(pitch, yaw, currentDistance);
    }

    private Vector3 direction = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;
    private Vector3 GetDesiredOffsetFromAvatar(float rotationXAxis, float rotationYAxis, float distance) {
        direction = new Vector3(0f, 0f, -distance);
        rotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0f);
        return avatarFocus.position + (rotation * direction);
    }

    private float posX = 0f;
    private float posY = 0f;
    private float posZ = 0f;
    private void UpdateCameraPosition() {
        posX = Mathf.SmoothDamp(currentPosition.x, desiredPosition.x, ref xSmoothVelocity, XSmoothTime);
        posY = Mathf.SmoothDamp(currentPosition.y, desiredPosition.y, ref ySmoothVelocity, YSmoothTime);
        posZ = Mathf.SmoothDamp(currentPosition.z, desiredPosition.z, ref zSmoothVelocity, XSmoothTime);
        currentPosition = new Vector3(posX, posY, posZ);

        camera.transform.position = currentPosition;
        camera.transform.LookAt(avatarFocus.transform);
    }

    #endregion

    #region Camera Raycasting

    private bool CheckIfOccluded(int numChecks) {
        bool isOccluded = false;

        float nearestDistance = GetOccludedDistance(avatarFocus.position, desiredPosition);

        if (!Mathf.Approximately(nearestDistance, -1f)) {
            if (numChecks < MaxOcclusionChecks) {
                isOccluded = true;
                currentDistance -= OcclusionDistanceStep;

                // Magic number that reduces jittering at close distances.
                if (currentDistance < 0.25f) {
                    currentDistance = 0.25f;
                }
            }
            else {
                // Force-move to the non-occluded spot plus a little buffer to account for the near clip plane.
                currentDistance = nearestDistance - camera.nearClipPlane;
            }

            desiredDistance = currentDistance;
            distanceSmoothTime = DistanceReturnSmoothTime;
        }

        return isOccluded;
    }

    // Gets a distance value that will influence the camera's distance.
    // This distance will take precedence as this function is what determines if the camera can't see the player
    // and must move in front of a wall.
    CameraHelper.ClipPlanePoints nearClipPoints;
    private float GetOccludedDistance(Vector3 from, Vector3 to) {
        float nearestDistance = -1f;
        RaycastHit hitInfo;

        nearClipPoints = CameraHelper.GetCameraNearClipPlanePoints(camera, to);

        // Draw Debug Lines for visualization.
        #region Debug Drawing
        Debug.DrawLine(from, to + camera.transform.forward * -camera.nearClipPlane, Color.red);
        Debug.DrawLine(from, nearClipPoints.UpperLeft);
        Debug.DrawLine(from, nearClipPoints.LowerLeft);
        Debug.DrawLine(from, nearClipPoints.UpperRight);
        Debug.DrawLine(from, nearClipPoints.LowerRight);
        Debug.DrawLine(nearClipPoints.UpperLeft, nearClipPoints.UpperRight);
        Debug.DrawLine(nearClipPoints.UpperRight, nearClipPoints.LowerRight);
        Debug.DrawLine(nearClipPoints.LowerRight, nearClipPoints.LowerLeft);
        Debug.DrawLine(nearClipPoints.LowerLeft, nearClipPoints.UpperLeft);
        #endregion

        #region Linecasts
        // Currently player is on the IgnoreRaycast layer, so we don't need to check collider tags.
        if (Physics.Linecast(from, nearClipPoints.UpperLeft, out hitInfo)) {
            nearestDistance = hitInfo.distance;
        }

        if (Physics.Linecast(from, nearClipPoints.LowerLeft, out hitInfo)) {
            if (hitInfo.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = hitInfo.distance;
            }
        }

        if (Physics.Linecast(from, nearClipPoints.UpperRight, out hitInfo)) {
            if (hitInfo.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = hitInfo.distance;
            }
        }

        if (Physics.Linecast(from, nearClipPoints.LowerRight, out hitInfo)) {
            if (hitInfo.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = hitInfo.distance;
            }
        }

        // Check back buffer area behind the camera.
        if (Physics.Linecast(from, to + camera.transform.forward * -camera.nearClipPlane, out hitInfo)) {
            if (hitInfo.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = hitInfo.distance;
            }
        }
        #endregion

        return nearestDistance;
    }

    private void ReturnOccludedDistanceToPreOcclusionValue() {
        if (desiredDistance < preOccludedDistance) {
            Vector3 position = GetDesiredOffsetFromAvatar(pitch, yaw, preOccludedDistance);
            float nearestDistance = GetOccludedDistance(avatarFocus.position, position);

            if (Mathf.Approximately(nearestDistance, -1f) || nearestDistance > preOccludedDistance) {
                desiredDistance = preOccludedDistance;
            }
        }
    }

    #endregion
}

public static class CameraHelper {
    public struct ClipPlanePoints {
        public Vector3 UpperLeft;
        public Vector3 UpperRight;
        public Vector3 LowerLeft;
        public Vector3 LowerRight;
    }

    public static ClipPlanePoints GetCameraNearClipPlanePoints(Camera camera, Vector3 position) {
        ClipPlanePoints points = new ClipPlanePoints();

        float height = camera.nearClipPlane * Mathf.Tan((camera.fieldOfView / 2f) * Mathf.Deg2Rad);
        float width = height * camera.aspect;

        points.LowerRight = position + (camera.transform.right * width) 
                                     - (camera.transform.up * height) 
                                     + (camera.transform.forward * camera.nearClipPlane);
        
        points.LowerLeft = position - (camera.transform.right * width) 
                                    - (camera.transform.up * height) 
                                    + (camera.transform.forward * camera.nearClipPlane);

        points.UpperRight = position + (camera.transform.right * width) 
                                     + (camera.transform.up * height) 
                                     + (camera.transform.forward * camera.nearClipPlane);
        
        points.UpperLeft = position - (camera.transform.right * width) 
                                    + (camera.transform.up * height) 
                                    + (camera.transform.forward * camera.nearClipPlane);

        return points;
    }

    public static float ClampAngle(float angle, float min, float max) {
        do {
            if (angle < -360f) {
                angle += 360f;
            }
            if (angle > 360f) {
                angle -= 360f;
            }
        } while (angle < -360f || angle > 360f);

        return Mathf.Clamp(angle, min, max);
    }
}