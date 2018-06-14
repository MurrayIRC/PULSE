using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Sirenix.OdinInspector;

public class AvatarCamera : MonoBehaviour {
    #region Inspector-Surfaced Properties

	[BoxGroup("References"), SerializeField] private new Camera camera;
    public Camera Camera { get { return camera; } }

    [BoxGroup("References"), SerializeField] private Avatar avatar;
    [BoxGroup("References"), SerializeField] private Transform avatarFocus;

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
    [SerializeField] private float DistanceReturnSmoothTime = 1f;

    [Header("Raycast Adjustment Values")]
    [SerializeField] private float WhiskerMoveSharpness = 1f;
    [SerializeField] private float BackcastForwardAdjustmentStep = 0.4f;
    
    [Header("Approximate Wall-Like Angles")]
    [SerializeField] private float MinWallAngle = 80f;
    [SerializeField] private float MaxWallAngle = 100f;

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
    private float preAdjustedDistance = 0f;

    private float yaw = 0f; // lateral
    private float pitch = 0f; // vertical

    private float lateralOffset = 0f;
    private float verticalOffset = 0f;

    private Transform secondaryFocus; // for determining if we should offset the focus.

    #endregion

    private void Awake() {
        currentDistance = DistanceMin + ((DistanceMax - DistanceMin) / 2f);
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
        preAdjustedDistance = currentDistance;
    }

    #region Internal Camera Logic

    private void LateUpdate() {
        if (avatarFocus == null) {
            return;
        }
    
        HandleInput();
        CalculateDesiredPosition();
        HandleRaycasting();
        UpdateCameraPosition();
    }

    private void HandleInput() {
        if (Managers.Input.LookInput.sqrMagnitude > LookInputDeadZone * LookInputDeadZone) {
            yaw += Managers.Input.LookInput.x * LateralInputSensitivity;
            pitch += -Managers.Input.LookInput.y * VerticalInputSensitivity; // Opposite input creates the non-inverted behaviour.
        }
        else if (avatar.Controller.MovementShouldAdjustCamera()) {
            yaw = Mathf.SmoothDampAngle(yaw, avatarFocus.eulerAngles.y, ref forwardCorrectionSmoothVelocity, ForwardCorrectionTime);
        }

        // clamp the pitch angle within the nice area.
        // TODO: have the speed toward these min and max values slow down as pitch gets closer, following a curve.
        pitch = CameraHelper.ClampAngle(pitch, PitchMin, PitchMax);

        // Lerp the field of view up when the camera is low, up against the player.
        camera.fieldOfView = Mathf.Lerp(FOVSkyward, FOVDefault, Mathf.Abs(PitchMin - pitch) / Mathf.Abs(PitchMin));

        float zoom = Managers.Input.ZoomInput;
        if (zoom < -ScrollInputDeadZone || zoom > ScrollInputDeadZone) {
            desiredDistance = Mathf.Clamp(currentDistance - (zoom * ZoomInputSensitivity), DistanceMin, DistanceMax);
            preAdjustedDistance = desiredDistance;
            distanceSmoothTime = DistanceSmoothTime;
        }
    }

