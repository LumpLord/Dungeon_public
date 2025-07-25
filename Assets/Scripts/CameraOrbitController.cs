using UnityEngine;

public class CameraOrbitController : MonoBehaviour
{
    [Header("References")]
    public Transform target;             // Usually the player
    public Transform cameraPivot;        // Pivot controls vertical angle
    public Transform cameraTransform;    // The Cinemachine Camera object

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    public float pitchMin = -20f;
    public float pitchMax = 60f;

    private float yawAngle = 0f;
    private float pitchAngle = 20f;

    // NOTE: Zoom is intentionally fixed (min == max). Mouse‑wheel input ignored.
    [Header("Zoom Settings")]
    public float zoomSpeed = 1f;   // Fixed zoom, kept for inspector completeness
    public float minZoom = 1f;
    public float maxZoom = 1f;
    private float currentZoom = 1f;

    [Header("Normal Mode Offsets")]
    public float normalOffsetY = -0.33f;

    [Header("Aim Mode Offsets")]
    public float aimZoom = -0.005f;
    public float aimOffsetX = 1f;
    public float aimOffsetY = 0f;
    public float aimRightOffset = 0.005f; // Extra right shift during aim

    [SerializeField] private CameraOrbitController orbit;

    void LateUpdate()
    {
        // Follow target position
        if (target != null)
        {
            transform.position = target.position;
        }

        // Always orbit based on mouse movement
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            yawAngle += mouseX * rotationSpeed;
            pitchAngle -= mouseY * rotationSpeed;
            pitchAngle = Mathf.Clamp(pitchAngle, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(0f, yawAngle, 0f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            }
        }

        // Zoom locked – ignore mouse scroll
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // Determine if aiming
        bool isAiming = Input.GetMouseButton(1);

        // Apply positional offsets
        float targetZoom = isAiming ? aimZoom : currentZoom;
        float targetOffsetX = isAiming ? (aimOffsetX + aimRightOffset) : 0f;
        float targetOffsetY = isAiming ? aimOffsetY : normalOffsetY;

        if (cameraTransform != null)
        {
            Vector3 targetLocalPos = new Vector3(
                targetOffsetX,
                targetOffsetY,
                -targetZoom
            );

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                targetLocalPos,
                Time.deltaTime * 10f
            );
        }
    }
}