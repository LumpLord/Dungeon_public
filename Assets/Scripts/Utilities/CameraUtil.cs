using UnityEngine;
using Unity.Cinemachine;

public static class CameraUtil
{
    public static Transform ActiveCamTransform
    {
        get
        {
            var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain && brain.ActiveVirtualCamera is {} vCam)
            {
                // If the active vCam is a Component (typical case), return its transform
                if (vCam is Component c)
                    return c.transform;
            }

            // Fallback: return the main camera's transform
            return Camera.main.transform;
        }
    }
}