    private void CalculateDesiredPosition() {
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

    #region Whisker Raycasting

    private void HandleRaycasting() {
        // TODO: save data over a frame and use it in place of doing the raycasts all over again

        // WHISKER CHECKS
        CameraHelper.WhiskerPoints whiskerPoints = CameraHelper.GetWhiskerPoints(avatarFocus.position, desiredPosition);
        CameraHelper.WhiskerHitInfo whiskerLeft3 = GetWhiskerHit(whiskerPoints.Left3, avatarFocus.position);
        CameraHelper.WhiskerHitInfo whiskerLeft2 = GetWhiskerHit(whiskerPoints.Left2, avatarFocus.position);
        CameraHelper.WhiskerHitInfo whiskerLeft1 = GetWhiskerHit(whiskerPoints.Left1, avatarFocus.position);
        CameraHelper.WhiskerHitInfo whiskerRight1 = GetWhiskerHit(whiskerPoints.Right1, avatarFocus.position);
        CameraHelper.WhiskerHitInfo whiskerRight2 = GetWhiskerHit(whiskerPoints.Right2, avatarFocus.position);
        CameraHelper.WhiskerHitInfo whiskerRight3 = GetWhiskerHit(whiskerPoints.Right3, avatarFocus.position);

        float nearestDistance = -1f;
        int numLeftHit = 0;
        if (IsValidWhiskerHit(whiskerLeft3)) {
            numLeftHit++; 
            nearestDistance = whiskerLeft3.distance;
        }
        if (IsValidWhiskerHit(whiskerLeft2)) {
            numLeftHit++; 
            if (whiskerLeft2.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = whiskerLeft2.distance;
            }
        }
        if (IsValidWhiskerHit(whiskerLeft1)) {
            numLeftHit++; 
            if (whiskerLeft1.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = whiskerLeft1.distance;
            }
        }

        int numRightHit = 0;
        if (IsValidWhiskerHit(whiskerRight1)) {
            numRightHit++; 
            if (whiskerRight1.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = whiskerRight1.distance;
            }
        }
        if (IsValidWhiskerHit(whiskerRight2)) {
            numRightHit++; 
            if (whiskerRight2.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = whiskerRight2.distance;
            }
        }
        if (IsValidWhiskerHit(whiskerRight3)) {
            numRightHit++;
            if (whiskerRight3.distance < nearestDistance || Mathf.Approximately(nearestDistance, -1f)) {
                nearestDistance = whiskerRight3.distance;
            }
        }

        if (numLeftHit > 0 || numRightHit > 0) {
            if (Managers.Input.LookInput.sqrMagnitude > LookInputDeadZone * LookInputDeadZone) {
                //Debug.Log("Nearest Distance: " + nearestDistance);
                desiredDistance = Mathf.Clamp(nearestDistance, DistanceMin, DistanceMax);
            }
            else if (avatar.Controller.IsMoving()) {
                if (numLeftHit > numRightHit) {
                    yaw = Mathf.LerpAngle(yaw, yaw - numLeftHit, 1f - Mathf.Exp(-WhiskerMoveSharpness * Time.deltaTime));
                }
                else if (numRightHit > numLeftHit) {
                    yaw = Mathf.LerpAngle(yaw, yaw + numRightHit, 1f - Mathf.Exp(-WhiskerMoveSharpness * Time.deltaTime));
                }
            }

            distanceSmoothTime = DistanceReturnSmoothTime;
        }
        else {
            // BACK CHECKS
            float backDistance = GetBackcastDistance(avatarFocus.position, desiredPosition);
            if (!Mathf.Approximately(backDistance, -1f)) {
                currentDistance -= BackcastForwardAdjustmentStep;

                // Magic number that reduces jittering at close distances.
                if (currentDistance < 0.25f) {
                    currentDistance = 0.25f;
                }

                desiredDistance = currentDistance;
                distanceSmoothTime = DistanceReturnSmoothTime;
            }
        }

        if (desiredDistance < preAdjustedDistance) {
            if (Mathf.Approximately(nearestDistance, -1f) || nearestDistance > preAdjustedDistance) {
                desiredDistance = preAdjustedDistance;
            }
        }
    }

    private CameraHelper.WhiskerHitInfo GetWhiskerHit(Vector3 whiskerPoint, Vector3 origin) {
        CameraHelper.WhiskerHitInfo info = new CameraHelper.WhiskerHitInfo();
        info.distance = -1f;

        Debug.DrawLine(origin, whiskerPoint);

        RaycastHit hit;
        if (Physics.Linecast(origin, whiskerPoint, out hit)) {
            if (IsWallHit(hit.normal)) {
                info.position = hit.point;
                info.distance = hit.distance;
            }
        }

        return info;
    }

    private bool IsValidWhiskerHit(CameraHelper.WhiskerHitInfo hitInfo) {
        return !Mathf.Approximately(hitInfo.distance, -1f);
    }

    // Returns true if the angle of the given normal is close enough to being a "wall angle".
    private bool IsWallHit(Vector3 normal) {
        float angle = Vector3.Angle(normal, Vector3.up);
        return angle >= MinWallAngle && angle <= MaxWallAngle;
    }

    #endregion

    #region Back Raycasting

    private float GetBackcastDistance(Vector3 from, Vector3 to) {
        float distance = -1f;
        RaycastHit hitInfo;

        #region Debug Drawing
        Debug.DrawLine(from, to + camera.transform.forward * -camera.nearClipPlane, Color.red);
        #endregion

        #region Linecasts
        // Check back buffer area behind the camera.
        if (Physics.Linecast(from, to + camera.transform.forward * -camera.nearClipPlane, out hitInfo)) {
            distance = hitInfo.distance;
        }
        #endregion

        return distance;
    }

    #endregion

    #region Ground Raycasting

    // Casts a ray down and helps move the camera along a smooth curve towards the worms-eye view.
    // This should bypass the occlusion raycasting.
    private void AdjustToGround() {
        Vector3 startPos = camera.transform.position;
        float avatarHeight = avatarFocus.transform.position.y - avatar.transform.position.y;
        Vector3 groundRayEndPosition = startPos + (Vector3.down * avatarHeight);
    }

    #endregion
}

public static class CameraHelper {
    // Alternative method to clip plane occlusion casting is done with whisker raycasts shot from the player
    // Towards points around the camera determined by angle.
    public struct WhiskerPoints {
        public Vector3 Left3;
        public Vector3 Left2;
        public Vector3 Left1;
        public Vector3 Right1;
        public Vector3 Right2;
        public Vector3 Right3;
    }

    public struct WhiskerHitInfo {
        public Vector3 position;
        public float distance;
    }
    
    public static WhiskerPoints GetWhiskerPoints(Vector3 origin, Vector3 cameraToPos) {
        WhiskerPoints points = new WhiskerPoints();
        points.Left3 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, 48f, 0f));
        points.Left2 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, 32f, 0f));
        points.Left1 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, 16f, 0f));
        points.Right1 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, -16f, 0f));
        points.Right2 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, -32f, 0f));
        points.Right3 = RotatePointAroundPivot(cameraToPos, origin, Quaternion.Euler(0f, -48f, 0f));
        return points;
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
        return (rotation * (point - pivot)) + pivot;
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