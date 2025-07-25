using UnityEngine;

public class LockOnOrbitDisabler : MonoBehaviour
{
    [SerializeField] private CameraOrbitController orbit;

    void OnEnable() => LockOnController.OnLockChanged += Handle;
    void OnDisable() => LockOnController.OnLockChanged -= Handle;

    void Handle(Transform tgt)
    {
        orbit.enabled = (tgt == null);   // disable while locked
    }
}