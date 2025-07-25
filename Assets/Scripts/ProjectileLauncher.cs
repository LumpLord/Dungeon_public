using UnityEngine;

/// <summary>
/// Fires a projectile so that it lands where the centre-screen reticle points,
/// while still spawning from the weapon muzzle. Works with free-look cameras.
/// </summary>
public class ProjectileLauncher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;          // ProjectileMuzzle
    [SerializeField] private Camera cam;                // Usually Camera.main
    [SerializeField] private LayerMask blockMask;       // ProjectileBlock
    [SerializeField] private GameObject projectilePrefab;

    [Header("Tuning")]
    [Tooltip("Max degrees a projectile can ‘bend’ toward the reticle")]
    [Range(0f,15f)] public float maxCorrectionAngle = 9f;
    public float maxRange = 60f;
    [Tooltip("Initial speed of the projectile in metres per second")]
    public float projectileSpeed = 38f;
    [Tooltip("Seconds between shots")]
    public float fireCooldown = 0.25f;
    private float nextFireTime = 0f;

    
    void Awake()
    {
        if (cam == null)
            cam = Camera.main;   // falls back to whatever is tagged MainCamera
    }
    public void Fire()
    {
        // Cool‑down guard
        if (Time.time < nextFireTime)
            return;
        nextFireTime = Time.time + fireCooldown;

        // 1. Camera-centre ray
        Ray centerRay = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        Vector3 targetPoint = Physics.Raycast(centerRay, out RaycastHit hit, maxRange, blockMask)
            ? hit.point
            : centerRay.origin + centerRay.direction * maxRange;

        // 2. Direction from muzzle to target
        Vector3 dir = (targetPoint - muzzle.position).normalized;

        // 3. Debug rays  (these lines MUST come after 'dir' is declared)
        Debug.DrawRay(muzzle.position, muzzle.forward * 4f, Color.green, 2f); // wand forward
        Debug.DrawRay(muzzle.position, dir            * 4f, Color.red,   2f); // corrected dir

        // 4. Clamp angle
        float angle = Vector3.Angle(muzzle.forward, dir);
        if (angle > maxCorrectionAngle)
        {
            Vector3 axis = Vector3.Cross(muzzle.forward, dir);
            dir = Quaternion.AngleAxis(maxCorrectionAngle, axis) * muzzle.forward;
        }

        // 5. Spawn projectile and set initial velocity
        GameObject p = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));

        // Tag the projectile with its shooter so damage events know the attacker
        Projectile proj = p.GetComponent<Projectile>();
        if (proj != null)
            proj.shooter = gameObject;          // 'gameObject' is the player holding this launcher

        // Apply velocity
        if (p.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = dir * projectileSpeed;
    }
}