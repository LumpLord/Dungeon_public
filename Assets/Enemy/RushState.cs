using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[CreateAssetMenu(menuName = "Enemy/Combat States/Rush State")]
public class RushState : EnemyCombatStateBase
{
    public float rushSpeed = 12f;
    public float maxRushDuration = 1.2f;
    public float minStartDistance = 2f;
    public float maxStartDistance = 15f;
    public bool ignoreMaxDistance = false;
    //[SerializeField] private float rushStoppingDistance = 1.2f;
    public float minTimeBeforeNextState = 0.75f;
    public float rushStopDistance = 1.5f;

    [SerializeField] private float overrideMinDistance = 10f;

    [SerializeField] private float groundDetectionRayDistance = 5.0f;
    [SerializeField] private LayerMask groundMask;

    public float startTime;
    public float endTime;
    public bool isRushing;

    public override void EnterState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is entering {this.GetType().Name} with maxRushDuration={maxRushDuration}");

        startTime = Time.time;
        endTime = startTime + maxRushDuration + minTimeBeforeNextState;
        isRushing = true;

        ignoreMaxDistance = false;

        controller.overrideRushRangeUsed = false;

        var agent = controller.GetAgent();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(controller.transform.position);
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.updatePosition = false;
        }
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");
        Debug.Log($"{controller.name} RushState: checking Rigidbody and NavMeshAgent state...");

        var target = controller.GetTarget();
        var rb = controller.GetComponent<Rigidbody>();
        var agent = controller.GetAgent();

        if (rb == null)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Rigidbody is null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (rb.isKinematic)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Rigidbody is kinematic");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (agent == null)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent is null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (!agent.enabled)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent is disabled.");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: NavMeshAgent not on NavMesh.");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }
        else if (controller == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"{controller.name} RushState aborted: Controller GameObject is inactive or null");
            isRushing = false;
            yield return new WaitForSeconds(minTimeBeforeNextState);
            yield break;
        }

        float failureGraceTime = 0.25f;
        float failureTimer = 0f;

        Vector3 rushDirection = (target.position - controller.transform.position).normalized;
        rushDirection.y = 0f;
        rb.linearVelocity = rushDirection * rushSpeed;
        Debug.Log($"{controller.name} RushState velocity set once to: {rb.linearVelocity}");

        while (Time.time - startTime < maxRushDuration)
        {
            if (target == null || controller == null) break;

            controller.FaceTargetSmooth();

            float distanceToTarget = Vector3.Distance(controller.transform.position, target.position);
            if (distanceToTarget <= rushStopDistance)
            {
                Debug.Log($"{controller.name} RushState hit range reached. Initiating attack.");
                rb.linearVelocity = Vector3.zero;
                controller.FaceTargetSmooth();

                var weaponController = controller.GetComponentInChildren<EquippedWeaponController>();
                if (weaponController != null)
                {
                    Debug.Log($"{controller.name} RushState triggering weapon attack.");
                    weaponController.PerformAttack();
                }
                else
                {
                    Debug.LogWarning($"{controller.name} RushState could not find EquippedWeaponController.");
                }

                break;
            }

            Vector3 nextPosition = controller.transform.position + rushDirection * Time.deltaTime * rushSpeed;
            nextPosition.y = controller.transform.position.y; // Option C: Prevent vertical desync

            // Updated ground detection logic
            float rayOffset = 0.75f;
            float rayOriginHeight = 0.3f;
            float dynamicAngleFactor = 1f - Mathf.Clamp(rb.linearVelocity.magnitude / rushSpeed, 0.0f, 1f);
            float angledRayLength = groundDetectionRayDistance * (5.5f + 2.5f * dynamicAngleFactor) * 0.25f;

            Vector3[] rayOrigins = new Vector3[]
            {
                nextPosition + Vector3.up * rayOriginHeight, // Center
                nextPosition + controller.transform.right * -rayOffset + Vector3.up * rayOriginHeight, // Left
                nextPosition + controller.transform.right * rayOffset + Vector3.up * rayOriginHeight,  // Right
                nextPosition + controller.transform.forward * 1.0f + Vector3.up * rayOriginHeight,     // Forward center
                nextPosition + (controller.transform.forward + controller.transform.right * 0.5f).normalized * 1.0f + Vector3.up * rayOriginHeight, // Forward right
                nextPosition + (controller.transform.forward - controller.transform.right * 0.5f).normalized * 1.0f + Vector3.up * rayOriginHeight  // Forward left
            };

            float speedRatio = rb.linearVelocity.magnitude / rushSpeed;
            float forwardAngle = Mathf.Lerp(90f, 45f, speedRatio);
            float sideAngle = Mathf.Lerp(75f, 35f, speedRatio);

            // Apply an additional -30 degree adjustment (angle upward by 30 degrees) when stationary
            Vector3 baseDownward = Quaternion.AngleAxis(forwardAngle - 15f, controller.transform.right) * controller.transform.forward;

            Vector3[] rayDirs = new Vector3[]
            {
                Vector3.down,
                Vector3.down,
                Vector3.down,
                baseDownward,
                Quaternion.AngleAxis(sideAngle, Vector3.up) * baseDownward,
                Quaternion.AngleAxis(-sideAngle, Vector3.up) * baseDownward
            };

            int failedRayCount = 0;
            for (int i = 0; i < rayOrigins.Length; i++)
            {
                float currentRayLength = (i >= 3) ? angledRayLength : groundDetectionRayDistance;
                bool hit = Physics.Raycast(rayOrigins[i], rayDirs[i], out RaycastHit hitInfo, currentRayLength, groundMask);
                if (!hit) failedRayCount++;

                Debug.DrawRay(rayOrigins[i], rayDirs[i] * currentRayLength, hit ? Color.green : Color.red, 1.0f);
                if (hit) Debug.DrawLine(rayOrigins[i], hitInfo.point, Color.magenta, 1.0f);
            }

            bool safeToRush = failedRayCount < 2;
            if (failedRayCount > 0)
            {
                Debug.Log($"{controller.name} HybridGroundCheck → failedRays={failedRayCount}, safeToRush={safeToRush}");
            }

            if (!safeToRush)
            {
                failureTimer += Time.deltaTime;
                rb.linearVelocity = Vector3.zero;

                Debug.LogWarning($"{controller.name} RushState ground check failed... (failedRayCount={failedRayCount}, graceTimer={failureTimer:F2})");

                if (failedRayCount >= 4 || failureTimer >= failureGraceTime)
                {
                    Debug.LogWarning($"{controller.name} RushState aborted: Unsafe terrain or grace timeout.");
                    isRushing = false;

                    float fallbackDistanceToTarget = Vector3.Distance(controller.transform.position, target.position);
                    float fallbackDistanceThreshold = 6f;
                    if (fallbackDistanceToTarget < fallbackDistanceThreshold)
                    {
                        controller.EnqueueWeightedFallback(new (IEnemyCombatState, float)[]
                        {
                            ((IEnemyCombatState)controller.GetStateByName("AttackState"), 0.3f),
                            ((IEnemyCombatState)controller.GetStateByName("StalkState"), 0.7f)
                        });
                    }
                    else
                    {
                        var pursuitState = controller.GetStateByName("PursuitState");
                        if (pursuitState != null)
                        {
                            controller.EnqueueState(pursuitState);
                        }
                        else
                        {
                            Debug.LogWarning($"{controller.name} RushState: PursuitState is null and could not be enqueued.");
                        }
                    }

                    yield return new WaitForSeconds(minTimeBeforeNextState);
                    yield break;
                }

                yield return null;
                continue;
            }
            else
            {
                failureTimer = 0f; // Reset timer on success
            }

            // Recalculate direction and set velocity
            Vector3 updatedDirection = (target.position - controller.transform.position).normalized;
            updatedDirection.y = 0f;
            rb.linearVelocity = updatedDirection * rushSpeed;

            yield return null;
        }

        Debug.Log($"{controller.name} RushState movement complete, cleaning up.");
        rb.linearVelocity = Vector3.zero;
        isRushing = false;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");

        controller.overrideRushRange = false;
        isRushing = false;
        var rb = controller.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        var agent = controller.GetAgent();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(controller.transform.position); // Option B: Resync at end
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }

    public override bool CanExit(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} CanExit RushState? isRushing={isRushing} time={Time.time} vs endTime={endTime}");
        if (!isRushing) return true;
        return Time.time >= endTime;
    }

    public override bool CanQueueNextState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} CanQueueNext RushState? isRushing={isRushing} time={Time.time} vs endTime={endTime}");
        return !isRushing && Time.time >= endTime;
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        var target = controller.GetTarget();
        if (target == null)
        {
            Debug.Log($"{controller.name} RushState.CanExecute: target is null");
            return false;
        }

        float distance = Vector3.Distance(controller.transform.position, target.position);
        Debug.Log($"{controller.name} RushState.CanExecute: distance={distance}, overrideRushRange={controller.overrideRushRange}");

        if (controller.overrideRushRange && !controller.overrideRushRangeUsed)
        {
            Debug.Log($"{controller.name} overrideRushRange is TRUE. Allowing rush at distance {distance}");
            controller.overrideRushRangeUsed = true;
            return distance >= overrideMinDistance;
        }

        bool canExecute = distance >= minStartDistance && distance <= maxStartDistance;
        if (!canExecute)
        {
            Debug.Log($"{controller.name} RushState denied execution: distance={distance} not in range ({minStartDistance}-{maxStartDistance}) and overrideRushRange={controller.overrideRushRange}");
        }
        return canExecute;
    }
}