using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewAttackState", menuName = "AI/Combat States/Attack")]
public class AttackState : EnemyCombatStateBase
{
    [Header("Attack Distance Settings")]
    public float minAllowedDistance = 1f;
    public float maxAllowedDistance = 7f;
    public float desiredAttackRange = 2.5f;
    public float idealAttackDistance = 2.5f;
    public float attackDistanceTolerance = 0.4f;

    [Header("Movement Settings")]
    public float minApproachSpeed = 2f;
    public float maxApproachSpeed = 4f;
    public float moveForwardAfterSwingStart = 0.2f;
    public float swingAdjustmentSpeed = 2f;
    public float maxSidewaysAdjustment = 0.3f;
    public float minDuration = 0.5f;
    public bool allowPursuitDuringSwing = true;

    [Header("Rotation Settings")]
    public float rotationSpeedPreSwing = 5f;
    public float rotationSpeedDuringSwing = 3f;
    public float maxTurnDelta = 15f;

    [Header("Cooldown Settings")]
    public float postAttackCooldown = 0.5f;

    [Header("Debug")]
    public bool debugEnabled = false;

    public override void EnterState(EnemyCombatController controller)
    {
        Transform target = controller.GetTarget();
        if (target == null) return;

        NavMeshAgent agent = controller.GetAgent();
        if (agent != null && agent.enabled)
        {
            agent.updatePosition = true;
            agent.ResetPath();
        }

        Vector3 direction = (target.position - controller.transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            float angleDelta = Mathf.Min(rotationSpeedPreSwing * 0.1f * 100f, maxTurnDelta);
            controller.transform.rotation = Quaternion.RotateTowards(controller.transform.rotation, targetRotation, angleDelta);
        }
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        NavMeshAgent agent = controller.GetAgent();
        Transform target = controller.GetTarget();
        EquippedWeaponController weapon = controller.GetWeaponController();

        if (controller == null || target == null || agent == null)
            yield break;

        // Ensure clean NavMeshAgent state at start of attack
        agent.updatePosition = true;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            if (debugEnabled)
            {
                Debug.LogWarning(controller.name + " Agent not ready. Aborting AttackState.");
            }
            yield break;
        }

        float initialDistance = Vector3.Distance(controller.transform.position, target.position);
        if (debugEnabled)
        {
            Debug.Log(controller.name + " AttackState Execute() — Initial Distance to Target: " + initialDistance);
        }

        float earlyAbortDistance = controller.maxAttackStateDistance > 0f ? controller.maxAttackStateDistance : 12f;
        if (Vector3.Distance(controller.transform.position, target.position) > earlyAbortDistance)
        {
            if (debugEnabled)
            {
                Debug.LogWarning(controller.name + " aborted attack: player too far at start.");
            }
            controller.RequestInterruptAndReplan();
            yield break;
        }

        // Removed agent.ResetPath();

        // Removed residual velocity warning block

        agent.speed = UnityEngine.Random.Range(minApproachSpeed, maxApproachSpeed);

        if (debugEnabled)
        {
            Debug.Log(controller.name + " AttackState entering Phase A: Approach");
        }

        if (Vector3.Distance(controller.transform.position, target.position) > idealAttackDistance + attackDistanceTolerance)
        {
            yield return new WaitUntil(() => !agent.pathPending);

            float approachTimeout = 2f;
            float approachTimer = 0f;

            while (Vector3.Distance(controller.transform.position, target.position) > idealAttackDistance + attackDistanceTolerance)
            {
                if (!controller.PlayerInCombatVision())
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning(controller.name + " lost vision during approach. Aborting AttackState.");
                    }
                    yield break;
                }

                agent.SetDestination(target.position);
                controller.RotateTowardsTarget(target.position, rotationSpeedPreSwing, maxTurnDelta);

