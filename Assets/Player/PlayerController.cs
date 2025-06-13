using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Profile")]
    public PlayerMovementStats movementStats;

    [Header("Input Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode dashKey = KeyCode.Space;

    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Aiming")]
    public bool isAiming = false;

    [Header("Smoothing")]
    public float directionSmoothTime = 0.1f;

    private Rigidbody rb;
    private Vector3 inputDirection;
    private Vector3 smoothInputDirection;
    private Vector3 velocityRef;
    private float lastDashTime = -999f;

    // Dash state
    private bool isDashing = false;
    private Vector3 dashStartPos;
    private Vector3 dashTargetPos;
    private float dashElapsed = 0f;

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
        CheckGrounded();
        CheckFallTimer();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.z + camRight * input.x;
            smoothInputDirection = Vector3.SmoothDamp(
                smoothInputDirection,
                moveDir,
                ref velocityRef,
                directionSmoothTime
            );
        }
        else
        {
            smoothInputDirection = Vector3.SmoothDamp(
                smoothInputDirection,
                input,
                ref velocityRef,
                directionSmoothTime
            );
        }

        inputDirection = smoothInputDirection;

        isAiming = Input.GetMouseButton(1);

        // Rotate player
        if (isAiming)
        {
            Vector3 aimDir = cameraTransform.forward;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.1f)
            {
                Quaternion aimRot = Quaternion.LookRotation(aimDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, Time.deltaTime * 10f);
            }
        }
        else if (inputDirection.sqrMagnitude > 0.1f)
        {
            Quaternion moveRot = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRot, Time.deltaTime * 10f);
        }

        // Dash
        if (!isDashing && Input.GetKeyDown(dashKey) && Time.time > lastDashTime + movementStats.dashCooldown)
        {
            Vector3 dashDir = inputDirection.sqrMagnitude > 0.01f
                ? transform.forward
                : -transform.forward;

            StartCoroutine(DashCoroutine(dashDir));
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
        if (isDashing) return;

        float speed = movementStats.moveSpeed;

        if (isAiming)
            speed *= movementStats.aimSpeedMultiplier;

        if (Input.GetKey(sprintKey) && inputDirection.sqrMagnitude > 0.01f)
            speed *= movementStats.sprintMultiplier;

        Vector3 move = inputDirection * speed;
        rb.MovePosition(rb.position + move * Time.fixedDeltaTime);
    }

    IEnumerator DashCoroutine(Vector3 direction)
    {
        isDashing = true;
        dashElapsed = 0f;

        dashStartPos = transform.position;
        dashTargetPos = dashStartPos + (direction * movementStats.dashDistance);

        while (dashElapsed < movementStats.dashTime)
        {
            dashElapsed += Time.deltaTime;
            float t = dashElapsed / movementStats.dashTime;
            transform.position = Vector3.Lerp(dashStartPos, dashTargetPos, t);
            yield return null;
        }

        transform.position = dashTargetPos;
        isDashing = false;
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