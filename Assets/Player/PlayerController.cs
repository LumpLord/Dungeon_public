using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Profile")]
    public PlayerMovementStats movementStats;

    [Header("Input Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode dodgeKey = KeyCode.RightAlt;


    [Header("Aiming")]
    public bool isAiming = false;

    [Header("Smoothing")]
    public float directionSmoothTime = 0.1f;

    private Rigidbody rb;
    private Vector3 inputDirection;
    private Vector3 smoothInputDirection;
    private Vector3 velocityRef;
    private float lastDashTime = -999f;

    // Dodge state
    private bool isDodging = false;
    private Vector3 dodgeStartPos;
    private Vector3 dodgeTargetPos;

    // Fall detection
    private float fallTimer = 0f;
    private float maxFallTime = 15f;
    private bool isGrounded = true;
    private Vector3 spawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        spawnPoint = transform.position;
    }

    void Update()
    {
        // --- Camera‑relative movement axes ---
        float hRaw = Input.GetAxisRaw("Horizontal");
        float vRaw = Input.GetAxisRaw("Vertical");

        Transform cam = CameraUtil.ActiveCamTransform;
        Vector3 camForward = cam.forward;  camForward.y = 0f;
        Vector3 camRight   = cam.right;    camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredDir = (camForward * vRaw + camRight * hRaw).normalized;

        // Smooth input direction
        smoothInputDirection = Vector3.SmoothDamp(
            smoothInputDirection,
            desiredDir,
            ref velocityRef,
            directionSmoothTime);

        inputDirection = smoothInputDirection;

        isAiming = Input.GetMouseButton(1);

        // -----------------------------------------------------------------
        // Hard‑lock: while aiming and a lock target exists, face the target
        bool hardLocked = LockOnController.CurrentTargetStatic !=null;
        if (!isDodging && isAiming && hardLocked)
        {
            Vector3 flatDir = LockOnController.CurrentTargetStatic.position - transform.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.1f)
            {
                Quaternion aimRot = Quaternion.LookRotation(flatDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, Time.deltaTime * 15f);
            }
        }
        else if (!isDodging && hardLocked)
        {
            Vector3 toTarget = LockOnController.CurrentTargetStatic.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(toTarget), Time.deltaTime * 10f);
        }
        // Normal free‑aim rotation
        else if (!isDodging && isAiming)
        {
            Transform activeCam = CameraUtil.ActiveCamTransform;
            Vector3 aimDir = activeCam.forward; aimDir.y = 0f;

            if (aimDir.sqrMagnitude > 0.1f)
            {
                Quaternion aimRot = Quaternion.LookRotation(aimDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, Time.deltaTime * 10f);
            }
        }
        // Face move direction when not aiming
        else if (!isDodging && inputDirection.sqrMagnitude > 0.1f)
        {
            Quaternion moveRot = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRot, Time.deltaTime * 10f);
        }
        // -----------------------------------------------------------------

        // Dodge (camera-relative; back-step fallback)
        if (!isDodging && Input.GetKeyDown(dodgeKey) &&
            Time.time > lastDashTime + movementStats.dashCooldown)
        {
            Vector3 dodgeDir;

            if (inputDirection.sqrMagnitude > 0.01f)
            {
                dodgeDir = (camForward * vRaw + camRight * hRaw).normalized;
            }
            else
            {
                // No input → back‑step
                dodgeDir = -transform.forward;
            }

            StartCoroutine(DodgeCoroutine(dodgeDir));
            lastDashTime = Time.time;
        }

        // Stay upright
        if (transform.up != Vector3.up)
        {
            transform.up = Vector3.up;
        }
    }

    void FixedUpdate()
    {
        if (isDodging) return;

        float speed = movementStats.moveSpeed;

        if (isAiming)
            speed *= movementStats.aimSpeedMultiplier;

        if (Input.GetKey(sprintKey) && inputDirection.sqrMagnitude > 0.01f)
            speed *= movementStats.sprintMultiplier;

        Vector3 move = inputDirection * speed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
    }

    IEnumerator DodgeCoroutine(Vector3 direction)
    {
        isDodging = true;

        Quaternion lockedRotation = transform.rotation;
        float elapsed = 0f;

        Vector3 startPos  = transform.position;
        Vector3 targetPos = startPos + (direction * movementStats.dashDistance);

        while (elapsed < movementStats.dashTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / movementStats.dashTime;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = lockedRotation;   // keep original facing

            yield return null;
        }

        transform.position = targetPos;
        isDodging = false;
    }

    private void CheckGrounded()
    {
        float rayDistance = 1.1f;
        Ray ray = new Ray(transform.position, Vector3.down);
        isGrounded = Physics.Raycast(ray, rayDistance);
    }

    private void CheckFallTimer()
    {
        if (!isGrounded)
        {
            fallTimer += Time.deltaTime;
            if (fallTimer >= maxFallTime)
            {
                Respawn();
                fallTimer = 0f;
            }
        }
        else
        {
            fallTimer = 0f;
        }
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = spawnPoint;
        Debug.Log("Player respawned due to fall.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FallZone"))
        {
            Respawn();
        }
    }
}