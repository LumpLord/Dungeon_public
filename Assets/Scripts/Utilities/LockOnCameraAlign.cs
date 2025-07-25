using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways]
public class LockOnCameraAlign : MonoBehaviour
{
    [SerializeField] private CinemachineThirdPersonFollow tpf;
    [SerializeField] private float radius = 8f;   // boom length
    [SerializeField] private float height = 1.5f; // boom height
    [SerializeField] private float lerpSpeed = 5f;
    
    void LateUpdate()
    {
        Transform player  = LockOnController.Player;
        Transform target  = LockOnController.CurrentTargetStatic;
        if (!player || !target) return;

        // ---------- POSITION ----------
        Vector3 dir     = (target.position - player.position).normalized;
        Vector3 offsetL = player.InverseTransformDirection(-dir) * radius;
        Vector3 goalSO  = new Vector3(offsetL.x, height, offsetL.z);
        tpf.ShoulderOffset = Vector3.Lerp(tpf.ShoulderOffset, goalSO, Time.deltaTime * lerpSpeed);
        
        // ---------- YAW (look behind player) ----------
        Vector3 faceDir = player.position - target.position;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(faceDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * lerpSpeed);
        }
    }
}
