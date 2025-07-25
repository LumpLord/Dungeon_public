using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewStalkState", menuName = "AI/Combat States/Stalk")]
public class StalkState : EnemyCombatStateBase
{
    public bool ignoreMaxDistance = false;
    public float circleRadius = 3.5f;
    public float stalkMoveSpeed = 2f;
    public float orbitSpeed = 1f;
    public float minExitDuration = 0.5f;
    public float minStalkTime = 12f;
    public float maxStalkTime = 12f;

    [Header("Stalk Distance Range")]
    public float minAllowedDistance = 7f;
    public float maxAllowedDistance = 12f;

    [Header("Circle Drift")]
    public float minCircleDriftSpeed = 0.1f;
    public float maxCircleDriftSpeed = 0.5f;
    [Range(0f, 1f)]
    public float oddsCircleInward = 0.5f;

    [Header("Debug")]
    public bool debugEnabled = false;

    public override void EnterState(EnemyCombatController controller)
    {
        if (debugEnabled)
            Debug.Log($"{controller.name} is entering {this.GetType().Name}");
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        if (debugEnabled)
            Debug.Log($"{controller.name} is executing {this.GetType().Name}");

        var agent = controller.GetAgent();
        var target = controller.GetTarget();
        if (controller == null || agent == null || target == null || !agent.enabled || !agent.isOnNavMesh)
            yield break;

        float stalkDuration = Random.Range(minStalkTime, maxStalkTime);
        float timer = 0f;
        bool clockwise = Random.value > 0.5f;
        float direction = clockwise ? 1f : -1f;

        // Sample drift speed and direction
        float circleDriftSpeed = Random.Range(minCircleDriftSpeed, maxCircleDriftSpeed);
        float driftDirection = (Random.value < oddsCircleInward) ? -1f : 1f; // -1 = inward, 1 = outward

        Vector3 toEnemy = controller.transform.position - target.position;
        float angle = Mathf.Atan2(toEnemy.z, toEnemy.x);

        agent.speed = stalkMoveSpeed;
        float currentRadius = circleRadius;

        while (timer < stalkDuration)
        {
            if (!controller.PlayerInCombatVision()) yield break;
            if (!agent.enabled || !agent.isOnNavMesh) yield break;

            float distanceToPlayer = Vector3.Distance(controller.transform.position, target.position);
            if (!ignoreMaxDistance && distanceToPlayer > maxAllowedDistance)
            {
                if (debugEnabled)
                    Debug.LogWarning($"{controller.name} aborted stalk: player moved out of range.");
                var rushState = controller.GetStateByName("RushStateTest");
                if (rushState != null)
                {
                    if (debugEnabled)
                        Debug.Log($"{controller.name} forcibly enqueuing RushState due to stalk abort.");
                    controller.EnqueueForceState("RushStateTest");
                }
                yield break;
            }

            timer += Time.deltaTime;
            angle += direction * orbitSpeed * Time.deltaTime;

            // Drift the radius
            currentRadius += driftDirection * circleDriftSpeed * Time.deltaTime;
            // Clamp radius to never go below 0.5 or above 2x the original circleRadius
            currentRadius = Mathf.Clamp(currentRadius, 0.5f, circleRadius * 2f);

            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * currentRadius;
            Vector3 desiredPosition = target.position + offset;

            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            controller.FaceTargetSmooth();
            yield return null;
        }

        agent.ResetPath();
        if (debugEnabled)
            Debug.Log($"{controller.name} StalkState: finished stalking movement, starting exit delay.");

        float elapsed = 0f;
        while (elapsed < minExitDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (debugEnabled)
            Debug.Log($"{controller.name} StalkState: completed execution.");
        if (debugEnabled)
            Debug.Log($"{controller.name} forcibly enqueuing RushState after stalk completion.");
        controller.EnqueueForceState("RushStateTest");
    }

    public override void ExitState(EnemyCombatController controller)
    {
        if (debugEnabled)
            Debug.Log($"{controller.name} is exiting {this.GetType().Name}");
    }

    public override bool CanExecute(EnemyCombatController controller)
    {
        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        if (ignoreMaxDistance)
            return distance >= minAllowedDistance;
        return distance >= minAllowedDistance && distance <= maxAllowedDistance;
    }

    // Diagnostics for lockup debugging
    public override bool CanExit(EnemyCombatController controller)
    {
        if (debugEnabled)
            Debug.Log($"{controller.name} CanExit StalkState? timer OK by minExitDuration={minExitDuration}");
        return base.CanExit(controller);
    }

    public override bool CanQueueNextState(EnemyCombatController controller)
    {
        if (debugEnabled)
            Debug.Log($"{controller.name} CanQueueNext StalkState? timer OK by minExitDuration={minExitDuration}");
        return base.CanQueueNextState(controller);
    }
}