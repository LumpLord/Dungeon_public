using UnityEngine;
using Unity.Cinemachine;

public class LockOnCameraRig : MonoBehaviour
{
    
    [SerializeField] private CinemachineCamera       lockVCam;
    [SerializeField] private CinemachineCamera       shoulderVCam;   // drag CM_ThirdPersonCam here
    [SerializeField] private CinemachineTargetGroup  targetGroup;
    [SerializeField] private int  lockedPriority = 20;
    [SerializeField] private int  freePriority   = 5;
    [SerializeField] private float targetWeight  = 1f;
    [SerializeField] private float targetRadius  = 0.3f;
    [SerializeField] private CameraOrbitController orbit;   // drag the CameraOrbitController here

    Transform current;

    void OnEnable()  => LockOnController.OnLockChanged += HandleLock;
    void OnDisable() => LockOnController.OnLockChanged -= HandleLock;

    void HandleLock(Transform t)
    {
        // Ignore dead corpses
        if (t != null)
        {
            var hc = t.GetComponent<HealthComponent>();
            if (hc != null && !hc.IsAlive)
                t = null;
        }

        // Enable orbit when unlocked; disable while locked-on
        if (orbit != null)
            orbit.enabled = (t == null);

        // Remove previous member, if any
        if (current) targetGroup.RemoveMember(current);

        current = t;

        if (current != null)
        {
            // Add new enemy to the group
            targetGroup.AddMember(current, targetWeight, targetRadius);

            // --- Live‑sync shoulder vCam pose every frame while locked ---
            // Removed redundant per-frame copy here
            /*
            if (shoulderVCam != null)
            {
                shoulderVCam.transform.position = lockVCam.transform.position;
                shoulderVCam.transform.rotation = lockVCam.transform.rotation;
            }
            */
        }

        // Follow & LookAt the group itself (player is already element 0)
        // lockVCam.Follow   = targetGroup.transform;
        // lockVCam.LookAt   = targetGroup.transform;
        // Stay behind the player (Follow = player) but aim at group (LookAt)
        lockVCam.Follow   = LockOnController.Player;
        lockVCam.LookAt   = targetGroup.transform;
        lockVCam.Priority = current ? lockedPriority : freePriority;

        // If we're unlocking (t is null) copy the lock camera pose to the shoulder camera
        if (t == null && shoulderVCam != null)
        {
            // Force the shoulder vCam to start exactly where the lock vCam ended
            Vector3 pos = lockVCam.transform.position;
            Quaternion rot = lockVCam.transform.rotation;
            // zero out roll so free cam starts level
            Vector3 e = rot.eulerAngles;
            rot = Quaternion.Euler(e.x, e.y, 0f);

            shoulderVCam.ForceCameraPosition(pos, rot);
        }
    }

    /*
    void LateUpdate()
    {
        if (current != null && shoulderVCam != null)
        {
            shoulderVCam.transform.position = lockVCam.transform.position;
            shoulderVCam.transform.rotation = lockVCam.transform.rotation;
        }
    }
    */
}