                // Check if player has moved out of acceptable range during approach
                float approachDist = Vector3.Distance(controller.transform.position, target.position);
                if (approachDist > desiredAttackRange + attackDistanceTolerance)
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning(controller.name + " aborted approach: player moved out of range.");
                    }
                    var rushState = controller.GetStateByName("RushStateTest");
                    if (rushState != null)
                    {
                        controller.overrideRushRange = true;
                        if (debugEnabled)
                        {
                            Debug.Log(controller.name + " overrideRushRange set to true due to approach abort.");
                        }
                        controller.EnqueueForceState("RushStateTest");
                    }
                    yield break;
                }

                approachTimer += Time.deltaTime;
                if (approachTimer >= approachTimeout)
                {
                    if (debugEnabled)
                    {
                        Debug.LogWarning(controller.name + " AttackState approach timeout. Aborting.");
                    }
                    // Notify controller to adjust next behavior weight due to timeout
                    controller.RegisterStateAbortReason("AttackTimeout");
                    yield break;
                }

                yield return null;
            }
        }
        else
        {
            if (debugEnabled)
            {
                Debug.Log(controller.name + " Target already within attack range. Skipping approach.");
            }
        }

        // Check if player has moved out of range before starting the swing
        float currentDistance = Vector3.Distance(controller.transform.position, target.position);
        if (currentDistance > desiredAttackRange + attackDistanceTolerance)
        {
            if (debugEnabled)
            {
                Debug.LogWarning(controller.name + " aborted attack: player moved out of range before swing.");
            }
            controller.RequestInterruptAndReplan();
            yield break;
        }

        // Phase B: Initiate Swing
        weapon?.PerformAttack();
        // Smooth swing initiation without delay or position reset
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        float preSwingDistance = Vector3.Distance(controller.transform.position, target.position);
        if (preSwingDistance > (idealAttackDistance + attackDistanceTolerance) ||
            preSwingDistance < (idealAttackDistance - attackDistanceTolerance))
        {
            Vector3 directionToTarget = (target.position - controller.transform.position).normalized;
            float distanceAdjustment = preSwingDistance - idealAttackDistance;
            controller.transform.position -= directionToTarget * distanceAdjustment * 0.5f;
        }

        // Phase C: Swing Maintain
        float elapsed = 0f;
        float tickAccumulator = 0f;
        while (elapsed < minDuration)
        {
            elapsed += Time.deltaTime;
            tickAccumulator += Time.deltaTime;
            if (tickAccumulator >= 0.25f)
            {
                // Debug.Log(controller.name + " AttackState swing maintain tick. Elapsed: " + elapsed);
                tickAccumulator = 0f;
            }

            if (target == null) break;
            Vector3 direction = (target.position - controller.transform.position).normalized;

            controller.RotateTowardsTarget(target.position, rotationSpeedDuringSwing, maxTurnDelta);

            float distanceToTarget = Vector3.Distance(controller.transform.position, target.position);
            if (distanceToTarget < (idealAttackDistance - attackDistanceTolerance) || distanceToTarget > (idealAttackDistance + attackDistanceTolerance))
            {
                Vector3 desiredOffset = (controller.transform.position - target.position).normalized * idealAttackDistance;
                Vector3 idealPosition = target.position + desiredOffset;
                Vector3 desiredMove = idealPosition - controller.transform.position;
                desiredMove.y = 0;

                Vector3 moveDir = desiredMove.normalized;
                Vector3 forwardComponent = Vector3.Project(moveDir, controller.transform.forward);
                Vector3 sidewaysComponent = Vector3.ProjectOnPlane(moveDir, controller.transform.forward);
                Vector3 cappedSideways = Vector3.ClampMagnitude(sidewaysComponent, maxSidewaysAdjustment);
                Vector3 finalMove = forwardComponent + cappedSideways;

                finalMove = Vector3.ClampMagnitude(finalMove, swingAdjustmentSpeed * Time.deltaTime);
                controller.transform.position += finalMove;
            }
            else if (allowPursuitDuringSwing)
            {
                Vector3 forwardMove = direction * swingAdjustmentSpeed * Time.deltaTime;
                controller.transform.position += forwardMove;
            }

            yield return null;
        }

        if (debugEnabled)
        {
            Debug.Log(controller.name + " AttackState completed swing. Checking distance to player...");
        }

        // Phase D: Post-Attack Cooldown and Reacquire
        float checkDuration = 0.5f;
        float timer = 0f;
        while (timer < checkDuration)
        {
            if (controller == null || controller.GetTarget() == null) break;

            float dist = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
            if (dist > desiredAttackRange + attackDistanceTolerance)
            {
                if (debugEnabled)
                {
                    Debug.Log(controller.name + " Post-attack distance exceeded. Replanning...");
                }
                var rushState = controller.GetStateByName("RushStateTest");
                if (rushState != null)
                {
                    controller.EnqueueForceState("RushStateTest");
                }
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(postAttackCooldown);
    }

    public override void ExitState(EnemyCombatController controller)
    {
        var agent = controller.GetAgent();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        if (controller == null || controller.GetTarget() == null || !controller.gameObject.activeInHierarchy)
            return false;

        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        return distance >= minAllowedDistance && distance <= maxAllowedDistance;
    }
}
