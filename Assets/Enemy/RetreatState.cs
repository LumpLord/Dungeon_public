using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[CreateAssetMenu(menuName = "Enemy/Combat States/Retreat State")]
public class RetreatState : EnemyCombatStateBase
{
    [SerializeField] public float minStartDistance = 3f;
    [SerializeField] public float maxStartDistance = 10f;
    [SerializeField] public float retreatDistance = 6f;
    [SerializeField] public float retreatSpeedMultiplier = 1.5f;
    [SerializeField] public float minTimeBeforeNextState = 1.25f;

    private float endTime;

    public override void EnterState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} EnterState() called for {this.GetType().Name}");
        Debug.Log($"{controller.name} is entering {this.GetType().Name}");

        var agent = controller.GetAgent();
        var target = controller.GetTarget();

        if (agent == null || target == null)
        {
            Debug.LogWarning($"{controller.name} RetreatState early exit: agent null? {agent == null}, target null? {target == null}");
            return;
        }

        agent.isStopped = false;
        agent.speed *= retreatSpeedMultiplier;

        Vector3 awayDirection = (controller.transform.position - target.position).normalized;
        // Introduce a small randomized angle to vary retreat direction slightly
        float angleVariation = Random.Range(-20f, 20f);
        awayDirection = Quaternion.Euler(0, angleVariation, 0) * awayDirection;
        Vector3 retreatDestination = controller.transform.position + awayDirection * retreatDistance;

        // Increase allowed area radius from 2f to 3f
        if (NavMesh.SamplePosition(retreatDestination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            retreatDestination = hit.position;
        }

        Debug.Log($"{controller.name} Retreat destination calculated: {retreatDestination}");

        agent.updateRotation = false; // Disable automatic NavMeshAgent rotation
        // Abort if retreat destination is too close to current position
        if (Vector3.Distance(controller.transform.position, retreatDestination) < 0.5f)
        {
            Debug.LogWarning($"{controller.name} RetreatState aborted: destination too close to retreat.");
            endTime = Time.time; // Immediately end the state
            return;
        }
        agent.SetDestination(retreatDestination);

        Debug.Log($"{controller.name} Retreat destination set: {agent.destination}, agent speed: {agent.speed}");

        Debug.Log($"{controller.name} RetreatState: setting endTime with minTimeBeforeNextState = {minTimeBeforeNextState}");
        if (minTimeBeforeNextState <= 0f)
        {
            minTimeBeforeNextState = 1.25f; // fallback default
            Debug.LogWarning($"{controller.name} RetreatState: minTimeBeforeNextState was 0 or less, defaulting to 1.25s");
        }
        endTime = Time.time + minTimeBeforeNextState;
        Debug.Log($"{controller.name} RetreatState endTime set to: {endTime}, current time: {Time.time}");
    }

    public override IEnumerator Execute(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is executing {this.GetType().Name}");
        Debug.Log($"{controller.name} RetreatState duration set to end at: {endTime}, current time: {Time.time}");
        var agent = controller.GetAgent();
        var target = controller.GetTarget();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh || target == null || controller == null || controller.gameObject == null || !controller.gameObject.activeInHierarchy)
            yield break;

        while (true)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                yield break;

            controller.FaceTargetSmooth();

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f)
            {
                Debug.Log($"{controller.name} RetreatState destination reached. Exiting early.");
                break;
            }

            yield return null;
        }

        if (agent != null && agent.isOnNavMesh && !agent.updatePosition)
        {
            Debug.LogWarning($"{controller.name} RetreatState completed but agent.updatePosition was disabled. Resetting...");
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;

            // Instead of snapping to current position, attempt to re-sync gently by warping
            NavMeshHit hit;
            if (NavMesh.SamplePosition(controller.transform.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.Log($"{controller.name} RetreatState: agent warped to valid navmesh position at {hit.position}");
            }
            else
            {
                Debug.LogWarning($"{controller.name} RetreatState: failed to find valid navmesh position near {controller.transform.position}");
            }
        }

        Debug.Log($"{controller.name} RetreatState duration met or destination reached. Exiting.");
    }

    public override void ExitState(EnemyCombatController controller)
    {
        Debug.Log($"{controller.name} is exiting {this.GetType().Name}");
        var agent = controller.GetAgent();
        if (agent != null)
            agent.speed /= retreatSpeedMultiplier;
    }

    public override bool CanExit(EnemyCombatController controller)
    {
        return true;
    }

    public override bool CanQueueNextState(EnemyCombatController controller)
    {
        return true;
    }

        public override bool CanExecute(EnemyCombatController controller)
    {
        if (controller == null || controller.GetTarget() == null)
            return false;

        float distance = Vector3.Distance(controller.transform.position, controller.GetTarget().position);
        return distance >= minStartDistance && distance <= maxStartDistance;
    }
}
